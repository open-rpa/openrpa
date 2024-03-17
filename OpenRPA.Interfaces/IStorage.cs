using OpenRPA.Input;
using OpenRPA.Interfaces.entity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public interface IStorage : IDisposable
    {
        string Name { get; set;  }
        Task Initialize();
        Task<T[]> FindAll<T>() where T : apibase;
        Task<T> FindById<T>(string id) where T : apibase;
        Task<T> Insert<T>(T item) where T : apibase;
        Task<T> Update<T>(T item) where T : apibase;
        Task Delete<T>(string id) where T : apibase;
    }
}
