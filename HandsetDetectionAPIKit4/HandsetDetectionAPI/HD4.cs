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

        string realm = "APIv4";


        Dictionary<string, dynamic> detectRequest = new Dictionary<string, dynamic>();

        string logger = null;
        bool debug = true;
        string configFile = "hdconfig.json";

        HDStore Store;
        HDCache cache = null;
        HDDevice device = null;
        private HttpRequest Request;
        public void cleanUp() { rawreply = ""; this.reply = new Dictionary<string, dynamic>(); }
        public string getLog() { return this.log; }
        public string getError() { return this.error; }


        Dictionary<string, dynamic> tree = new Dictionary<string, dynamic>();

        public HD4(HttpRequest request, dynamic configuration = null)
        {
            this.Request = request;
            if (configuration != null && configuration is IDictionary)
            {
                foreach (var item in (Dictionary<string, dynamic>)configuration)
                {
                    if (this.config.ContainsKey(item.Key))
                    {
                        this.config[item.Key] = item.Value;
                    }
                    else
                    {
                        this.config.Add(item.Key, item.Value);
                    }
                }
            }
            else if (configuration != null && configuration is string && File.Exists(configuration))
            {
                AddConfigSettingFromFile(configuration);
            }
            else if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + configFile))
            {
                throw new Exception("Error : Invalid config file and no config passed to constructor");
            }
            else
            {
                AddConfigSettingFromFile(AppDomain.CurrentDomain.BaseDirectory + configFile);
            }

            this.debug = this.config["debug"];

            this.Store = HDStore.Instance;
            this.Store.setPath(this.config["filesdir"], true);

            this.cache = new HDCache();
            this.device = new HDDevice();

            this.setup();


            //    if (! empty($this->config['use_local']) && ! class_exists('ZipArchive')) {
            //    throw new \Exception('Ultimate detection needs ZipArchive to unzip archive files. Please install this php module.');
            //}
        }

        private void AddConfigSettingFromFile(string config)
        {
            Dictionary<string, dynamic> hdConfig = new Dictionary<string, dynamic>();

            var serializer = new JavaScriptSerializer();
            string jsonText = System.IO.File.ReadAllText(config);
            hdConfig = serializer.Deserialize<Dictionary<string, dynamic>>(jsonText);

            foreach (var item in hdConfig)
            {
                if (this.config.ContainsKey(item.Key))
                {
                    this.config[item.Key] = item.Value;
                }
                else
                {
                    this.config.Add(item.Key, item.Value);
                }
            }
        }

        void setup()
        {
            reply = new Dictionary<string, dynamic>();
            rawReply = new Dictionary<string, dynamic>();
            detectRequest = new Dictionary<string, dynamic>();

            //if (function_exists('apache_request_headers')) {
            //            $this->detectRequest = apache_request_headers();
            //        } else {
            //            // From http://php.net/manual/en/function.apache-request-headers.php
            //            $rx_http = '/\AHTTP_/';
            //            foreach($_SERVER as $key => $val) {
            //                if (preg_match($rx_http, $key) ) {
            //                    $arh_key = preg_replace($rx_http, '', $key);
            //                    $rx_matches = array();
            //                    // do some nasty string manipulations to restore the original letter case
            //                    // this should work in most cases
            //                    $rx_matches = explode('_', $arh_key);
            //                    if (count($rx_matches) > 0 and strlen($arh_key) > 2 ) {
            //                        foreach($rx_matches as $ak_key => $ak_val) $rx_matches[$ak_key] = ucfirst($ak_val);
            //                        $arh_key = implode('-', $rx_matches);
            //                    }
            //                    $this->detectRequest[$arh_key] = $val;
            //                }
            //            }
            //        }


            if (!this.UseLocal && this.config["geoip"])
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


        public bool deviceDetect(Dictionary<string, dynamic> data = null)
        {
            int id = 0;
            if (data == null || data.Count() == 0)
            {
                id = Convert.ToInt32(config["site_id"]);
            }
            else
            {
                id = Convert.ToInt32(data["id"]);
            }

            Dictionary<string, dynamic> requestBody = detectRequest;
            foreach (var item in data)
            {
                requestBody.Add(item.Key, item.Value);
            }

            if (config["cache_requests"])
            {
                if (this.cache.read("").Count > 0)
                {
                    reply = new Dictionary<string, dynamic>();
                    setRawReply();
                    return setError(0, "OK");
                }
            }

            try
            {
                if (UseLocal)
                {
                    return device.localDetect(requestBody);
                }
                else
                {
                    return Remote(string.Format("/device/detect/{0}}", id), null);
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
                return setError(299, "Error : FetchArchive failed. Bad Download. File too short at '.strlen($data).' bytes.");
            }

            //$status = file_put_contents($this->config['filesdir'] . DIRECTORY_SEPARATOR . "ultimate.zip", $this->getRawReply());
            //if ($status === false)
            //    return $this->setError(299, "Error : FetchArchive failed. Could not write ". $this->config['filesdir'] . DIRECTORY_SEPARATOR . "ultimate.zip");

            return installArchive(this.config["filesdir"] + "\\" + "ultimate.zip");
        }


        /// <summary>
        /// Community Fetch Archive - Fetch the community archive version
        /// </summary>
        /// <returns>hd_specs data on success, false otherwise</returns>
        public dynamic communityFetchArchive()
        {
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
                return setError(299, "Error : FetchArchive failed. Bad Download. File too short at '.strlen($data).' bytes.");
            }

            //var status = file_put_contents($this->config['filesdir'] . DIRECTORY_SEPARATOR . "ultimate.zip", $this->getRawReply());
            //if ($status === false)
            //    return $this->setError(299, "Error : FetchArchive failed. Could not write ". $this->config['filesdir'] . DIRECTORY_SEPARATOR . "ultimate.zip");

            return installArchive(this.config["filesdir"] + "\\" + "ultimate.zip");
        }

        /// <summary>
        /// Install an ultimate archive file
        /// </summary>
        /// <param name="file">string file Fully qualified path to file</param>
        /// <returns>boolean true on success, false otherwise</returns>
        public bool installArchive(string file)
        {
            //TODO:
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
