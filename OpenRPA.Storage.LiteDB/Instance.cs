using LiteDB;
using Newtonsoft.Json.Linq;
using OpenRPA.Input;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;
using System;
using System.Linq;
using System.Management.Instrumentation;
using System.Threading.Tasks;

namespace OpenRPA.Storage.LiteDB
{
    public class Instance : IStorage
    {
        public LiteDatabase db;
        public string Name { get; set;  }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task Initialize()
        {
            BsonMapper.Global.MaxDepth = 50;
            BsonMapper.Global.TypeDescriptor = "__type";

            BsonMapper.Global.RegisterType<Uri>
            (
                serialize: (uri) => uri.AbsoluteUri,
                deserialize: (bson) => new Uri(bson.AsString)
            );
            BsonMapper.Global.RegisterType<JToken>
            (
                serialize: (o) => o.ToString(),
                deserialize: (bson) => JToken.Parse(bson.ToString())
            );
            
            var connecttype = "";
            try
            {
                if (!Config.local.skip_child_session_check)
                {
                    if (Interfaces.win32.ChildSession.IsChildSessionsEnabled())
                    {
                        connecttype = ";connection=shared";
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex.Message);
            }
            if (!System.IO.Directory.Exists(Interfaces.Extensions.ProjectsDirectory))
            {
                System.IO.Directory.CreateDirectory(Interfaces.Extensions.ProjectsDirectory);

            }
            var mapper = new UseIgnoreMapper();
            // mapper.IncludeFields = true;
            var dbfilename = "offline.db";
            var logfilename = "offline-log.db";
            if (!string.IsNullOrEmpty(Config.local.wsurl))
            {
                dbfilename = new Uri(Config.local.wsurl).Host + ".db";
                logfilename = new Uri(Config.local.wsurl).Host + "-log.db";
            }
            try
            {
                db = new LiteDatabase("Filename=" + Interfaces.Extensions.ProjectsDirectory + @"\" + dbfilename + connecttype, mapper);
            }
            catch (System.IO.IOException ex)
            {
                System.Windows.MessageBox.Show("Cannot start OpenRPA" + Environment.NewLine + ex.Message);
                System.Windows.Forms.Application.Exit();
                System.Environment.Exit(1);
                return;
            }
            catch (LiteException)
            {
                if (System.IO.File.Exists(Interfaces.Extensions.ProjectsDirectory + @"\" + logfilename))
                {
                    System.IO.File.Delete(Interfaces.Extensions.ProjectsDirectory + @"\" + logfilename);
                }
                if (System.IO.File.Exists(Interfaces.Extensions.ProjectsDirectory + @"\" + dbfilename))
                {
                    var backupfilename = dbfilename + ".bak"; int counter = 0;
                    while (System.IO.File.Exists(Interfaces.Extensions.ProjectsDirectory + @"\" + backupfilename))
                    {
                        counter++;
                        backupfilename = dbfilename + ".bak" + counter;
                    }
                    System.IO.File.Copy(Interfaces.Extensions.ProjectsDirectory + @"\" + dbfilename, Interfaces.Extensions.ProjectsDirectory + @"\" + backupfilename);
                    System.IO.File.Delete(Interfaces.Extensions.ProjectsDirectory + @"\" + dbfilename);
                    db = new LiteDatabase("Filename=" + Interfaces.Extensions.ProjectsDirectory + @"\" + dbfilename + connecttype, mapper);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Cannot start OpenRPA" + Environment.NewLine + ex.Message);
                System.Windows.Forms.Application.Exit();
                System.Environment.Exit(1);
                return;
            }
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                try
                {
                    isDisposing= true;
                    if (db != null) db.Dispose();
                }
                catch (Exception)
                {
                }
            };
            //dbWorkflows = db.GetCollection<IWorkflow>("workflows");
            //dbWorkflows.EnsureIndex(x => x._id, true);

            //dbProjects = db.GetCollection<IProject>("projects");
            //dbProjects.EnsureIndex(x => x._id, true);

            //dbDetectors = db.GetCollection<IDetector>("detectors");
            //dbDetectors.EnsureIndex(x => x._id, true);

            //dbWorkflowInstances = db.GetCollection<IWorkflowInstance>("workflowinstances");
            //dbWorkflowInstances.EnsureIndex(x => x._id, true);

            //dbWorkItemQueues = db.GetCollection<IWorkitemQueue>("workitemqueues");
            //dbWorkItemQueues.EnsureIndex(x => x._id, true);

            //dbWorkitems = db.GetCollection<IWorkitem>("workitems");
            //dbWorkItemQueues.EnsureIndex(x => x._id, true);

        }
        private ILiteCollection<T> Collection<T> () where T : class
        {
            ILiteCollection<T> result = null;

            if (typeof(IWorkflow).IsAssignableFrom(typeof(T)))
            {
                result  = db.GetCollection<T>("workflows");
            }
            else if (typeof(IDetector).IsAssignableFrom(typeof(T)))
            {
                result = db.GetCollection<T>("detectors");
            }
            else if (typeof(IProject).IsAssignableFrom(typeof(T)))
            {
                result = db.GetCollection<T>("projects");
            }
            else if (typeof(IWorkitemQueue).IsAssignableFrom(typeof(T)))
            {
                result = db.GetCollection<T>("workitemqueues");
            }
            else if (typeof(IWorkitem).IsAssignableFrom(typeof(T)))
            {
                result = db.GetCollection<T>("workitems");
            } else
            {
                return null;
            }
            return result;
        }
        public async Task<T[]> FindAll<T>() where T : apibase
        {
            if (isDisposing) return Array.Empty<T>();
            return Collection<T>().FindAll().Select(w => w as T).ToArray();
        }
        public async Task<T> FindById<T>(string id) where T : apibase
        {
            if (isDisposing) return null;
            return Collection<T>().FindById(id);
        }
        public async Task<T> Insert<T>(T item) where T : apibase
        {
            if (isDisposing) return item;
            var id = Collection<T>().Insert(item);
            return await FindById<T>(id);
        }
        public async Task<T> Update<T>(T item) where T : apibase
        {
            if (isDisposing) return item;
            Collection<T>().Update(item);
            return item;
        }
        public async Task Delete<T>(string id) where T : apibase
        {
            if(isDisposing) return;
            Collection<T>().Delete(id);
        }
        private bool isDisposing = false;
        public void Dispose()
        {
            isDisposing = true;
            try
            {
                db?.Dispose();
            }
            catch (Exception)
            {
            }            
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
