using System;
#if NET40
using System.Runtime.Caching;
using System.Collections.Specialized;
#else
using Microsoft.Extensions.Caching.Memory;
#endif

namespace Octostache.Templates
{
    public class ItemCache<T> where T: class
    {
        MemoryCache cache;
        readonly object nullItem = new object();

        public ItemCache(string name, int megabyteLimit, TimeSpan slidingExpiration)
        {
            Name = name;
            MegabyteLimit = megabyteLimit;
            SlidingExpiration = slidingExpiration;
            
#if NET40
            cache = new MemoryCache(Name, new NameValueCollection() { { "CacheMemoryLimitMegabytes", MegabyteLimit.ToString() } });
#else
            cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = MegabyteLimit * 1024 * 1024 });
#endif      
        }
        
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string Name { get; }
        // ReSharper disable once MemberCanBePrivate.Global
        public int MegabyteLimit { get; }
        // ReSharper disable once MemberCanBePrivate.Global
        public TimeSpan SlidingExpiration { get; }
        
        // ReSharper disable once MemberCanBePrivate.Global
        public void Add(string key, T? item)
        {
#if NET40
            cache.Set(key, item ?? nullItem, new CacheItemPolicy { SlidingExpiration = SlidingExpiration });
#else
            // NOTE: Setting the size to the string length, is not quite right, but close enough for our purposes. 
            cache.Set(key, item ?? nullItem, new MemoryCacheEntryOptions { SlidingExpiration = SlidingExpiration, Size = item?.ToString().Length ?? 0 });
#endif
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public T? Get(string key)
        {
            return cache.Get(key) as T;
        }

        public T? GetOrAdd(string key, Func<T?> getItem)
        {
            var obj = cache.Get(key);
           
            if (obj != null)
            {
                return obj as T;
            }

            var item = getItem();
            Add(key, item);

            return item;
        }

        public void Clear()
        {
#if NET40
            cache = new MemoryCache(Name, new NameValueCollection() { { "CacheMemoryLimitMegabytes", MegabyteLimit.ToString() } });
#else
            cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = MegabyteLimit * 1024 * 1024 });
#endif
        }
    }
}