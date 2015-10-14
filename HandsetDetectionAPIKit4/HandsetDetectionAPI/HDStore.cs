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
        string directory = "";
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
            this.path = AppDomain.CurrentDomain.BaseDirectory;
            this.directory = this.path + "\\" + this.dirname;
            this._Cache = new HDCache();
        }

        public static HDStore Instance
        {
            get
            {
                return instance;
            }
        }


        public void setPath(string path = null, bool IsCreateDirectory = false)
        {
            this.path = string.IsNullOrEmpty(path) ? AppDomain.CurrentDomain.BaseDirectory : path;//dirname(__FILE__)
            this.directory = this.path + "\\" + this.dirname;

            if (IsCreateDirectory)
            {

                if (!Directory.Exists(this.directory))
                {
                    try
                    {
                        Directory.CreateDirectory(this.directory);
                    }
                    catch (Exception)
                    {
                        throw new Exception("Error : Failed to create storage directory at (" + this.directory + "). Check permissions.");


                    }
                }
            }
        }

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
                System.IO.File.WriteAllText(this.directory + "//" + key + ".json", jsonstr);

            }
            catch (Exception)
            {

                return false;
            }
            return true;
        }
        public Dictionary<string, dynamic> read(string key)
        {
            Dictionary<string, dynamic> reply = this._Cache.read(key);
            if (reply.Any())
                return reply;

            reply = this.fetch(key);
            if (reply.Any())
            {
                this._Cache.write(key, reply);
                return reply;
            }
            else
            {
                return null;
            }

        }
        /**
   * Fetch data from disk
   *
   * @param string $key.
   * @reply mixed
   **/
        public Dictionary<string, dynamic> fetch(string key)
        {
            var jss = new JavaScriptSerializer();
            jss.MaxJsonLength = this.maxJsonLength;

            string jsonText = System.IO.File.ReadAllText(this.directory + "//" + key + ".json");
            if (string.IsNullOrEmpty(jsonText))
            {
                return null;
            }
            return jss.Deserialize<Dictionary<string, dynamic>>(jsonText);
        }

        public Dictionary<string, dynamic> fetchDevices()
        {

            var jss = new JavaScriptSerializer();
            jss.MaxJsonLength = this.maxJsonLength;
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>();
            List<Dictionary<string, dynamic>> dicList = new List<Dictionary<string, dynamic>>();
            try
            {
                string[] filePaths = Directory.GetFiles(this.directory, "Device*.json");
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

        public bool purge()
        {
            string[] filePaths = Directory.GetFiles(this.directory, "*.json");
            foreach (var item in filePaths)
            {
                File.Delete(item);
            }

            return this._Cache.purge();
        }
    }
}
