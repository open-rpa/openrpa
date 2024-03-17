using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA
{
    public static class StorageProvider
    {
        public async static Task<T[]> FindAll<T>() where T : apibase
        {
            T[] results;
            foreach (var s in Plugins.Storages)
            {
                results = await s.FindAll<T>();
                if(results.Length > 0) return results;
            }
            return Array.Empty<T>();
        }
        public async static Task<T> FindById<T>(string id) where T : apibase
        {
            T result;
            foreach (var s in Plugins.Storages)
            {
                result = await s.FindById<T>(id);
                if (result != null) return result;
            }
            return default(T);
        }
        public async static Task<T> Insert<T>(T item) where T : apibase
        {
            T result = item;
            foreach (var s in Plugins.Storages)
            {
                result = await s.Insert(result);
            }
            return result;
        }
        public async static Task<T> Update<T>(T item) where T : apibase
        {
            T result = item;
            foreach (var s in Plugins.Storages)
            {
                result = await s.Update(result);
            }
            return result;
        }
        public async static Task Delete<T>(string id) where T : apibase
        {
            foreach (var s in Plugins.Storages)
            {
                await s.Delete<T>(id);
            }
        }
    }
}
