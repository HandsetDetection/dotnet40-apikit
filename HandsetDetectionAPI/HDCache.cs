using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using System.Web.Caching;
using System.Web.Script.Serialization;

namespace HandsetDetectionAPI
{
    public class HdCache
    {
        private static int _maxJsonLength = 40000000;
        string _prefix = "hd4-";
        //int duration = 7200;
        ObjectCache _myCache;
        CacheItemPolicy _policy = new CacheItemPolicy();
        JavaScriptSerializer _jss = new JavaScriptSerializer { MaxJsonLength = _maxJsonLength };

        public HdCache()
        {
            _policy.AbsoluteExpiration = new DateTimeOffset(DateTime.Now.AddHours(24));
            NameValueCollection cacheSettings = new NameValueCollection(3)
            {
                {"CacheMemoryLimitMegabytes", Convert.ToString(200)}
            };
            _myCache = MemoryCache.Default;
        }

        public T Write<T>(string key, T value)
        {

            try
            {
                if (value != null && key != "")
                {
                    //  string storethis = _jss.Serialize(value);
                    _myCache.Set(_prefix + key, value, _policy);
                    return value;
                }
                else
                {
                    return default(T);
                }
            }
            catch (Exception)
            {

                throw;
            }

        }

        public T Read<T>(string key)
        {
            try
            {
                object fromCache = _myCache.Get(_prefix + key);
                if (fromCache != null) return (T)Convert.ChangeType(fromCache, typeof(T));
            }
            catch (Exception)
            {
                // Not in cache

            }
            return default(T);
        }

        public bool Purge()
        {
            try
            {
                foreach (string item in _myCache.Select(kvp => kvp.Key))
                {
                    _myCache.Remove(item);
                }
                return true;
            }
            catch (Exception)
            {

                return false;
            }

        }
    }
}
