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
        public async Task Save<T>() where T : apibase
        {
            bool wasDisableWatch = RobotInstance.instance.DisableWatch;
            try
            {
                RobotInstance.instance.DisableWatch = true;
                var collection = RobotInstance.instance.db.GetCollection<T>(_type.ToLower() + "s");
                var entity = (T)Convert.ChangeType(this, typeof(T));
                if (!global.isConnected)
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
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                }
                if (global.isConnected)
                {
                    if (string.IsNullOrEmpty(_id) || isLocalOnly == true)
                    {
                        var result = await global.webSocketClient.InsertOne("openrpa", 0, false, entity);
                        isLocalOnly = false;
                        isDirty = false;
                        _id = result._id;
                        _acl = result._acl;
                        _modified = result._modified;
                        _modifiedby = result._modifiedby;
                        _modifiedbyid = result._modifiedbyid;
                        if (System.Diagnostics.Debugger.IsAttached) Log.Output("Inserted to openflow and returned as version " + entity._version + " " + entity._type + " " + entity.name);
                    }
                    else
                    {
                        if(entity.isDirty)
                        {
                            entity._version++; // Add one to avoid watch update
                            var result = await global.webSocketClient.UpdateOne("openrpa", 0, false, entity);
                            isDirty = false;
                            _acl = result._acl;
                            _modified = result._modified;
                            _modifiedby = result._modifiedby;
                            _modifiedbyid = result._modifiedbyid;
                            _version = result._version;
                            if (System.Diagnostics.Debugger.IsAttached) Log.Output("Updated in openflow and returned as version " + entity._version + " " + entity._type + " " + entity.name);
                        }
                    }
                }
                lock(savelock)
                {
                    var exists = collection.FindById(_id);
                    if (exists != null) { collection.Update(entity); if (System.Diagnostics.Debugger.IsAttached) Log.Output("Updated in local db as version " + entity._version + " " + entity._type + " " + entity.name); }
                    if (exists == null) { collection.Insert(entity); if (System.Diagnostics.Debugger.IsAttached) Log.Output("Inserted in local db as version  " + entity._version + " " + entity._type + " " + entity.name); }
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                RobotInstance.instance.DisableWatch = wasDisableWatch;
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
                    lock (savelock)
                    {
                        var exists = collection.FindById(_id);
                        if (exists != null) { collection.Update(entity); if (System.Diagnostics.Debugger.IsAttached) Log.Output("Updated in local db as version " + entity._version + " " + entity._type + " " + entity.name); }
                        if (exists == null) { collection.Insert(entity); if (System.Diagnostics.Debugger.IsAttached) Log.Output("Inserted in local db as version  " + entity._version + " " + entity._type + " " + entity.name); }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            } else
            {
                await global.webSocketClient.DeleteOne("openrpa", entity._id);
                if (System.Diagnostics.Debugger.IsAttached) Log.Output("Deleted in openflow and as version " + entity._version + " " + entity._type + " " + entity.name);
                lock (savelock)
                {
                    var exists = collection.FindById(_id);
                    if (exists != null) { collection.Delete(entity._id); if (System.Diagnostics.Debugger.IsAttached) Log.Output("Deleted in local db as version " + entity._version + " " + entity._type + " " + entity.name); }
                }
            }
        }
    }
}
