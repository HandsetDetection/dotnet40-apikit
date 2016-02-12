using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace HandsetDetectionAPI
{
    public class HdStore : HdBase
    {
        public string Dirname = "hd40store";
        string _path = "";
        public static string Directory = "";
        public string StoreDirectory
        {
            get
            {
                if (!string.IsNullOrEmpty(Directory) && !System.IO.Directory.Exists(Directory))
                {
                    System.IO.Directory.CreateDirectory(Directory);
                }

                return Directory;
            }
        }
        private HdCache _cache = null;

        /***        
         * 
         * Singleton object creation code from http://csharpindepth.com/Articles/General/Singleton.aspx
         * **/
        private static readonly HdStore instance = new HdStore();

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static HdStore()
        {

        }

        private HdStore()
        {
            _path = ApplicationRootDirectory;
            Directory = _path + "\\" + Dirname;
            _cache = new HdCache();
        }

        public static HdStore Instance
        {
            get
            {
                return instance;
            }
        }

        /// <summary>
        /// Sets the path to the root directory for storage operations, optionally creating the storage directory in it.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="isCreateDirectory"></param>
        public void SetPath(string path = null, bool isCreateDirectory = false)
        {
            this._path = string.IsNullOrEmpty(path) ? AppDomain.CurrentDomain.BaseDirectory : path;//dirname(__FILE__)
            Directory = _path + "\\" + Dirname;
            Config["filesdir"] = path;
            if (!isCreateDirectory) return;
            if (System.IO.Directory.Exists(StoreDirectory)) return;
            try
            {
                System.IO.Directory.CreateDirectory(StoreDirectory);
            }
            catch (Exception)
            {
                throw new Exception("Error : Failed to create storage directory at (" + StoreDirectory + "). Check permissions.");


            }
        }

        /// <summary>
        /// Write data to cache & disk
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool Write(string key, Dictionary<string, dynamic> data)
        {
            if (!data.Any())
            {
                return false;
            }

            if (!store(key, data))
            {
                return false;
            }

            return _cache.Write(key, data).Any();

        }

        /// <summary>
        ///  Store data to disk
        /// </summary>
        /// <param name="key">key The search key (becomes the filename .. so keep it alphanumeric)</param>
        /// <param name="data">data Data to persist (will be persisted in json format)</param>
        /// <returns> true on success, false otherwise</returns>
        public bool store(string key, Dictionary<string, dynamic> data)
        {
            string jsonstr = Jss.Serialize(data);
            try
            {
                File.WriteAllText(StoreDirectory + "//" + key + ".json", jsonstr);
            }
            catch (Exception)
            {

                return false;
            }
            return true;
        }

        /// <summary>
        /// Read $data, try cache first
        /// </summary>
        /// <param name="key">Key to search for</param>
        /// <returns> boolean true on success, false</returns>
        public T Read<T>(string key)
        {
            var reply = _cache.Read<T>(key);
            if (reply != null)
                return reply;

            reply = Fetch<T>(key);
            if (reply != null)
            {
                _cache.Write(key, reply);
                return reply;
            }
            else
            {
                return default(T);
            }

        }

        /// <summary>
        ///     Fetch data from disk
        /// </summary>
        /// <param name="key">key</param>
        /// <returns></returns>

        public T Fetch<T>(string key)
        {
            try
            {
                string jsonText = File.ReadAllText(StoreDirectory + "//" + key + ".json");
                return !string.IsNullOrEmpty(jsonText) ? Jss.Deserialize<T>(jsonText) : default(T);

            }
            catch (Exception)
            {

                return default(T);
            }

        }

        /// <summary>
        /// Returns all devices inside one giant array
        /// Used by localDevice* functions to iterate over all devies
        /// </summary>
        /// <returns>array All devices in one giant assoc array</returns>
        public Dictionary<string, dynamic> FetchDevices()
        {
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>();
            List<Dictionary<string, dynamic>> dicList = new List<Dictionary<string, dynamic>>();
            try
            {
                string[] filePaths = System.IO.Directory.GetFiles(StoreDirectory, "Device*.json");
                dicList.AddRange(from item in filePaths select File.ReadAllText(item) into jsonText where !string.IsNullOrEmpty(jsonText) select Jss.Deserialize<Dictionary<string, dynamic>>(jsonText));
                data["devices"] = dicList;
                return data;
            }
            catch (Exception ex)
            {
                Reply = new Dictionary<string, dynamic>();
                SetError(1, "Exception : " + ex.Message + " " + ex.StackTrace);
            }
            return null;
        }

        /// <summary>
        /// Moves a json file into storage.
        /// </summary>
        /// <param name="srcAbsName">srcAbsName The fully qualified path and file name eg /tmp/sjjhas778hsjhh</param>
        /// <param name="destName">destName The key name inside the cache eg Device_19.json</param>
        /// <returns>true on success, false otherwise</returns>
        public bool MoveIn(string srcAbsName, string destName)
        {
            // Move the file.
            try
            {
                File.Move(srcAbsName, destName);
                return true;

            }
            catch (Exception)
            {

                return false;
            }

        }

        /// <summary>
        /// Cleans out the store - Use with caution
        /// </summary>
        /// <returns>true on success, false otherwise</returns>
        public bool Purge()
        {
            string[] filePaths = System.IO.Directory.GetFiles(StoreDirectory, "*.json");
            foreach (string item in filePaths)
            {
                File.Delete(item);
            }

            return _cache.Purge();
        }
    }
}
