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
    public class HDCache
    {
        private int maxJsonLength = 40000000;
        string prefix = "hd4-";
        int duration = 7200;
        ObjectCache myCache;
        CacheItemPolicy policy = new CacheItemPolicy();

        public HDCache()
        {
            policy.AbsoluteExpiration = new DateTimeOffset(DateTime.Now.AddHours(24));
            NameValueCollection CacheSettings = new NameValueCollection(3);
            CacheSettings.Add("CacheMemoryLimitMegabytes", Convert.ToString(200));
            this.myCache = MemoryCache.Default;
        }

        public Dictionary<string, dynamic> write(string key, dynamic value)
        {
            if (value != null && key != "")
            {
                var jss = new JavaScriptSerializer();
                jss.MaxJsonLength = this.maxJsonLength;
                string storethis = jss.Serialize(value);
                this.myCache.Set(this.prefix + key, storethis, policy);
                return value;
            }
            else
            {
                return null;
            }
        }

        public Dictionary<string, dynamic> read(string key)
        {
            try
            {
                string fromCache = this.myCache.Get(this.prefix + key) as string;
                var jss = new JavaScriptSerializer();
                jss.MaxJsonLength = this.maxJsonLength;
                return jss.Deserialize<Dictionary<string, dynamic>>(fromCache);
            }
            catch (Exception ex)
            {
                // Not in cache
                return null;
            }
        }

        public bool purge()
        {
            try
            {
                foreach (var item in this.myCache.Select(kvp => kvp.Key))
                {
                    this.myCache.Remove(item);
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
