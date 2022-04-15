using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA
{
    public class LocallyCached : apibase
    {
        private static object savelock = new object();
        public async Task Save<T>(bool skipOnline = false) where T : apibase
        {
            try
            {
                _backingFieldValues["_disabledirty"] = true;
                if (RobotInstance.instance.db == null) return;
                var collection = RobotInstance.instance.db.GetCollection<T>(_type.ToLower() + "s");
                var entity = (T)Convert.ChangeType(this, typeof(T));
#if DEBUG
                // Log.Output("LocallyCached.Save<" + typeof(T).Name + ">()");
#endif
                if (!global.isConnected )
                {
                    try
                    {
                        if (string.IsNullOrEmpty(_id))
                        {
                            _id = Guid.NewGuid().ToString();
                            isLocalOnly = true;
                            // collection.Insert(entity);
                        }
                        else
                        {
                            entity._version++; // Add one to avoid watch update
                            // collection.Update(entity);
                            entity._modified = DateTime.Now;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                } 
                else if (string.IsNullOrEmpty(_id))
                {
                    isDirty = true;
                    _id = Guid.NewGuid().ToString();
                }
                    string collectionname = "openrpa";
                if (_type == "workflowinstance") collectionname = "openrpa_instances";
                if (_type == "workitemqueue") collectionname = "mq";
                if (_type == "workflowinstance" && Config.local.skip_online_state) {
                    skipOnline = true;
                    isDirty = false;
                }                
                if (global.isConnected && !skipOnline)
                {
                    if (string.IsNullOrEmpty(_id) || isLocalOnly == true)
                    {
                        T result = default(T);
                        try
                        {
                            result = await global.webSocketClient.InsertOne(collectionname, 0, false, entity);
                        }
                        catch (Exception ex)
                        {
                            if(ex.Message.Contains("E11000 duplicate key error"))
                            {
                                result = await global.webSocketClient.InsertOrUpdateOne(collectionname, 0, false, null, entity);
                            } else
                            {
                                throw;
                            }
                        }
                        EnumerableExtensions.CopyPropertiesTo(result, entity, true);
                        isLocalOnly = false;
                        // _backingFieldValues["isDirty"] = false;
                        isDirty = false;
                        Log.Verbose("Inserted to openflow and returned as version " + entity._version + " " + entity._type + " " + entity.name);
                    }
                    else
                    {
                        if (entity.isDirty)
                        {
                            entity._version++; // Add one to avoid watch update
                            try
                            {
                                var result = await global.webSocketClient.InsertOrUpdateOne(collectionname, 0, false, null, entity);
                                if (result != null)
                                {
                                    EnumerableExtensions.CopyPropertiesTo(result, entity, true);
                                    // _backingFieldValues["isDirty"] = false;
                                    isDirty = false;
                                    Log.Verbose("Updated in openflow and returned as version " + entity._version + " " + entity._type + " " + entity.name);
                                }
                            }
                            catch (Exception) 
                            {
                                //Log.Debug("Failed saving " + entity._type + " " + entity._id + " will be updated at next sync or save");
                                throw;
                            }
                        }
                    }
                }
                if (System.Threading.Monitor.TryEnter(savelock, Config.local.thread_lock_timeout_seconds * 1000))
                {
                    try
                    {
                        if(!string.IsNullOrEmpty(_id))
                        {
                            var exists = collection.FindById(_id);
                            if (exists != null) { collection.Update(entity); Log.Verbose("Updated in local db as version " + entity._version + " " + entity._type + " " + entity.name); }
                            if (exists == null) { collection.Insert(entity); Log.Verbose("Inserted in local db as version  " + entity._version + " " + entity._type + " " + entity.name); }
                        } else
                        {
                            // WHY ????
                            System.Diagnostics.Debugger.Break();
                        }
                    }
                    finally
                    {
                        System.Threading.Monitor.Exit(savelock);
                    }
                }
                else { throw new LockNotReceivedException("Locally Cached savelock"); }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _backingFieldValues.Remove("_disabledirty");
            }
        }
        public async Task Delete<T>() where T : apibase
        {
            var collection = RobotInstance.instance.db.GetCollection<T>(_type.ToLower() + "s");
            var entity = (T)Convert.ChangeType(this, typeof(T));
            if (!global.isConnected)
            {
                try
                {
                    isDeleted = true;
                    isDirty = true;
                    if (System.Threading.Monitor.TryEnter(savelock, Config.local.thread_lock_timeout_seconds * 1000))
                    {
                        try
                        {
                            var exists = collection.FindById(_id);
                            if (exists != null) { collection.Update(entity); Log.Verbose("Updated in local db as version " + entity._version + " " + entity._type + " " + entity.name); }
                            if (exists == null) { collection.Insert(entity); Log.Verbose("Inserted in local db as version  " + entity._version + " " + entity._type + " " + entity.name); }
                        }
                        finally
                        {
                            System.Threading.Monitor.Exit(savelock);
                        }
                    }
                    else { throw new LockNotReceivedException("Locally Cached savelock"); }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
            else
            {
                string collectionname = "openrpa";
                if (_type == "workflowinstance") collectionname = "openrpa_instances";

                await global.webSocketClient.DeleteOne(collectionname, entity._id);
                Log.Verbose("Deleted in openflow and as version " + entity._version + " " + entity._type + " " + entity.name);
                if (System.Threading.Monitor.TryEnter(savelock, Config.local.thread_lock_timeout_seconds * 1000))
                {
                    try
                    {
                        var exists = collection.FindById(_id);
                        if (exists != null) { collection.Delete(entity._id); Log.Verbose("Deleted in local db as version " + entity._version + " " + entity._type + " " + entity.name); }
                    }
                    finally
                    {
                        System.Threading.Monitor.Exit(savelock);
                    }
                }
                else { throw new LockNotReceivedException("Locally Cached savelock"); }
            }
        }
    }
}
