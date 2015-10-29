using Ionic.Zip;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

namespace HandsetDetectionAPI
{
    public class HD4 : HDBase
    {
        public bool UseLocal
        {
            get
            {
                return config["use_local"];
            }
            set
            {
                config["use_local"] = value;
            }
        }




        bool debug = true;
        string configFile = "/hdUltimateConfig.json";

        HDStore Store;
        HDCache cache = null;
        HDDevice device = null;
        private HttpRequest Request;
        public void cleanUp() { rawreply = ""; reply = new Dictionary<string, dynamic>(); }
        public string getLog() { return this.log; }
        public string getError() { return this.error; }



        private void AddKey(string key, string value)
        {
            key = key.ToLower();
            if (detectRequest.ContainsKey(key))
            {
                this.detectRequest.Remove(key);
            }
            this.detectRequest.Add(key, value);
        }

        /// <summary>
        /// This is the main constructor for the class HD4
        /// </summary>
        /// <param name="request">Curret Request Object</param>
        /// <param name="configuration">config can be an array of config options or a fully qualified path to an alternate config file.</param>
        public HD4(HttpRequest request, dynamic configuration = null)
        {
            this.Request = request;
            if (configuration != null && configuration is IDictionary)
            {
                foreach (var item in (Dictionary<string, dynamic>)configuration)
                {
                    if (config.ContainsKey(item.Key))
                    {
                        config[item.Key] = item.Value;
                    }
                    else
                    {
                        config.Add(item.Key, item.Value);
                    }
                }
            }
            else if (configuration != null && configuration is string && File.Exists(ApplicationRootDirectory + configuration))
            {
                AddConfigSettingFromFile(ApplicationRootDirectory + configuration);
            }
            else if (!File.Exists(ApplicationRootDirectory + configFile))
            {
                throw new Exception("Error : Invalid config file and no config passed to constructor");
            }
            else
            {
                AddConfigSettingFromFile(ApplicationRootDirectory + configFile);
            }

            this.debug = config["debug"];

            this.Store = HDStore.Instance;
            this.Store.setPath(config["filesdir"], true);

            this.cache = new HDCache();
            this.device = new HDDevice();

            this.setup();
        }

        /// <summary>
        /// Read Setting from config file and add or updet in "config" object
        /// </summary>
        /// <param name="configFile"></param>
        private void AddConfigSettingFromFile(string configFile)
        {
            Dictionary<string, dynamic> hdConfig = new Dictionary<string, dynamic>();

            var serializer = new JavaScriptSerializer();
            string jsonText = System.IO.File.ReadAllText(configFile);
            hdConfig = serializer.Deserialize<Dictionary<string, dynamic>>(jsonText);

            foreach (var item in hdConfig)
            {
                if (config.ContainsKey(item.Key))
                {
                    config[item.Key] = item.Value;
                }
                else
                {
                    config.Add(item.Key, item.Value);
                }
            }
        }

        /// <summary>
        /// Initialize inital properties
        /// </summary>
        void setup()
        {
            reply = new Dictionary<string, dynamic>();
            rawReply = new Dictionary<string, dynamic>();
            detectRequest = new Dictionary<string, dynamic>();

            Regex reg = new Regex("^x|^http", RegexOptions.IgnoreCase);
            foreach (string header in Request.Headers)
            {
                if (reg.IsMatch(header))
                {
                    AddKey(header.ToLower(), Request[header]);
                }
            }
            AddKey("user-agent", Request.UserAgent);
            AddKey("ipaddress", Request.UserHostAddress);
            AddKey("request_uri", Request.Url.ToString());

            if (!this.UseLocal && config["geoip"])
            {
                // Ip address only used in cloud mode
                this.detectRequest["ipaddress"] = this.Request.ServerVariables["REMOTE_ADDR"] != null ? this.Request.ServerVariables["REMOTE_ADDR"] : null;
            }
            detectRequest["Cookie"] = null;
        }

