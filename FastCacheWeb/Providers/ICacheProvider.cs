namespace FastCacheWeb.Providers
{
    public interface ICacheProvider
    {
        bool Contains(string key);
        void Put(string key, object value, int duration, bool forever);
        object? Get(string key);
    }
}