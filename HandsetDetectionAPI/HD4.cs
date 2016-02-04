using Ionic.Zip;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;

namespace HandsetDetectionAPI
{
    public class Hd4 : HdBase
    {
        public bool UseLocal
        {
            get
            {
                return Convert.ToBoolean(Config["use_local"]);
            }
            set
            {
                Config["use_local"] = value.ToString();
            }
        }

        public bool Geoip
        {
            get
            {
                return Convert.ToBoolean(Config["geoip"]);
            }
            set
            {
                Config["geoip"] = value.ToString();
            }
        }


        string _configFile = "/hdUltimateConfig.json";

        HdStore _store;
        HdCache _cache = null;
        HdDevice _device = null;
        private HttpRequest _request;
        public void CleanUp() { Rawreply = ""; Reply = new Dictionary<string, dynamic>(); }
        public string GetLog() { return Log; }
        public string GetError() { return Error; }



        private void AddKey(string key, string value)
        {
            key = key.ToLower();
            if (DetectRequest.ContainsKey(key))
            {
                DetectRequest.Remove(key);
            }
            DetectRequest.Add(key, value);
        }

        /// <summary>
        /// This is the main constructor for the class HD4
        /// </summary>
        /// <param name="request">Curret Request Object</param>
        /// <param name="configuration">config can be an array of config options or a fully qualified path to an alternate config file.</param>
        public Hd4(HttpRequest request, dynamic configuration = null)
        {
            _request = request;
            if (configuration != null && configuration is IDictionary)
            {
                foreach (KeyValuePair<string, string> item in (Dictionary<string, string>)configuration)
                {
                    if (Config.ContainsKey(item.Key))
                    {
                        Config[item.Key] = item.Value;
                    }
                    else
                    {
                        Config.Add(item.Key, item.Value);
                    }
                }
            }
            else if (configuration != null && configuration is string && File.Exists(ApplicationRootDirectory + configuration))
            {
                AddConfigSettingFromFile(ApplicationRootDirectory + configuration);
            }
            else if (!File.Exists(ApplicationRootDirectory + _configFile))
            {
                throw new Exception("Error : Invalid config file and no config passed to constructor");
            }
            else
            {
                AddConfigSettingFromFile(ApplicationRootDirectory + _configFile);
            }

            _store = HdStore.Instance;
            _store.SetPath(Config["filesdir"], true);

            _cache = new HdCache();
            _device = new HdDevice();

            Setup();
        }

        /// <summary>
        /// Read Setting from config file and add or updet in "config" object
        /// </summary>
        /// <param name="configFile"></param>
        private void AddConfigSettingFromFile(string configFile)
        {
            Dictionary<string, string> hdConfig = new Dictionary<string, string>();

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string jsonText = File.ReadAllText(configFile);
            hdConfig = serializer.Deserialize<Dictionary<string, string>>(jsonText);

            foreach (KeyValuePair<string, string> item in hdConfig)
            {
                if (Config.ContainsKey(item.Key))
                {
                    Config[item.Key] = item.Value;
                }
                else
                {
                    Config.Add(item.Key, item.Value);
                }
            }
        }

        /// <summary>
        /// Initialize inital properties
        /// </summary>
        void Setup()
        {
            Reply = new Dictionary<string, dynamic>();
            RawReply = new Dictionary<string, dynamic>();
            DetectRequest = new Dictionary<string, dynamic>();

            Regex reg = new Regex("^x|^http", RegexOptions.IgnoreCase);
            foreach (string header in _request.Headers)
            {
                if (reg.IsMatch(header))
                {
                    AddKey(header.ToLower(), _request[header]);
                }
            }
            AddKey("user-agent", _request.UserAgent);
            AddKey("ipaddress", _request.UserHostAddress);
            AddKey("request_uri", _request.Url.ToString());

            if (!UseLocal && Geoip)
            {
                // Ip address only used in cloud mode
                DetectRequest["ipaddress"] = _request.ServerVariables["REMOTE_ADDR"] != null ? _request.ServerVariables["REMOTE_ADDR"] : null;
            }
            DetectRequest["Cookie"] = null;
        }