        /// <summary>
        /// List all known vendors
        /// </summary>
        /// <returns>true on success, false otherwise. Use getRawReply to inspect results on success.</returns>
        public bool deviceVendors()
        {
            // resetLog();
            try
            {
                if (UseLocal)
                {

                    return device.localDeviceVendors();
                }
                else
                {
                    return Remote("/device/vendors", null);
                }
            }
            catch (Exception ex)
            {
                this.setError(299, "Exception : " + ex.Message + " " + ex.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// List all models for a given vendor
        /// </summary>
        /// <param name="vendor">vendor The device vendor eg Apple</param>
        /// <returns>true on success, false otherwise. Use getRawReply to inspect results on success</returns>
        public bool deviceModels(string vendor)
        {
            // resetLog();
            try
            {
                if (UseLocal)
                {
                    return device.localDeviceModels(vendor);
                }
                else
                {
                    return Remote("/device/models/" + vendor, null);
                }
            }
            catch (Exception ex)
            {
                this.setError(299, "Exception : " + ex.Message + " " + ex.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// Find properties for a specific device
        /// </summary>
        /// <param name="vendor">vendor The device vendor eg. Nokia</param>
        /// <param name="model">model The deviec model eg. N95</param>
        /// <returns>true on success, false otherwise. Use getReply to inspect results on success</returns>
        public bool deviceView(string vendor, string model)
        {
            try
            {
                if (UseLocal)
                {
                    return device.localDeviceView(vendor, model);
                }
                else
                {
                    return Remote(string.Format("/device/view/{0}/{1}", vendor, model), null);
                }
            }
            catch (Exception ex)
            {
                this.setError(299, "Exception : " + ex.Message + " " + ex.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// Find which devices have property 'X'.
        /// </summary>
        /// <param name="key">Property to inquire about eg 'network', 'connectors' etc...</param>
        /// <param name="value">true on success, false otherwise. Use getReply to inspect results on success. </param>
        /// <returns></returns>
        public bool deviceWhatHas(string key, string value)
        {
            try
            {
                if (UseLocal)
                {
                    return device.localWhatHas(key, value);
                }
                else
                {
                    return Remote(string.Format("/device/whathas/{0}/{1}", key, value), null);
                }
            }
            catch (Exception ex)
            {
                this.setError(299, "Exception : " + ex.Message + " " + ex.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// Device Detect
        /// </summary>
        /// <param name="data">Data for device detection : HTTP Headers usually</param>
        /// <returns>true on success, false otherwise. Use getReply to inspect results on success.</returns>
        public bool deviceDetect(Dictionary<string, dynamic> data = null)
        {
            int id = 0;
            if (data == null || data.Count() == 0 || !data.ContainsKey("id"))
            {
                id = Convert.ToInt32(config["site_id"]);
            }
            else
            {
                id = Convert.ToInt32(data["id"]);
            }

            Dictionary<string, dynamic> requestBody = new Dictionary<string, dynamic>();
            foreach (var item in data)
            {
                if (requestBody.ContainsKey(item.Key.ToLower()))
                {
                    requestBody[item.Key.ToLower()] = item.Value;
                }
                else
                {
                    requestBody.Add(item.Key.ToLower(), item.Value);
                }
            }



            string fastKey = "";
            // If caching enabled then check cache
            if (config["cache_requests"])
            {
                var headersKeys = requestBody.Keys.Select(c => c.ToLower()).OrderBy(c => c);
                var serializer = new JavaScriptSerializer();
                fastKey = serializer.Serialize(headersKeys).Replace(" ", "");
                var objReply = this.cache.read(fastKey);
                if (objReply.Count > 0)
                {
                    reply = objReply;
                    setRawReply();
                    return setError(0, "OK");
                }
            }

            try
            {
                if (UseLocal)
                {
                    var result = device.localDetect(requestBody);
                    // Log unknown headers if enabled
                    setError(device.getStatus(), device.getMessage());
                    return result;
                }
                else
                {
                    return Remote(string.Format("/device/detect/{0}", id.ToString()), requestBody);
                }
            }
            catch (Exception ex)
            {
                this.setError(299, "Exception : " + ex.Message + " " + ex.StackTrace);
                return false;
            }
        }

        /// <summary>
        ///  Fetch an archive from handset detection which contains all the device specs and matching trees as individual json files.
        /// </summary>
        /// <returns>hd_specs data on success, false otherwise</returns>
        public dynamic deviceFetchArchive()
        {
            this.isDownloadableFiles = true;
            if (!this.Remote("device/fetcharchive", null, "zip"))
                return false;

            var data = this.getRawReply();

            if (data.Count() == 0)
                return this.setError(299, "Error : FetchArchive failed. Bad Download. File is zero length");
            else if (data.Length < 9000000)
            {
                var serializer = new JavaScriptSerializer();
                var trythis = serializer.Deserialize<Dictionary<string, string>>(data);
                if (trythis.Count > 0 && trythis.ContainsKey("status") && trythis.ContainsKey("message"))
                    return setError(Convert.ToInt32(trythis["status"]), trythis["message"]);
            }

            return installArchive(config["filesdir"], "ultimate.zip");
        }

        /// <summary>
        /// Community Fetch Archive - Fetch the community archive version
        /// </summary>
        /// <returns>hd_specs data on success, false otherwise</returns>
        public dynamic communityFetchArchive()
        {
            this.isDownloadableFiles = true;
            if (!this.Remote("community/fetcharchive", null, "zip", false))
                return false;

            var data = this.getRawReply();

            if (string.IsNullOrEmpty(data))
                return setError(299, "Error : FetchArchive failed. Bad Download. File is zero length");
            else if (data.Length < 900000)
            {
                var serializer = new JavaScriptSerializer();
                var trythis = serializer.Deserialize<Dictionary<string, string>>(data);
                if (trythis.Count > 0 && trythis.ContainsKey("status") && trythis.ContainsKey("message"))
                    return setError(Convert.ToInt32(trythis["status"]), trythis["message"]);
            }


            return installArchive(config["filesdir"], "communityTest.zip");
        }

        /// <summary>
        /// Install an ultimate archive file
        /// </summary>
        /// <param name="file">string file Fully qualified path to file</param>
        /// <returns>boolean true on success, false otherwise</returns>
        public bool installArchive(string directoryPath, string fileName)
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                fileName = ApplicationRootDirectory + "\\" + fileName;
            }
            else
            {
                var directoryArray = directoryPath.Replace("/", "\\").Split(new String[] { "\\" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                var directoryString = "";
                foreach (var item in directoryArray)
                {
                    directoryString += item + "\\";
                    if (DriveInfo.GetDrives().Any(drive => drive.Name.ToLower() == directoryString))
                    {
                        continue;
                    }
                    else
                    {
                        if (!Directory.Exists(directoryString))
                        {
                            Directory.CreateDirectory(directoryString);
                        }
                    }
                }
            }

            fileName = config["filesdir"] + "\\" + fileName;

            // responseStream.Close();
            return installArchive(fileName);
        }

        public bool installArchive(string filePath)
        {
            BinaryWriter bw = new BinaryWriter(new FileStream(filePath, FileMode.Create), Encoding.UTF8);
            byte[] buff = new byte[1024];
            int c = 1;
            while (c > 0)
            {
                c = reader.Read(buff, 0, 1024);
                for (int i = 0; i < c; i++)
                    bw.Write(buff[i]);
            }
            bw.Close();
            var directoryPath = filePath.Substring(0, filePath.LastIndexOf("\\"));

            if (!directoryPath.Contains(Store.dirname))
            {
                directoryPath += "\\" + Store.dirname;
            }

            using (ZipFile zip = ZipFile.Read(filePath))
            {
                zip.ToList().ForEach(entry =>
                {
                    entry.FileName = System.IO.Path.GetFileName(entry.FileName.Replace(':', '_'));
                    entry.Extract(directoryPath, ExtractExistingFileAction.OverwriteSilently);
                });
            }
            return true;
        }

        /// <summary>
        /// This method can indicate if using the js Helper would yeild more accurate results.
        /// </summary>
        /// <param name="headers">headers</param>
        /// <returns>true if helpful, false otherwise.</returns>
        public bool isHelperUseful(Dictionary<string, dynamic> headers)
        {
            return device.isHelperUseful(headers);
        }
    }
}
