using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace OpenRPA.Store
{
    public class XMLFileInstanceStore : CustomInstanceStoreBase
    {
        string _basePath;
        private static Guid storeId = new Guid("0bfcc3a5-3c77-421b-b575-73533563a1f3");

        /// <summary>
        /// Create store that persists instance state to XML file on the filesystem with a path of:
        ///    {basePath}\{storeId}\{instance}
        /// </summary>
        public XMLFileInstanceStore(string BasePath) : base(storeId)
        {

            //_basePath = basePath;
            //var p = System.Diagnostics.Process.GetCurrentProcess();
            //_basePath = System.IO.Path.GetDirectoryName(p.MainModule.FileName);
            // _basePath = socketService.instance.settings.getpath("state", true);
            _basePath = BasePath;
        }

        private static object _lock = new object();
        //public override void Save(Guid instanceId, Guid storeId, XmlDocument doc)
        //{
        //    try
        //    {
        //        var filename = getFilePath(instanceId, storeId);
        //        var directoryName = new FileInfo(filename).Directory.FullName;
        //        Directory.CreateDirectory(directoryName);
        //        lock (_lock)
        //        {
        //            File.WriteAllText(filename, doc.InnerXml);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Trace.WriteLine("XMLFileInstanceStore.save: " + ex.Message);
        //    }
        //}
        //public override XmlDocument Load(Guid instanceId, Guid storeId)
        //{
        //    try
        //    {
        //        var filename = getFilePath(instanceId, storeId);
        //        if (!System.IO.File.Exists(filename))
        //        {
        //            System.Diagnostics.Trace.WriteLine("Cannot resume workflow instanse with id " + instanceId.ToString() + " cannot find file " + filename);
        //            throw new ArgumentException("Cannot resume workflow instanse with id " + instanceId.ToString() + " cannot find file " + filename);
        //            //return null;
        //        }
        //        var xmlDoc = new XmlDocument();
        //        //xmlDoc.Load(filename);
        //        lock (_lock)
        //        {
        //            xmlDoc.Load(new MemoryStream(File.ReadAllBytes(filename)));
        //        }
        //        return xmlDoc;
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Trace.WriteLine("XMLFileInstanceStore.save: " + ex.Message);
        //    }
        //    return null;
        //}
        public override void Save(Guid instanceId, Guid storeId, string doc)
        {
            try
            {
                var filename = getFilePath(instanceId, storeId);
                var directoryName = new FileInfo(filename).Directory.FullName;
                Directory.CreateDirectory(directoryName);
                lock (_lock)
                {
                    File.WriteAllText(filename, doc);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("XMLFileInstanceStore.save: " + ex.Message);
            }
        }
        public override string Load(Guid instanceId, Guid storeId)
        {
            try
            {
                var filename = getFilePath(instanceId, storeId);
                if (!System.IO.File.Exists(filename))
                {
                    System.Diagnostics.Trace.WriteLine("Cannot resume workflow instanse with id " + instanceId.ToString() + " cannot find file " + filename);
                    throw new ArgumentException("Cannot resume workflow instanse with id " + instanceId.ToString() + " cannot find file " + filename);
                    //return null;
                }
                //var xmlDoc = new XmlDocument();
                //xmlDoc.Load(filename);
                string json = null;
                lock (_lock)
                {
                    //xmlDoc.Load(new MemoryStream(File.ReadAllBytes(filename)));
                    json = File.ReadAllText(filename);
                }
                return json;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("XMLFileInstanceStore.save: " + ex.Message);
            }
            return null;
        }
        private string getFilePath(Guid instanceId, Guid storeId)
        {
            //return Path.Combine(_basePath, storeId.ToString(), instanceId.ToString() + ".xml");
            return Path.Combine(_basePath, instanceId.ToString() + ".xml");
        }

        internal static void cleanup(Guid instanceId)
        {
            //throw new NotImplementedException();
        }
    }
}
