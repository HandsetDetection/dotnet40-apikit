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
    public class HDStore : HDBase
    {
        public string dirname = "hd40store";
        string path = "";
        public static string directory = "";
        public string StoreDirectory { get { return directory; } }
        private HDCache _Cache = null;

        /***        
         * 
         * Singleton object creation code from http://csharpindepth.com/Articles/General/Singleton.aspx
         * **/
        private static readonly HDStore instance = new HDStore();

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static HDStore()
        {

        }

        private HDStore()
        {
            this.path = ApplicationRootDirectory;
            directory = this.path + "\\" + this.dirname;
            this._Cache = new HDCache();
        }

        public static HDStore Instance
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
        /// <param name="IsCreateDirectory"></param>
        public void setPath(string path = null, bool IsCreateDirectory = false)
        {
            this.path = string.IsNullOrEmpty(path) ? AppDomain.CurrentDomain.BaseDirectory : path;//dirname(__FILE__)
            directory = this.path + "\\" + this.dirname;
            config["filesdir"] = path;
            if (IsCreateDirectory)
            {

                if (!Directory.Exists(this.StoreDirectory))
                {
                    try
                    {
                        Directory.CreateDirectory(this.StoreDirectory);
                    }
                    catch (Exception)
                    {
                        throw new Exception("Error : Failed to create storage directory at (" + this.StoreDirectory + "). Check permissions.");


                    }
                }
            }
        }

        /// <summary>
        /// Write data to cache & disk
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool write(string key, Dictionary<string, dynamic> data)
        {
            if (!data.Any())
            {
                return false;
            }

            if (!store(key, data))
            {
                return false;
            }

            return this._Cache.write(key, data).Any();

        }

        /// <summary>
        ///  Store data to disk
        /// </summary>
        /// <param name="key">key The search key (becomes the filename .. so keep it alphanumeric)</param>
        /// <param name="data">data Data to persist (will be persisted in json format)</param>
        /// <returns> true on success, false otherwise</returns>
        public bool store(string key, Dictionary<string, dynamic> data)
        {
            var jss = new JavaScriptSerializer();
            jss.MaxJsonLength = this.maxJsonLength;

            string jsonstr = jss.Serialize(data);
            try
            {
                System.IO.File.WriteAllText(this.StoreDirectory + "//" + key + ".json", jsonstr);

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
        public Dictionary<string, dynamic> read(string key)
        {
            Dictionary<string, dynamic> reply = this._Cache.read(key);
            if (reply != null && reply.Any())
                return reply;

            reply = this.fetch(key);
            if (reply != null && reply.Any())
            {
                this._Cache.write(key, reply);
                return reply;
            }
            else
            {
                return null;
            }

        }
   
       /// <summary>
       ///     Fetch data from disk
       /// </summary>
       /// <param name="key">key</param>
       /// <returns></returns>
            
        public Dictionary<string, dynamic> fetch(string key)
        {
            var jss = new JavaScriptSerializer();
            jss.MaxJsonLength = this.maxJsonLength;

            string jsonText = System.IO.File.ReadAllText(this.StoreDirectory + "//" + key + ".json");
            if (string.IsNullOrEmpty(jsonText))
            {
                return null;
            }
            return jss.Deserialize<Dictionary<string, dynamic>>(jsonText);
        }

        /// <summary>
        /// Returns all devices inside one giant array
        /// Used by localDevice* functions to iterate over all devies
        /// </summary>
        /// <returns>array All devices in one giant assoc array</returns>
        public Dictionary<string, dynamic> fetchDevices()
        {
            var jss = new JavaScriptSerializer();
            jss.MaxJsonLength = this.maxJsonLength;
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>();
            List<Dictionary<string, dynamic>> dicList = new List<Dictionary<string, dynamic>>();
            try
            {
                string[] filePaths = Directory.GetFiles(this.StoreDirectory, "Device*.json");
                foreach (var item in filePaths)
                {
                    string jsonText = System.IO.File.ReadAllText(item);
                    if (string.IsNullOrEmpty(jsonText))
                    {
                        continue;
                    }
                    dicList.Add(jss.Deserialize<Dictionary<string, dynamic>>(jsonText));
                }
                data["devices"] = dicList;
                return data;
            }
            catch (Exception ex)
            {
                reply = new Dictionary<string, dynamic>();
                this.setError(1, "Exception : " + ex.Message + " " + ex.StackTrace);
            }
            return null;
        }

        /// <summary>
        /// Moves a json file into storage.
        /// </summary>
        /// <param name="srcAbsName">srcAbsName The fully qualified path and file name eg /tmp/sjjhas778hsjhh</param>
        /// <param name="destName">destName The key name inside the cache eg Device_19.json</param>
        /// <returns>true on success, false otherwise</returns>
        public bool moveIn(string srcAbsName, string destName)
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
        public bool purge()
        {
            string[] filePaths = Directory.GetFiles(this.StoreDirectory, "*.json");
            foreach (var item in filePaths)
            {
                File.Delete(item);
            }

            return this._Cache.purge();
        }
    }
}
