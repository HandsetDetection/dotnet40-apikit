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
        string DETECTIONV4_GENERIC = "1";

        private HDStore Store;
        private HDExtra Extra;

        public HDDevice()
        {
            this.Store = HDStore.Instance;
            this.Extra = new HDExtra();
        }

        /// <summary>
        /// Find all device vendors
        /// </summary>
        /// <returns>bool true on success, false otherwise. Use getReply to inspect results on success.</returns>
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

            reply["vendor"] = temp.OrderBy(c => c).ToList();
            reply["message"] = "OK";
            reply["status"] = 0;
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
            reply = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> data = fetchDevices();
            if (data == null)
                return false;

            HashSet<string> temp = new HashSet<string>();
            foreach (Dictionary<string, dynamic> item in data["devices"])
            {
                string vendorNAme = item["Device"]["hd_specs"]["general_vendor"].ToString();
                string modelName = item["Device"]["hd_specs"]["general_model"].ToString();
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
            reply["model"] = temp.OrderBy(c => c).ToList();
            reply["status"] = 0;
            reply["message"] = "OK";
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
                    reply = new Dictionary<string, dynamic>();
                    reply["device"] = item["Device"]["hd_specs"];
                    reply["status"] = 0;
                    reply["message"] = "OK";
                    this.setRawReply();
                    return this.setError(0, "OK");
                }
            }
            reply = new Dictionary<string, dynamic>();
            reply["status"] = 301;
            reply["message"] = "Nothing found";
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
            reply = new Dictionary<string, dynamic>();
            reply["devices"] = temp;
            reply["status"] = 0;
            reply["message"] = "OK";
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
            string hardwareInfo = string.Empty;

            if (headers.ContainsKey("x-local-hardwareinfo"))
            {
                hardwareInfo = headers["x-local-hardwareinfo"];
                headers.Remove("x-local-hardwareinfo");
            }

            if (!(this.hasBiKeys(headers) is Boolean))
            {
                return v4MatchBuildInfo(headers);
            }

            return v4MatchHttpHeaders(headers, hardwareInfo);
        }

        /// <summary>
        ///  Returns the rating score for a device based on the passed values
        /// </summary>
        /// <param name="deviceId">deviceId : The ID of the device to check.</param>
        /// <param name="props">Properties extracted from the device (display_x, display_y etc .. )</param>
        /// <returns></returns>
        public Dictionary<string, dynamic> findRating(string deviceId, Dictionary<string, dynamic> props)
        {
            var device = findById(deviceId);
            if (device["Device"]["hd_specs"] is string)
                return null;

            var specs = device["Device"]["hd_specs"];

            Double total = 0;
            var result = new Dictionary<string, dynamic>();
            var adjX = 0;
            var adjY = 0;
            // Display Resolution - Worth 40 points if correct
            if (props["display_x"].ToString() != "" && props["display_y"].ToString() != "")
            {
                total += 40;
                if (Convert.ToInt32(specs["display_x"]) == props["display_x"] && Convert.ToInt32(specs["display_y"]) == props["display_y"])
                {
                    result["resolution"] = 40;
                }
                else if (Convert.ToInt32(specs["display_x"]) == props["display_y"] && Convert.ToInt32(specs["display_y"]) == props["display_x"])
                {
                    result["resolution"] = 40;
                }
                else if (Convert.ToDouble(specs["display_pixel_ratio"]) > 1.0)
                {
                    // The resolution is often scaled by the pixel ratio for apple devices.
                    adjX = Convert.ToInt32(props["display_x"] * Convert.ToDouble(specs["display_pixel_ratio"]));
                    adjY = Convert.ToInt32(props["display_y"] * Convert.ToDouble(specs["display_pixel_ratio"]));
                    if (Convert.ToInt32(specs["display_x"]) == adjX && Convert.ToInt32(specs["display_y"]) == adjY)
                    {
                        result["resolution"] = 40;
                    }
                    else if (Convert.ToInt32(specs["display_x"]) == adjY && Convert.ToInt32(specs["display_y"]) == adjX)
                    {
                        result["resolution"] = 40;
                    }
                }
            }

            // Display pixel ratio - also worth 40 points
            if (props["display_pixel_ratio"].ToString() != "")
            {
                total += 40;
                // Note : display_pixel_ratio will be a string stored as 1.33 or 1.5 or 2, perhaps 2.0 ..
                if (Convert.ToDouble(specs["display_pixel_ratio"]) == Math.Round(Convert.ToDouble(props["display_pixel_ratio"] / 100.0), 2))
                {
                    result["display_pixel_ratio"] = 40;
                }
            }

            var steps = 0;
            var tmp = 0;
            // Benchmark - 10 points - Enough to tie break but not enough to overrule display or pixel ratio.
            if (!string.IsNullOrEmpty(props["benchmark"].ToString()))
            {
                total += 10;
                if (specs["benchmark_min"].ToString() != "" && specs["benchmark_max"].ToString() != "")
                {
                    if ((int)props["benchmark"] >= Convert.ToInt32(specs["benchmark_min"]) && (int)props["benchmark"] <= Convert.ToInt32(specs["benchmark_max"]))
                    {
                        // Inside range
                        result["benchmark"] = 10;
                        result["benchmark_span"] = (int)10;
                    }
                    else
                    {
                        // Calculate benchmark chunk spans .. as a tie breaker for close calls.
                        result["benchmark"] = 0;
                        //steps = (int)Math.Round(Convert.ToDouble((Convert.ToDouble(specs["benchmark_max"]) - Convert.ToDouble(specs["benchmark_min"])) / 10.0));
                        //// Outside range
                        //if (steps > 0)
                        //{
                        //    if ((int)props["benchmark"] >= Convert.ToInt32(specs["benchmark_max"]))
                        //    {
                        //        // Above range : Calculate how many steps above range
                        //        int objbenchmacrk = props["benchmark"];
                        //        int sobjbenchmacrk = Convert.ToInt32(specs["benchmark_max"]);
                        //        Double objResult = Convert.ToDouble(Convert.ToDouble(objbenchmacrk - sobjbenchmacrk) / steps);

                        //        tmp = (int)Math.Round(objResult, MidpointRounding.AwayFromZero);
                        //        result["benchmark_span"] = (int)10 - (Math.Min(10, Math.Max(0, tmp)));
                        //    }
                        //    else if ((int)props["benchmark"] <= Convert.ToInt32(specs["benchmark_min"]))
                        //    {
                        //        // Below range : Calculate how many steps above range
                        //        int objbenchmacrk = props["benchmark"];
                        //        int sobjbenchmacrk = Convert.ToInt32(specs["benchmark_min"]);


                        //        Double objResult = Convert.ToDouble(Convert.ToDouble(sobjbenchmacrk - objbenchmacrk) / steps);

                        //        tmp = (int)Math.Round(objResult, MidpointRounding.AwayFromZero);
                        //        result["benchmark_span"] = (int)10 - (Math.Min(10, Math.Max(0, tmp)));
                        //    }
                        //}
                    }
                }
            }
            var valuesSum = result.Values.Sum(c => Convert.ToInt32(c));
            result["score"] = (total == 0) ? (int)0 : (int)Math.Round(Convert.ToDouble(valuesSum / total) * 100, 2);
            result["possible"] = total;

            // Distance from mean used in tie breaking situations if two devices have the same score.
            result["distance"] = 100000;
            if (specs["benchmark_min"].ToString() != "" && specs["benchmark_max"].ToString() != "" && props["benchmark"].ToString() != "")
            {
                result["distance"] = (int)Math.Abs(((Convert.ToInt32(specs["benchmark_min"]) + Convert.ToInt32(specs["benchmark_max"])) / 2) - props["benchmark"]);
            }
            return result;
        }

        /// <summary>
        /// Overlays specs onto a device
        /// </summary>
        /// <param name="specsField">string specsField : Either "platform', 'browser', 'language'</param>
        /// <param name="device"></param>
        /// <param name="specs"></param>
        public void specsOverlay(string specsField, ref  dynamic device, Dictionary<string, dynamic> specs)
        {
            switch (specsField)
            {
                case "platform":
                    {
                        if (!string.IsNullOrEmpty(specs["hd_specs"]["general_platform"]))
                        {
                            device["Device"]["hd_specs"]["general_platform"] = specs["hd_specs"]["general_platform"];
                            device["Device"]["hd_specs"]["general_platform_version"] = specs["hd_specs"]["general_platform_version"];
                        }
                    } break;

                case "browser":
                    {
                        if (!string.IsNullOrEmpty(specs["hd_specs"]["general_browser"]))
                        {
                            device["Device"]["hd_specs"]["general_browser"] = specs["hd_specs"]["general_browser"];
                            device["Device"]["hd_specs"]["general_browser_version"] = specs["hd_specs"]["general_browser_version"];
                        }

                    } break;

                case "app":
                    {
                        if (!string.IsNullOrEmpty(specs["hd_specs"]["general_app"]))
                        {
                            device["Device"]["hd_specs"]["general_app"] = specs["hd_specs"]["general_app"];
                            device["Device"]["hd_specs"]["general_app_version"] = specs["hd_specs"]["general_app_version"];
                            device["Device"]["hd_specs"]["general_app_category"] = specs["hd_specs"]["general_app_category"];
                        }

                    } break;

                case "language":
                    {
                        if (!string.IsNullOrEmpty(specs["hd_specs"]["general_language"]))
                        {
                            device["Device"]["hd_specs"]["general_language"] = specs["hd_specs"]["general_language"];
                            device["Device"]["hd_specs"]["general_language_full"] = specs["hd_specs"]["general_language_full"];
                        }
                    } break;
            }
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

            device["Device"]["hd_ops"]["display_x"] = infoArray["display_x"];
            device["Device"]["hd_ops"]["display_y"] = infoArray["display_y"];
            device["Device"]["hd_ops"]["display_pixel_ratio"] = infoArray["display_pixel_ratio"];


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
                if (!(id is Boolean))
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
                if (!(id is Boolean))
                {
                    return this.findById(id);
                }
                headers.Remove("profile");
            }

            // Profile header matching
            if (headers.ContainsKey("x-wap-profile"))
            {
                var id = this.getMatch("x-wap-profile", headers["x-wap-profile"], DETECTIONV4_STANDARD, "x-wap-profile", "device");
                if (!(id is Boolean))
                {
                    return this.findById(id);
                }
                headers.Remove("x-wap-profile");
            }

            List<string> order = this.detectionConfig["device-ua-order"];
            foreach (var item in headers)
            {
                if (!order.Contains(item.Key) && Regex.IsMatch(item.Key, "^x-"))
                {
                    order.Add(item.Key);
                }
            }

            foreach (var item in order)
            {
                if (headers.ContainsKey(item))
                {
                    var id = this.getMatch("user-agent", headers[item], DETECTIONV4_STANDARD, item, "device");
                    if (!(id is Boolean))
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
                itemid = this.getMatch("x-operamini-phone-ua", headers["x-operamini-phone-ua"], DETECTIONV4_GENERIC, "x-operamini-phone-ua", "device");
            }
            if (!HasGetData && headers.ContainsKey("agent"))
            {
                itemid = this.getMatch("agent", headers["agent"], DETECTIONV4_GENERIC, "agent", "device");
            }
            if (!HasGetData && headers.ContainsKey("user-agent"))
            {
                itemid = this.getMatch("user-agent", headers["user-agent"], DETECTIONV4_GENERIC, "user-agent", "device");
                if (itemid is string)
                    HasGetData = true;
            }

            if (HasGetData)
            {
                return this.findById(itemid);
            }

            return false;
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
        /// BuildInfo Matching
        /// Takes a set of buildInfo key/value pairs & works out what the device is
        /// </summary>
        /// <param name="buildInfo">Buildinfo key/value array</param>
        /// <returns>mixed device array on success, false otherwise</returns>
        public dynamic v4MatchBuildInfo(Dictionary<string, dynamic> buildInfo)
        {
            dynamic device = null;
            dynamic platform = null;
            this.detectedRuleKey = null;
            reply = new Dictionary<string, dynamic>();

            // Nothing to check		
            if (buildInfo.Count == 0)
                return false;

            //this.buildInfo = buildInfo;

            // Device Detection
            device = this.v4MatchBIHelper(buildInfo, "device");

            if (device == null || device.Count == 0)
                return false;

            // Platform Detection
            platform = v4MatchBIHelper(buildInfo, "platform");
            if (platform != null && !(platform.Count == 0))
                this.specsOverlay("platform", ref device, platform["Extra"]);

            reply["hd_specs"] = device["Device"]["hd_specs"];
            return this.setError(0, "OK");
        }

        /// <summary>
        /// buildInfo Match helper - Does the build info match heavy lifting
        /// </summary>
        /// <param name="buildInfo">A buildInfo key/value array</param>
        /// <param name="category"></param>
        /// <returns></returns>
        private Dictionary<string, dynamic> v4MatchBIHelper(Dictionary<string, dynamic> buildInfo, string category = "device")
        {
            // ***** Device Detection *****
            var confBIKeys = new Dictionary<string, dynamic>();
            if (detectionConfig.ContainsKey(string.Format("{0}-bi-order", category)))
            {
                confBIKeys = detectionConfig[string.Format("{0}-bi-order", category)];
            }

            if (confBIKeys.Count == 0 || buildInfo.Count == 0)
                return null;

            var hints = new Dictionary<string, dynamic>();
            foreach (KeyValuePair<string, dynamic> platform in confBIKeys)
            {
                var value = "";
                List<List<string>> platformValue = platform.Value;
                foreach (List<string> tuple in platformValue)
                {
                    bool checking = true;
                    foreach (var item in tuple)
                    {
                        if (!buildInfo.ContainsKey(item))
                        {
                            checking = false;
                            break;
                        }
                        else
                        {
                            value += "|" + buildInfo[item];
                        }
                    }

                    if (checking)
                    {
                        value = value.Trim(("| \t\n\r\0\x0B").ToArray());
                        hints[value] = value;
                        var subtree = (category == "device") ? DETECTIONV4_STANDARD : category;
                        var _id = this.getMatch("buildinfo", value, subtree, "buildinfo", category);
                        if (!(_id is Boolean))
                        {
                            return (category == "device") ? this.findById(_id) : this.Extra.findById(_id);
                        }
                    }
                }
            }

            // If we get this far then not found, so try generic.
            var objplatform = this.hasBiKeys(buildInfo);
            if (!(objplatform is Boolean))
            {
                var objTry = new string[2] { string.Format("generic|{0}", objplatform.Key.ToLower()), string.Format("{0}|generic", objplatform.Key.ToLower()) };

                foreach (var objvalue in objTry)
                {
                    var subtree = (category == "device") ? DETECTIONV4_GENERIC : category;
                    var _id = this.getMatch("buildinfo", objvalue, subtree, "buildinfo", category);
                    if (!(_id is Boolean))
                    {
                        return (category == "device") ? this.findById(_id) : this.Extra.findById(_id);
                    }
                }
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
            reply = new Dictionary<string, dynamic>();
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
                    if (tmp.Length == 0)
                    {
                        continue;
                    }
                    else
                    {
                        value = cleanStr(tmp[0]);
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
                    extraHeaders[key] = Extra.extraCleanStr(value);
                }
                else
                {
                    extraHeaders.Add(key, Extra.extraCleanStr(value));

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
            if (!(device["Device"]["hd_ops"]["stop_on_detect"].ToString() == "") && device["Device"]["hd_ops"]["stop_on_detect"].ToString() == "1")
            {
                // Check for hardwareInfo overlay
                if (!string.IsNullOrEmpty(device["Device"]["hd_ops"]["overlay_result_specs"]))
                {
                    if (hwProps is IDictionary)
                    {
                        hardwareInfoOverlay(ref device, (Dictionary<string, dynamic>)hwProps);
                    }

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
                var pass1List = new List<string>();
                // Resolve contention with OS check
                if (!(platform is Boolean))
                {
                    Extra.set(platform);


                    foreach (var item in deviceList)
                    {
                        var tryDevice = this.findById(item);
                        var modelno = tryDevice["Device"]["hd_specs"]["general_model"];
                        if (Extra.verifyPlatform(tryDevice["Device"]["hd_specs"]))
                        {
                            pass1List.Add(item);
                        }
                    }
                }

                // Contention still not resolved .. check hardware
                if (pass1List.Count >= 2 && (hwProps is IDictionary))
                {
                    // Score the list based on hardware
                    List<dynamic> result = new List<dynamic>();
                    foreach (var id in pass1List)
                    {
                        var tmp = findRating(id, hwProps);
                        if (tmp.Count > 0)
                        {
                            tmp["_id"] = id;
                            result.Add(tmp);
                        }
                    }

                    // Sort the results
                    //usort($result, array($this, 'hd_sortByScore'));
                    ratingResult = result;
                    var bestRatedDevice = GetDeviceFromRatingResult(result);
                    var objDevice = this.findById(bestRatedDevice["_id"]);
                    if (objDevice.Count > 0)
                    {
                        var modelno1 = objDevice["Device"]["hd_specs"]["general_model"];

                        device = objDevice;
                    }

                }

            }

            // Overlay specs
            if (!(platform is Boolean))
            {
                specsOverlay("platform", ref device, platform["Extra"]);
            }
            if (!(browser is Boolean))
            {
                specsOverlay("browser", ref device, browser["Extra"]);
            }
            if (!(app is Boolean))
            {
                specsOverlay("app", ref device, app["Extra"]);
            }
            if (!(language is Boolean))
            {
                specsOverlay("language", ref device, language["Extra"]);
            }
            // Overlay hardware info result if required
            if (device["Device"]["hd_ops"]["overlay_result_specs"].ToString() == "1" && !string.IsNullOrEmpty(hardwareInfo))
                hardwareInfoOverlay(ref device, hwProps);

            reply["hd_specs"] = device["Device"]["hd_specs"];
            return setError(0, "OK");//for meantime
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
            if (branch.ContainsKey(ruleKey))
            {
                return branch[ruleKey];
            }
            return false;
        }

        /// <summary>
        /// Determines if hd4Helper would provide mor accurate results.
        /// </summary>
        /// <param name="headers">$headers HTTP Headers</param>
        /// <returns>true if required, false otherwise</returns>
        public bool isHelperUseful(Dictionary<string, dynamic> headers)
        {
            if (headers.Count == 0)
                return false;

            headers.Remove("ip");
            headers.Remove("host");

            if (!localDetect(headers))
                return false;

            if (getHighAccuracyCandidates() is Boolean)
                return false;

            return true;
        }

        /// <summary>
        ///  Custom sort function for sorting results.
        ///  
        /// Includes a tie-breaker for results which score out the same
        /// </summary>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <returns>-1 ($result1 < $result2), 0 ($result1 === $result2) , 1 ($result1 > $result2)</returns>
        public int hd_sortByScore(Dictionary<string, dynamic> d1, Dictionary<string, dynamic> d2)
        {
            if ((Convert.ToInt32(d2["score"]) - Convert.ToInt32(d1["score"])) != 0)
                return (Convert.ToInt32(d2["score"]) - Convert.ToInt32(d1["score"]));
            return Convert.ToInt32(d1["distance"]) - Convert.ToInt32(d2["distance"]);
        }

        /// <summary>
        /// Get a device whose score is maximum
        /// if two devices have same score then device with less distance is returned
        /// </summary>
        /// <param name="deviceResult"></param>
        /// <returns></returns>
        public Dictionary<string, dynamic> GetDeviceFromRatingResult(List<dynamic> deviceResult)
        {
            Dictionary<string, dynamic> bestDevice = null;

            foreach (Dictionary<string, dynamic> item in deviceResult)
            {
                if (bestDevice == null)
                {
                    bestDevice = item;
                    continue;
                }
                else
                {
                    if (item["score"] > bestDevice["score"])
                    {
                        bestDevice = item;
                    }
                    else if (item["score"] == bestDevice["score"])
                    {
                        if (item["distance"] < bestDevice["distance"])
                        {
                            bestDevice = item;
                        }

                    }

                }
            }

            return bestDevice;
        }

    }
}
