using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using System.Web.Script.Serialization;

namespace HandsetDetectionAPI
{
    public class HdCache
    {
        private static int _maxJsonLength = 40000000;
        string _prefix = "hd4_";
        //int duration = 7200;
        readonly MemoryCache _myCache = MemoryCache.Default;
        CacheItemPolicy _policy = new CacheItemPolicy();
        JavaScriptSerializer _jss = new JavaScriptSerializer { MaxJsonLength = _maxJsonLength };

        public HdCache()
        {
            _policy.AbsoluteExpiration = new DateTimeOffset(DateTime.Now.AddHours(24));
        }

        public T Write<T>(string key, T value)
        {

            try
            {
                if (value != null && key != "")
                {
                    // var count = _myCache.Count();
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


                if (fromCache != null)
                {
                    T item = (T)Convert.ChangeType(fromCache, typeof(T));
                    return item.Clone();
                }
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

    /// <summary>  
    /// Provides a method for performing a deep copy of an object.  
    /// Binary Serialization is used to perform the copy.  
    /// </summary>  
    public static class ObjectCopier
    {
        /// <summary>  
        /// Perform a deep Copy of the object.  
        /// </summary>  
        /// <typeparam name="T">The type of object being copied.</typeparam>  
        /// <param name="source">The object instance to copy./// <returns>The copied object.</returns>  
        public static T Clone<T>(this T source)
        {
            if (!typeof(T).IsSerializable)
            {
                throw new ArgumentException("The type must be serializable.", "source");
            }

            // Don't serialize a null object, simply return the default for that object  
            if (Object.ReferenceEquals(source, null))
            {
                return default(T);
            }

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                formatter.Serialize(stream, source);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }
    }
}
