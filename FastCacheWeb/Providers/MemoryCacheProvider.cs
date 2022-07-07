using Microsoft.Extensions.Caching.Memory;

namespace FastCacheWeb.Providers
{
    public class MemoryCacheProvider : ICacheProvider
    {
        private readonly IMemoryCache _memoryCache;

        public MemoryCacheProvider(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public bool Contains(string key)
        {
            return _memoryCache.TryGetValue(key, out _);
        }

        public object? Get(string key)
        {
            return _memoryCache.TryGetValue(key, out var o) ? o : null;
        }

        public void Put(string key, object result, int duration, bool forever)
        {
            if (forever)
            {
                _memoryCache.Set(key, result);
                return;
            }

            if (duration <= 0)
            {
                throw new ArgumentException("Duration cannot be less or equal to zero", nameof(duration));
            }

            _memoryCache.Set(key, result, TimeSpan.FromMilliseconds(duration));
        }
    }
}