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
            bool wasDisableWatch = RobotInstance.instance.DisableWatch;
            try
            {
                _backingFieldValues["_disabledirty"] = true;
                if (RobotInstance.instance.db == null) return;
                var collection = RobotInstance.instance.db.GetCollection<T>(_type.ToLower() + "s");
                var entity = (T)Convert.ChangeType(this, typeof(T));
#if DEBUG
                // Log.Output("LocallyCached.Save<" + typeof(T).Name + ">()");
#endif
                if (!global.isConnected)
                {
                    try
                    {
                        isDirty = true;
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
                string collectionname = "openrpa";
                if (_type == "workflowinstance") collectionname = "openrpa_instances";
                if (global.isConnected && !skipOnline)
                {
                    if (string.IsNullOrEmpty(_id) || isLocalOnly == true)
                    {
                        wasDisableWatch = RobotInstance.instance.DisableWatch;
                        Log.Debug("Save::DisableWatch true");
                        RobotInstance.instance.DisableWatch = true;
                        var result = await global.webSocketClient.InsertOne(collectionname, 0, false, entity);
                        isLocalOnly = false;
                        isDirty = false;
                        _id = result._id;
                        _acl = result._acl;
                        _modified = result._modified;
                        _modifiedby = result._modifiedby;
                        _modifiedbyid = result._modifiedbyid;
                        _created = result._created;
                        _createdby = result._createdby;
                        _createdbyid = result._createdbyid;
                        Log.Verbose("Inserted to openflow and returned as version " + entity._version + " " + entity._type + " " + entity.name);
                    }
                    else
                    {
                        if (entity.isDirty)
                        {
                            entity._version++; // Add one to avoid watch update
                            try
                            {
                                wasDisableWatch = RobotInstance.instance.DisableWatch;
                                Log.Debug("Save::DisableWatch true");
                                RobotInstance.instance.DisableWatch = true;
                                var result = await global.webSocketClient.InsertOrUpdateOne(collectionname, 0, false, null, entity);
                                if (result != null)
                                {
                                    _backingFieldValues["isDirty"] = false;
                                    _acl = result._acl;
                                    _modified = result._modified;
                                    _modifiedby = result._modifiedby;
                                    _modifiedbyid = result._modifiedbyid;
                                    _created = result._created;
                                    _createdby = result._createdby;
                                    _createdbyid = result._createdbyid;
                                    _version = result._version;
                                    Log.Verbose("Updated in openflow and returned as version " + entity._version + " " + entity._type + " " + entity.name);
                                }
                                else
                                {
                                    Log.Debug("Failed saving " + entity._type + " " + entity._id + " will be updated at next sync or save");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.ToString());
                            }
                        }
                    }
                }
                lock (savelock)
                {
                    var exists = collection.FindById(_id);
                    if (exists != null) { collection.Update(entity); Log.Verbose("Updated in local db as version " + entity._version + " " + entity._type + " " + entity.name); }
                    if (exists == null) { collection.Insert(entity); Log.Verbose("Inserted in local db as version  " + entity._version + " " + entity._type + " " + entity.name); }
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _backingFieldValues.Remove("_disabledirty");
                Log.Debug("Save::DisableWatch " + wasDisableWatch);
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
                        if (exists != null) { collection.Update(entity); Log.Verbose("Updated in local db as version " + entity._version + " " + entity._type + " " + entity.name); }
                        if (exists == null) { collection.Insert(entity); Log.Verbose("Inserted in local db as version  " + entity._version + " " + entity._type + " " + entity.name); }
                    }
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
                lock (savelock)
                {
                    var exists = collection.FindById(_id);
                    if (exists != null) { collection.Delete(entity._id); Log.Verbose("Deleted in local db as version " + entity._version + " " + entity._type + " " + entity.name); }
                }
            }
        }
    }
}
