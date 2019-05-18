//using System;
//using System.Runtime.Caching;
//using System.Xml;

//namespace OpenRPA.Store
//{
//    /// <summary>
//    /// Persists instance data to the default memory cache. This is useful for unit tests
//    /// and debugging.
//    /// </summary>
//    public class MemoryCacheInstanceStore : xmlInstanceStoreBase
//    {
//        public MemoryCacheInstanceStore(Guid storeId) : base(storeId) { }

//        public override void Save(Guid instanceId, Guid storeId, XmlDocument doc)
//        {
//            var cacheKey = GetCacheKey(instanceId, storeId);
//            if (MemoryCache.Default.Contains(cacheKey))
//                MemoryCache.Default.Remove(cacheKey);

//            // Cache never expires
//            var cachePolicy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.MaxValue };
//            MemoryCache.Default.Add(cacheKey, doc, cachePolicy);
//        }
//        public override XmlDocument Load(Guid instanceId, Guid storeId)
//        {
//            var cacheKey = GetCacheKey(instanceId, storeId);
//            var instanceData = MemoryCache.Default[cacheKey] as XmlDocument;
//            if (instanceData == null)
//                throw new Exception("Cached instance not found.");

//            return instanceData;
//        }
//        private string GetCacheKey(Guid instanceId, Guid storeId)
//        {
//            return "WorkflowInstance_" + instanceId + "_" + storeId;
//        }
//    }
//}