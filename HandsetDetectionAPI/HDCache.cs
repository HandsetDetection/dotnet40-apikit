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

        public Dictionary<string, dynamic> Write(string key, dynamic value)
        {
            if (value != null && key != "")
            {
                string storethis = _jss.Serialize(value);
                _myCache.Set(_prefix + key, storethis, _policy);
                return value;
            }
            else
            {
                return null;
            }
        }

        public Dictionary<string, dynamic> Read(string key)
        {
            try
            {
                string fromCache = _myCache.Get(_prefix + key) as string;
                if (fromCache != null) return _jss.Deserialize<Dictionary<string, dynamic>>(fromCache);
            }
            catch (Exception)
            {
                // Not in cache

            }
            return null;
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
