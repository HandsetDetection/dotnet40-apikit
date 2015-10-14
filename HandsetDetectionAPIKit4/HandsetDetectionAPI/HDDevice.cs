using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace HandsetDetectionAPI
{
    public class HDDevice : HDBase
    {
        string DETECTIONV4_STANDARD = "0";
        private HDStore Store;
        private HDExtra Extra;

        public HDDevice()
        {
            this.Store = HDStore.Instance;
            this.Extra = new HDExtra();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool localDeviceVendors()
        {
            reply = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> data = fetchDevices();
            if (data == null)
                return false;
            var temp = new HashSet<string>();
            foreach (var item in data["devices"])
            {
                temp.Add(item["Device"]["hd_specs"]["general_vendor"].ToString());
            }

            reply["vendor"] = temp;
            reply["status"] = 0;
            reply["message"] = "OK";
            setRawReply();
            return setError(0, "OK");
        }

        /// <summary>
        /// Find all models for the sepecified vendor
        /// </summary>
        /// <param name="vendor">vendor The device vendor</param>
        /// <returns>true on success, false otherwise. Use getRawReply to inspect results on success</returns>
        public bool localDeviceModels(string vendor)
        {
            this.reply = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> data = fetchDevices();
            if (data == null)
                return false;

            HashSet<string> temp = new HashSet<string>();
            foreach (Dictionary<string, dynamic> item in data["devices"])
            {
                if (vendor.ToLower() == (item["Device"]["hd_specs"]["general_vendor"].ToString()).ToLower())
                {
                    temp.Add(item["Device"]["hd_specs"]["general_model"].ToString());
                }
                string key = vendor + " ";
                if (item["Device"]["hd_specs"]["general_aliases"].ToString() != "")
                {
                    foreach (string alias_item in item["Device"]["hd_specs"]["general_aliases"])
                    {
                        int result = alias_item.IndexOf(key);
                        if (result == 0)
                        {
                            temp.Add(alias_item.Replace(key, ""));
                        }
                    }
                }
            }
            this.reply["model"] = temp;
            this.reply["status"] = 0;
            this.reply["message"] = "OK";
            this.setRawReply();
            return this.setError(0, "OK");
        }

        /// <summary>
        /// Finds all the specs for a specific device
        /// </summary>
        /// <param name="vendor">vendor The device vendor</param>
        /// <param name="model">model The device model</param>
        /// <returns>true on success, false otherwise. Use getRawReply to inspect results on success</returns>
        public bool localDeviceView(string vendor, string model)
        {
            Dictionary<string, dynamic> data = fetchDevices();
            if (data == null)
                return false;
            vendor = vendor.ToLower();
            model = model.ToLower();
            foreach (Dictionary<string, dynamic> item in data["devices"])
            {
                if (vendor == (item["Device"]["hd_specs"]["general_vendor"].ToString().ToLower()) && model == item["Device"]["hd_specs"]["general_model"].ToString().ToLower())
                {
                    this.reply = new Dictionary<string, dynamic>();
                    this.reply["device"] = item["Device"]["hd_specs"];
                    this.reply["status"] = 0;
                    this.reply["message"] = "OK";
                    this.setRawReply();
                    return this.setError(0, "OK");
                }
            }
            this.reply = new Dictionary<string, dynamic>();
            this.reply["status"] = 301;
            this.reply["message"] = "Nothing found";
            this.setRawReply();
            return this.setError(301, "Nothing found");
        }


        /// <summary>
        /// Finds all devices that have a specific property
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="value">value</param>
        /// <returns>true on success, false otherwise. Use getRawReply to inspect results on success</returns>
        public bool localWhatHas(string key, string value)
        {
            Dictionary<string, dynamic> data = this.fetchDevices();
            if (data == null)
                return false;
            value = value.ToLower();
            key = key.ToLower();
            string s = "";
            Type sType = s.GetType();
            var temp = new ArrayList();
            foreach (Dictionary<string, dynamic> item in data["devices"])
            {
                if (item["Device"]["hd_specs"][key].ToString() == "")
                    continue;
                var match = false;
                if (item["Device"]["hd_specs"][key].GetType() == sType)
                {
                    string check = item["Device"]["hd_specs"][key].ToString().ToLower();
                    if (check.IndexOf(value) >= 0)
                        match = true;
                }
                else
                {
                    foreach (string check in item["Device"]["hd_specs"][key])
                    {
                        string tmpcheck = check.ToLower();
                        if (tmpcheck.IndexOf(value) >= 0)
                            match = true;
                    }
                }
                if (match == true)
                {
                    Dictionary<string, string> sublist = new Dictionary<string, string>();
                    sublist.Add("id", item["Device"]["_id"].ToString());
                    sublist.Add("general_vendor", item["Device"]["hd_specs"]["general_vendor"].ToString());
                    sublist.Add("general_model", item["Device"]["hd_specs"]["general_model"].ToString());
                    temp.Add(sublist);
                }
            }
            this.reply = new Dictionary<string, dynamic>();
            this.reply["device"] = temp;
            this.reply["status"] = 0;
            this.reply["message"] = "OK";
            this.setRawReply();
            return this.setError(0, "OK");
        }

        /// <summary>
        /// Perform a local detection
        /// </summary>
        /// <param name="headers">headers HTTP headers as an assoc array. keys are standard http header names eg user-agent, x-wap-profile</param>
        /// <returns>true on success, false otherwise</returns>
        public bool localDetect(Dictionary<string, dynamic> headers)
        {
            Dictionary<string, dynamic> hardwareInfo = headers["x-local-hardwareinfo"];
            if (hardwareInfo.Count > 0)
            {
                headers.Remove("x-local-hardwareinfo");
            }

            if (this.hasBiKeys(headers))
            {

            }

            return false;//for meantime;
        }


        /// <summary>
        /// Find a device by its id
        /// </summary>
        /// <param name="id">id</param>
        /// <returns>list of device on success, false otherwise</returns>
        public Dictionary<string, dynamic> findById(string id)
        {
            return this.Store.read(string.Format("Device_{0}", id));
        }

        /// <summary>
        /// Internal helper for building a list of all devices.
        /// </summary>
        /// <returns>Dictionary List of all devices.</returns>
        private Dictionary<string, dynamic> fetchDevices()
        {
            try
            {
                Dictionary<string, dynamic> data = this.Store.fetchDevices();
                if (!(data.Count() > 0))
                {
                    this.setError(299, "Error : fetchDevices cannot read files from store.");

                }
                return data;
            }
            catch (Exception ex)
            {
                this.setError(299, "Exception : " + ex.Message + " " + ex.StackTrace);
            }
            return null;
        }

        /// <summary>
        /// Find the best device match for a given set of headers and optional device properties.
        /// 
        /// In 'all' mode all conflicted devces will be returned as a list.
        /// In 'default' mode if there is a conflict then the detected device is returned only (backwards compatible with v3).
        /// </summary>
        /// <param name="headers">headers Set of sanitized http headers</param>
        /// <param name="hardwareInfo">hardwareInfo Information about the hardware</param>
        /// <returns></returns>
        private dynamic v4MatchHttpHeaders(Dictionary<string, dynamic> headers, string hardwareInfo)
        {
            dynamic device = new Dictionary<string, dynamic>();
            dynamic platform = new Dictionary<string, dynamic>();
            dynamic browser = new Dictionary<string, dynamic>();
            dynamic app = new Dictionary<string, dynamic>();
            dynamic ratingResult = new Dictionary<string, dynamic>();
            this.detectedRuleKey = new Dictionary<string, dynamic>(); ;
            this.reply = new Dictionary<string, dynamic>();
            dynamic hwProps = "";

            if (headers == null || headers.Count == 0)
            {
                return false;
            }

            if (headers.ContainsKey("ip"))
            {
                headers.Remove("ip");
            }

            if (headers.ContainsKey("host"))
            {
                headers.Remove("host");
            }

            Dictionary<string, dynamic> deviceHeaders = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> extraHeaders = new Dictionary<string, dynamic>();
            // Sanitize headers & cleanup language

            foreach (var item in headers)
            {
                string key = item.Key.ToLower();
                string value = item.Value;

                if (item.Key.ToLower() == "accept-language" || item.Key.ToLower() == "content-language")
                {
                    key = "language";
                    var tmp = Regex.Split(Convert.ToString(item.Value).Replace(" ", ""), "[,;]");
                    if (tmp.Count == 0)
                    {
                        continue;
                    }
                    else
                    {
                        value = cleanStr(item.Value);
                    }


                }

                if (deviceHeaders.ContainsKey(key))
                {
                    deviceHeaders[key] = value;
                }
                else
                {
                    deviceHeaders.Add(key, value);

                }

                if (extraHeaders.ContainsKey(key))
                {
                    extraHeaders[key] = Extra.extraCleanStr(item.Value);
                }
                else
                {
                    extraHeaders.Add(key, Extra.extraCleanStr(item.Value));

                }
            }

            device = matchDevice(deviceHeaders);

            if (device is Boolean)
            {
                return setError(301, "Not Found");
            }

            if (!string.IsNullOrEmpty(hardwareInfo))
            {
                hwProps = infoStringToArray(hardwareInfo);
            }

            // Stop on detect set - Tidy up and return
            if (!string.IsNullOrEmpty(device["Device"]["hd_ops"]["stop_on_detect"]))
            {
                // Check for hardwareInfo overlay
                if (!string.IsNullOrEmpty(device["Device"]["hd_ops"]["overlay_result_specs"]))
                {

                    hardwareInfoOverlay(ref device, (Dictionary<string, dynamic>)hwProps);

                }
                reply["hd_specs"] = device["Device"]["hd_specs"];
                return setError(0, "OK");

            }
            // Get extra info

            platform = Extra.matchExtra("platform", extraHeaders);
            browser = Extra.matchExtra("browser", extraHeaders);
            app = Extra.matchExtra("app", extraHeaders);
            var language = Extra.matchLanguage(extraHeaders);

            // Find out if there is any contention on the detected rule.
            var deviceList = this.getHighAccuracyCandidates();

            if (!(deviceList is Boolean))
            {
                // Resolve contention with OS check
                Extra.set(platform);
                var pass1List = new Dictionary<string, dynamic>();
                foreach (var item in deviceList)
                {
                    var tryDevice = this.findById(item.Key);
                    if (Extra.verifyPlatform(tryDevice["Device"]["hd_specs"]))
                    {
                        pass1List.Add(item.Key, item.Key);
                    }
                }
            }


            return null;//for meantime
        }


        /// <summary>
        /// Device matching
        /// 
        /// Plan of attack :
        /// 1) Look for opera headers first - as they're definitive
        /// 2) Try profile match - only devices which have unique profiles will match.
        /// 3) Try user-agent match
        /// 4) Try other x-headers
        /// 5) Try all remaining headers
        /// </summary>
        /// <param name="headers"></param>
        /// <returns>array The matched device or null if not found</returns>
        private dynamic matchDevice(Dictionary<string, dynamic> headers)
        {
            string agent = "";
            // Opera mini sometimes puts the vendor # model in the header - nice! ... sometimes it puts ? # ? in as well
            if (headers.ContainsKey("x-operamini-phone") && headers["x-operamini-phone"].trim() != "? # ?")
            {
                var id = this.getMatch("x-operamini-phone", headers["x-operamini-phone"], DETECTIONV4_STANDARD, "x-operamini-phone", "device");
                if (id)
                {
                    return this.findById(id);
                }
                agent = headers["x-operamini-phone"];
                headers.Remove("x-operamini-phone");
            }

            // Profile header matching
            if (headers.ContainsKey("profile"))
            {
                var id = this.getMatch("profile", headers["profile"], DETECTIONV4_STANDARD, "profile", "device");
                if (id)
                {
                    return this.findById(id);
                }
                headers.Remove("profile");
            }

            // Profile header matching
            if (headers.ContainsKey("x-wap-profile"))
            {
                var id = this.getMatch("x-wap-profile", headers["x-wap-profile"], DETECTIONV4_STANDARD, "x-wap-profile", "device");
                if (id)
                {
                    return this.findById(id);
                }
                headers.Remove("x-wap-profile");
            }

            Dictionary<string, dynamic> order = this.detectionConfig["device-ua-order"];
            foreach (var item in headers)
            {
                if (!order.ContainsKey(item.Key) && Regex.IsMatch(item.Key, "^x-"))
                {
                    order.Add(item.Key, item.Value);
                }
            }

            foreach (var item in order)
            {
                if (!headers.ContainsKey(item.Key))
                {
                    var id = this.getMatch("user-agent", headers[item.Key], DETECTIONV4_STANDARD, item.Key, "device");
                    if (id)
                    {
                        return this.findById(id);
                    }
                }
            }

            bool HasGetData = false;
            dynamic itemid = "";
            // Generic matching - Match of last resort
            if (headers.ContainsKey("x-operamini-phone-ua"))
            {
                itemid = this.getMatch("x-operamini-phone-ua", headers["x-operamini-phone-ua"], DETECTIONV4_STANDARD, "x-operamini-phone-ua", "device");
            }
            if (!HasGetData && headers.ContainsKey("agent"))
            {
                itemid = this.getMatch("agent", headers["agent"], DETECTIONV4_STANDARD, "agent", "device");
            }
            if (!HasGetData && headers.ContainsKey("user-agent"))
            {
                itemid = this.getMatch("user-agent", headers["user-agent"], DETECTIONV4_STANDARD, "user-agent", "device");
            }

            if (!HasGetData)
            {
                return this.findById(itemid);
            }

            return false;
        }

        /// <summary>
        /// Takes a string of onDeviceInformation and turns it into something that can be used for high accuracy checking.
        /// 
        /// Strings a usually generated from cookies, but may also be supplied in headers.
        /// The format is $w;$h;$r;$b where w is the display width, h is the display height, r is the pixel ratio and b is the benchmark.
        /// display_x, display_y, display_pixel_ratio, general_benchmark
        /// </summary>
        /// <param name="hardwareInfo">hardwareInfo String of light weight device property information, separated by ':'</param>
        /// <returns>partial specs array of information we can use to improve detection accuracy</returns>
        private Dictionary<string, dynamic> infoStringToArray(string hardwareInfo)
        {
            Dictionary<string, dynamic> result = new Dictionary<string, dynamic>();
            // Remove the header or cookie name from the string 'x-specs1a='
            if (hardwareInfo.IndexOf("=") >= 0)
            {
                List<string> lstHardwareInfo = hardwareInfo.Split(new String[] { "=" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (lstHardwareInfo.Count > 1)
                {
                    hardwareInfo = lstHardwareInfo[1];
                }
                else
                {
                    return result;
                }
            }
            reply = new Dictionary<string, dynamic>();
            var info = hardwareInfo.Split(new String[] { ":" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (info.Count != 4)
            {
                return result;
            }
            reply["display_x"] = Convert.ToInt32(info[0].Trim());
            reply["display_y"] = Convert.ToInt32(info[1].Trim());
            reply["display_pixel_ratio"] = Convert.ToInt32(info[2].Trim());
            reply["benchmark"] = Convert.ToInt32(info[3].Trim());
            return reply;
        }

        /// <summary>
        /// Overlays hardware info onto a device - Used in generic replys
        /// </summary>
        /// <param name="device"></param>
        /// <param name="infoArray"></param>
        private void hardwareInfoOverlay(ref dynamic device, Dictionary<string, dynamic> infoArray)
        {
            if (!string.IsNullOrEmpty(infoArray["display_x"]))
            {
                device["Device"]["hd_ops"]["display_x"] = infoArray["display_x"];
            }
            if (!string.IsNullOrEmpty(infoArray["display_y"]))
            {
                device["Device"]["hd_ops"]["display_y"] = infoArray["display_y"];

            }
            if (!string.IsNullOrEmpty(infoArray["display_pixel_ratio"]))
            {
                device["Device"]["hd_ops"]["display_pixel_ratio"] = infoArray["display_pixel_ratio"];

            }
        }

        /// <summary>
        /// Determines if high accuracy checks are available on the device which was just detected
        /// 
        /// </summary>
        /// <returns>a list of candidate devices which have this detection rule or false otherwise.</returns>
        private dynamic getHighAccuracyCandidates()
        {
            var branch = this.getBranch("hachecks");
            var ruleKey = detectedRuleKey["device"];
            if (branch[ruleKey].Count > 0)
            {
                return branch[ruleKey];
            }
            return false;
        }

    }
}