        /// <summary>
        /// List all known vendors
        /// </summary>
        /// <returns>true on success, false otherwise. Use getRawReply to inspect results on success.</returns>
        public bool DeviceVendors()
        {
            // resetLog();
            try
            {
                if (UseLocal)
                {

                    return _device.LocalDeviceVendors();
                }
                else
                {
                    return Remote("/device/vendors", null);
                }
            }
            catch (Exception ex)
            {
                SetError(299, "Exception : " + ex.Message + " " + ex.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// List all models for a given vendor
        /// </summary>
        /// <param name="vendor">vendor The device vendor eg Apple</param>
        /// <returns>true on success, false otherwise. Use getRawReply to inspect results on success</returns>
        public bool DeviceModels(string vendor)
        {
            // resetLog();
            try
            {
                if (UseLocal)
                {
                    return _device.LocalDeviceModels(vendor);
                }
                else
                {
                    return Remote("/device/models/" + vendor, null);
                }
            }
            catch (Exception ex)
            {
                SetError(299, "Exception : " + ex.Message + " " + ex.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// Find properties for a specific device
        /// </summary>
        /// <param name="vendor">vendor The device vendor eg. Nokia</param>
        /// <param name="model">model The deviec model eg. N95</param>
        /// <returns>true on success, false otherwise. Use getReply to inspect results on success</returns>
        public bool DeviceView(string vendor, string model)
        {
            try
            {
                if (UseLocal)
                {
                    return _device.LocalDeviceView(vendor, model);
                }
                else
                {
                    return Remote(string.Format("/device/view/{0}/{1}", vendor, model), null);
                }
            }
            catch (Exception ex)
            {
                SetError(299, "Exception : " + ex.Message + " " + ex.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// Find which devices have property 'X'.
        /// </summary>
        /// <param name="key">Property to inquire about eg 'network', 'connectors' etc...</param>
        /// <param name="value">true on success, false otherwise. Use getReply to inspect results on success. </param>
        /// <returns></returns>
        public bool DeviceWhatHas(string key, string value)
        {
            try
            {
                if (UseLocal)
                {
                    return _device.LocalWhatHas(key, value);
                }
                else
                {
                    return Remote(string.Format("/device/whathas/{0}/{1}", key, value), null);
                }
            }
            catch (Exception ex)
            {
                SetError(299, "Exception : " + ex.Message + " " + ex.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// Device Detect
        /// </summary>
        /// <param name="data">Data for device detection : HTTP Headers usually</param>
        /// <returns>true on success, false otherwise. Use getReply to inspect results on success.</returns>
        public bool DeviceDetect(Dictionary<string, dynamic> data = null)
        {
            int id = 0;
            if (data == null || !data.Any() || !data.ContainsKey("id"))
            {
                id = Convert.ToInt32(Config["site_id"]);
            }
            else
            {
                id = Convert.ToInt32(data["id"]);
            }

            Dictionary<string, dynamic> requestBody = new Dictionary<string, dynamic>();
            foreach (KeyValuePair<string, dynamic> item in data)
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
            if (Cacherequests)
            {
                IOrderedEnumerable<dynamic> headersKeys = requestBody.Values.Select(c => c).OrderBy(c => c);
                fastKey = Jss.Serialize(headersKeys).Replace(" ", "");
                Dictionary<string, dynamic> objReply = _cache.Read(fastKey);
                if (objReply.Count > 0)
                {
                    Reply = objReply;
                    SetRawReply();
                    return SetError(0, "OK");
                }
            }

            try
            {
                bool result = false;
                if (UseLocal)
                {
                    result = _device.LocalDetect(requestBody);
                    // Log unknown headers if enabled
                    SetError(_device.GetStatus(), _device.GetMessage());
                }
                else
                {
                    result = Remote(string.Format("/device/detect/{0}", id), requestBody);
                }
                if (Cacherequests)
                {
                    _cache.Write(fastKey, GetReply());
                }
                return result;
            }
            catch (Exception ex)
            {
                SetError(299, "Exception : " + ex.Message + " " + ex.StackTrace);
                return false;
            }
        }

        /// <summary>
        ///  Fetch an archive from handset detection which contains all the device specs and matching trees as individual json files.
        /// </summary>
        /// <returns>hd_specs data on success, false otherwise</returns>
        public dynamic DeviceFetchArchive()
        {
            IsDownloadableFiles = true;
            if (!Remote("device/fetcharchive", null, "zip"))
                return false;

            string data = GetRawReply();

            if (!data.Any())
                return SetError(299, "Error : FetchArchive failed. Bad Download. File is zero length");
            else if (data.Length < 9000000)
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                Dictionary<string, string> trythis = serializer.Deserialize<Dictionary<string, string>>(data);
                if (trythis.Count > 0 && trythis.ContainsKey("status") && trythis.ContainsKey("message"))
                    return SetError(Convert.ToInt32(trythis["status"]), trythis["message"]);
            }

            return installArchive(Config["filesdir"], "ultimate.zip");
        }

        /// <summary>
        /// Community Fetch Archive - Fetch the community archive version
        /// </summary>
        /// <returns>hd_specs data on success, false otherwise</returns>
        public dynamic CommunityFetchArchive()
        {
            IsDownloadableFiles = true;
            if (!Remote("community/fetcharchive", null, "zip", false))
                return false;

            string data = GetRawReply();

            if (string.IsNullOrEmpty(data))
                return SetError(299, "Error : FetchArchive failed. Bad Download. File is zero length");
            else if (data.Length < 900000)
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                Dictionary<string, string> trythis = serializer.Deserialize<Dictionary<string, string>>(data);
                if (trythis.Count > 0 && trythis.ContainsKey("status") && trythis.ContainsKey("message"))
                    return SetError(Convert.ToInt32(trythis["status"]), trythis["message"]);
            }


            return installArchive(Config["filesdir"], "communityTest.zip");
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
                List<string> directoryArray = directoryPath.Replace("/", "\\").Split(new String[] { "\\" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                string directoryString = "";
                foreach (string item in directoryArray)
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

            fileName = Config["filesdir"] + "\\" + fileName;

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
                c = Reader.Read(buff, 0, 1024);
                for (int i = 0; i < c; i++)
                    bw.Write(buff[i]);
            }
            bw.Close();
            string directoryPath = filePath.Substring(0, filePath.LastIndexOf("\\"));

            if (!directoryPath.Contains(_store.Dirname))
            {
                directoryPath += "\\" + _store.Dirname;
            }

            using (ZipFile zip = ZipFile.Read(filePath))
            {
                zip.ToList().ForEach(entry =>
                {
                    entry.FileName = Path.GetFileName(entry.FileName.Replace(':', '_'));
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
        public bool IsHelperUseful(Dictionary<string, dynamic> headers)
        {
            return _device.IsHelperUseful(headers);
        }
    }
}
