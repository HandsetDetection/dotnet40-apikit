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
    public class HdDevice : HdBase
    {
        string _detectionv4Standard = "0";
        string _detectionv4Generic = "1";

        private HdStore _store;
        private HdExtra _extra;

        public HdDevice()
        {

            _store = HdStore.Instance;
            _extra = new HdExtra();
        }

        /// <summary>
        /// Find all device vendors
        /// </summary>
        /// <returns>bool true on success, false otherwise. Use getReply to inspect results on success.</returns>
        public bool LocalDeviceVendors()
        {
            Reply = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> data = FetchDevices();
            if (data == null)
                return false;
            HashSet<string> temp = new HashSet<string>();
            foreach (dynamic item in data["devices"])
            {
                temp.Add(item["Device"]["hd_specs"]["general_vendor"].ToString());
            }

            Reply["vendor"] = temp.OrderBy(c => c).ToList();
            Reply["message"] = "OK";
            Reply["status"] = 0;
            SetRawReply();
            return SetError(0, "OK");
        }

        /// <summary>
        /// Find all models for the sepecified vendor
        /// </summary>
        /// <param name="vendor">vendor The device vendor</param>
        /// <returns>true on success, false otherwise. Use getRawReply to inspect results on success</returns>
        public bool LocalDeviceModels(string vendor)
        {
            Reply = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> data = FetchDevices();
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

                if (item["Device"]["hd_specs"]["general_aliases"].ToString() == "") continue;

                foreach (string aliasItem in item["Device"]["hd_specs"]["general_aliases"])
                {
                    int result = aliasItem.IndexOf(key, StringComparison.Ordinal);
                    if (result == 0)
                    {
                        temp.Add(aliasItem.Replace(key, ""));
                    }
                }
            }
            Reply["model"] = temp.OrderBy(c => c).ToList();
            Reply["status"] = 0;
            Reply["message"] = "OK";
            SetRawReply();
            return SetError(0, "OK");
        }

        /// <summary>
        /// Finds all the specs for a specific device
        /// </summary>
        /// <param name="vendor">vendor The device vendor</param>
        /// <param name="model">model The device model</param>
        /// <returns>true on success, false otherwise. Use getRawReply to inspect results on success</returns>
        public bool LocalDeviceView(string vendor, string model)
        {
            Dictionary<string, dynamic> data = FetchDevices();
            if (data == null)
                return false;
            vendor = vendor.ToLower();
            model = model.ToLower();
            foreach (Dictionary<string, dynamic> item in data["devices"])
            {
                if (string.Compare(vendor, (item["Device"]["hd_specs"]["general_vendor"].ToString().ToLower()), StringComparison.OrdinalIgnoreCase) == 0 && string.Compare(model, item["Device"]["hd_specs"]["general_model"].ToString(), StringComparison.OrdinalIgnoreCase) == 0)
                {
                    Reply = new Dictionary<string, dynamic>();
                    Reply["device"] = item["Device"]["hd_specs"];
                    Reply["status"] = 0;
                    Reply["message"] = "OK";
                    SetRawReply();
                    return SetError(0, "OK");
                }
            }
            Reply = new Dictionary<string, dynamic>();
            Reply["status"] = 301;
            Reply["message"] = "Nothing found";
            SetRawReply();
            return SetError(301, "Nothing found");
        }

        /// <summary>
        /// Finds all devices that have a specific property
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="value">value</param>
        /// <returns>true on success, false otherwise. Use getRawReply to inspect results on success</returns>
        public bool LocalWhatHas(string key, string value)
        {
            Dictionary<string, dynamic> data = FetchDevices();
            if (data == null)
                return false;
            value = value.ToLower();
            key = key.ToLower();
            string s = "";
            Type sType = s.GetType();
            ArrayList temp = new ArrayList();
            foreach (Dictionary<string, dynamic> item in data["devices"])
            {
                if (item["Device"]["hd_specs"][key].ToString() == "")
                    continue;
                bool match = false;
                if (item["Device"]["hd_specs"][key].GetType() == sType)
                {
                    string check = item["Device"]["hd_specs"][key].ToString().ToLower();
                    if (check.IndexOf(value, StringComparison.Ordinal) >= 0)
                        match = true;
                }
                else
                {
                    foreach (string check in item["Device"]["hd_specs"][key])
                    {
                        string tmpcheck = check.ToLower();
                        if (tmpcheck.IndexOf(value, StringComparison.Ordinal) >= 0)
                            match = true;
                    }
                }
                if (match != true) continue;
                Dictionary<string, string> sublist = new Dictionary<string, string>
                {
                    {"id", item["Device"]["_id"].ToString()},
                    {"general_vendor", item["Device"]["hd_specs"]["general_vendor"].ToString()},
                    {"general_model", item["Device"]["hd_specs"]["general_model"].ToString()}
                };
                temp.Add(sublist);
            }
            Reply = new Dictionary<string, dynamic>();
            Reply["devices"] = temp;
            Reply["status"] = 0;
            Reply["message"] = "OK";
            SetRawReply();
            return SetError(0, "OK");
        }

        /// <summary>
        /// Perform a local detection
        /// </summary>
        /// <param name="headers">headers HTTP headers as an assoc array. keys are standard http header names eg user-agent, x-wap-profile</param>
        /// <returns>true on success, false otherwise</returns>
        public bool LocalDetect(Dictionary<string, dynamic> headers)
        {
            string hardwareInfo = string.Empty;

            if (headers.ContainsKey("x-local-hardwareinfo"))
            {
                hardwareInfo = headers["x-local-hardwareinfo"];
                headers.Remove("x-local-hardwareinfo");
            }

            if (!(HasBiKeys(headers) is bool))
            {
                return V4MatchBuildInfo(headers);
            }

            return V4MatchHttpHeaders(headers, hardwareInfo);
        }

        /// <summary>
        ///  Returns the rating score for a device based on the passed values
        /// </summary>
        /// <param name="deviceId">deviceId : The ID of the device to check.</param>
        /// <param name="props">Properties extracted from the device (display_x, display_y etc .. )</param>
        /// <returns></returns>
        public Dictionary<string, dynamic> FindRating(string deviceId, Dictionary<string, dynamic> props)
        {
            Dictionary<string, dynamic> device = FindById(deviceId);
            if (device["Device"]["hd_specs"] is string)
                return null;

            dynamic specs = device["Device"]["hd_specs"];

            double total = 0;
            Dictionary<string, dynamic> result = new Dictionary<string, dynamic>();
            int adjX = 0;
            int adjY = 0;
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
            int valuesSum = result.Values.Sum(c => Convert.ToInt32(c));
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
        public void SpecsOverlay(string specsField, ref  dynamic device, Dictionary<string, dynamic> specs)
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
        private Dictionary<string, dynamic> InfoStringToArray(string hardwareInfo)
        {
            Dictionary<string, dynamic> result = new Dictionary<string, dynamic>();
            // Remove the header or cookie name from the string 'x-specs1a='
            if (hardwareInfo.IndexOf("=", StringComparison.Ordinal) >= 0)
            {
                List<string> lstHardwareInfo = hardwareInfo.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (lstHardwareInfo.Count > 1)
                {
                    hardwareInfo = lstHardwareInfo[1];
                }
                else
                {
                    return result;
                }
            }
            Reply = new Dictionary<string, dynamic>();
            List<string> info = hardwareInfo.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (info.Count != 4)
            {
                return result;
            }
            Reply["display_x"] = Convert.ToInt32(info[0].Trim());
            Reply["display_y"] = Convert.ToInt32(info[1].Trim());
            Reply["display_pixel_ratio"] = Convert.ToInt32(info[2].Trim());
            Reply["benchmark"] = Convert.ToInt32(info[3].Trim());
            return Reply;
        }

        /// <summary>
        /// Overlays hardware info onto a device - Used in generic replys
        /// </summary>
        /// <param name="device"></param>
        /// <param name="infoArray"></param>
        private void HardwareInfoOverlay(ref dynamic device, Dictionary<string, dynamic> infoArray)
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
        private dynamic MatchDevice(Dictionary<string, dynamic> headers)
        {
            // Opera mini sometimes puts the vendor # model in the header - nice! ... sometimes it puts ? # ? in as well
            if (headers.ContainsKey("x-operamini-phone") && headers["x-operamini-phone"].ToString() != "? # ?")
            {
                dynamic id = this.GetMatch("x-operamini-phone", headers["x-operamini-phone"], _detectionv4Standard, "x-operamini-phone", "device");
                if (!(id is bool))
                {
                    return this.FindById(id);
                }
                headers.Remove("x-operamini-phone");
            }

            // Profile header matching
            if (headers.ContainsKey("profile"))
            {
                dynamic id = this.GetMatch("profile", headers["profile"], _detectionv4Standard, "profile", "device");
                if (!(id is bool))
                {
                    return this.FindById(id);
                }
                headers.Remove("profile");
            }

            // Profile header matching
            if (headers.ContainsKey("x-wap-profile"))
            {
                dynamic id = this.GetMatch("x-wap-profile", headers["x-wap-profile"], _detectionv4Standard, "x-wap-profile", "device");
                if (!(id is bool))
                {
                    return this.FindById(id);
                }
                headers.Remove("x-wap-profile");
            }

            List<string> order = DetectionConfig["device-ua-order"];
            foreach (KeyValuePair<string, dynamic> item in headers.Where(item => !order.Contains(item.Key) && Regex.IsMatch(item.Key, "^x-")))
            {
                order.Add(item.Key);
            }

            foreach (dynamic id in from item in order where headers.ContainsKey(item) select this.GetMatch("user-agent", headers[item], _detectionv4Standard, item, "device") into id where !(id is bool) select id)
            {
                return this.FindById(id);
            }

            bool hasGetData = false;
            dynamic itemid = "";
            // Generic matching - Match of last resort
            if (headers.ContainsKey("x-operamini-phone-ua"))
            {
                itemid = this.GetMatch("x-operamini-phone-ua", headers["x-operamini-phone-ua"], _detectionv4Generic, "x-operamini-phone-ua", "device");
            }
            if (!hasGetData && headers.ContainsKey("agent"))
            {
                itemid = this.GetMatch("agent", headers["agent"], _detectionv4Generic, "agent", "device");
            }
            if (!hasGetData && headers.ContainsKey("user-agent"))
            {
                itemid = this.GetMatch("user-agent", headers["user-agent"], _detectionv4Generic, "user-agent", "device");
                if (itemid is string)
                    hasGetData = true;
            }

            if (hasGetData)
            {
                return this.FindById(itemid);
            }

            return false;
        }

        /// <summary>
        /// Find a device by its id
        /// </summary>
        /// <param name="id">id</param>
        /// <returns>list of device on success, false otherwise</returns>
        public Dictionary<string, dynamic> FindById(string id)
        {
            return _store.Read<Dictionary<string, dynamic>>(string.Format("Device_{0}", id));
        }

        /// <summary>
        /// Internal helper for building a list of all devices.
        /// </summary>
        /// <returns>Dictionary List of all devices.</returns>
        private Dictionary<string, dynamic> FetchDevices()
        {
            try
            {
                Dictionary<string, dynamic> data = _store.FetchDevices();
                if (!(data.Any()))
                {
                    SetError(299, "Error : fetchDevices cannot read files from store.");

                }
                return data;
            }
            catch (Exception ex)
            {
                SetError(299, "Exception : " + ex.Message + " " + ex.StackTrace);
            }
            return null;
        }

        /// <summary>
        /// BuildInfo Matching
        /// Takes a set of buildInfo key/value pairs & works out what the device is
        /// </summary>
        /// <param name="buildInfo">Buildinfo key/value array</param>
        /// <returns>mixed device array on success, false otherwise</returns>
        public dynamic V4MatchBuildInfo(Dictionary<string, dynamic> buildInfo)
        {


            // Nothing to check		
            if (buildInfo.Count == 0)
                return false;


            DetectedRuleKey = null;
            Reply = new Dictionary<string, dynamic>();
            //this.buildInfo = buildInfo;

            // Device Detection
            dynamic device = V4MatchBiHelper(buildInfo);

            if (device == null || device.Count == 0)
                return false;

            // Platform Detection
            dynamic platform = V4MatchBiHelper(buildInfo, "platform");
            if (platform != null && platform.Count != 0)
                this.SpecsOverlay("platform", ref device, platform["Extra"]);

            Reply["hd_specs"] = device["Device"]["hd_specs"];
            return SetError(0, "OK");
        }

        /// <summary>
        /// buildInfo Match helper - Does the build info match heavy lifting
        /// </summary>
        /// <param name="buildInfo">A buildInfo key/value array</param>
        /// <param name="category"></param>
        /// <returns></returns>
        private Dictionary<string, dynamic> V4MatchBiHelper(Dictionary<string, dynamic> buildInfo, string category = "device")
        {
            // ***** Device Detection *****
            Dictionary<string, dynamic> confBiKeys = new Dictionary<string, dynamic>();
            if (DetectionConfig.ContainsKey(string.Format("{0}-bi-order", category)))
            {
                confBiKeys = DetectionConfig[string.Format("{0}-bi-order", category)];
            }

            if (confBiKeys.Count == 0 || buildInfo.Count == 0)
                return null;

            foreach (KeyValuePair<string, dynamic> platform in confBiKeys)
            {
                string value = "";
                List<List<string>> platformValue = platform.Value;
                for (int index = 0; index < platformValue.Count; index++)
                {
                    List<string> tuple = platformValue[index];
                    bool checking = true;
                    for (int i = 0; i < tuple.Count; i++)
                    {
                        string item = tuple[i];
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

                    if (!checking) continue;

                    value = value.Trim(("| \t\n\r\0\x0B").ToArray());
                    string subtree = string.Compare(category, "device", StringComparison.OrdinalIgnoreCase) == 0 ? _detectionv4Standard : category;
                    dynamic id = GetMatch("buildinfo", value, subtree, "buildinfo", category);
                    if (!(id is bool))
                    {
                        return string.Compare(category, "device", StringComparison.OrdinalIgnoreCase) == 0 ? this.FindById(id) : _extra.FindById(id);
                    }
                }
            }

            // If we get this far then not found, so try generic.
            dynamic objplatform = HasBiKeys(buildInfo);
            if (objplatform is bool) return null;

            string[] objTry = new string[2] { string.Format("generic|{0}", objplatform.Key.ToLower()), string.Format("{0}|generic", objplatform.Key.ToLower()) };

            return (from objvalue in objTry let subtree = string.Compare(category, "device", StringComparison.OrdinalIgnoreCase) == 0 ? _detectionv4Generic : category select GetMatch("buildinfo", objvalue, subtree, "buildinfo", category) into id where !(id is bool) select string.Compare(category, "device", StringComparison.OrdinalIgnoreCase) == 0 ? this.FindById(id) : _extra.FindById(id)).FirstOrDefault();
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
        private dynamic V4MatchHttpHeaders(Dictionary<string, dynamic> headers, string hardwareInfo)
        {
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

            DetectedRuleKey = new Dictionary<string, dynamic>();
            Reply = new Dictionary<string, dynamic>();
            dynamic hwProps = "";
            Dictionary<string, dynamic> deviceHeaders = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> extraHeaders = new Dictionary<string, dynamic>();
            // Sanitize headers & cleanup language

            foreach (KeyValuePair<string, dynamic> item in headers)
            {
                string key = item.Key.ToLower();
                string value = item.Value;

                if (string.Compare(item.Key, "accept-language", StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(item.Key, "content-language", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    key = "language";
                    dynamic tmp = Regex.Split(Convert.ToString(item.Value).Replace(" ", ""), "[,;]");
                    if (tmp.Length == 0)
                    {
                        continue;
                    }
                    value = CleanStr(tmp[0]);
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
                    extraHeaders[key] = _extra.ExtraCleanStr(value);
                }
                else
                {
                    extraHeaders.Add(key, _extra.ExtraCleanStr(value));

                }
            }

            dynamic device = MatchDevice(deviceHeaders);

            if (device is bool)
            {
                return SetError(301, "Not Found");
            }

            if (!string.IsNullOrEmpty(hardwareInfo))
            {
                hwProps = InfoStringToArray(hardwareInfo);
            }

            // Stop on detect set - Tidy up and return
            if (device["Device"]["hd_ops"]["stop_on_detect"].ToString() != "" && device["Device"]["hd_ops"]["stop_on_detect"].ToString() == "1")
            {
                // Check for hardwareInfo overlay
                if (!string.IsNullOrEmpty(device["Device"]["hd_ops"]["overlay_result_specs"]))
                {
                    if (hwProps is IDictionary)
                    {
                        HardwareInfoOverlay(ref device, (Dictionary<string, dynamic>)hwProps);
                    }

                }
                Reply["hd_specs"] = device["Device"]["hd_specs"];
                return SetError(0, "OK");

            }
            // Get extra info

            dynamic platform = _extra.MatchExtra("platform", extraHeaders);
            dynamic browser = _extra.MatchExtra("browser", extraHeaders);
            dynamic app = _extra.MatchExtra("app", extraHeaders);
            dynamic language = _extra.MatchLanguage(extraHeaders);

            // Find out if there is any contention on the detected rule.
            dynamic deviceList = GetHighAccuracyCandidates();

            if (!(deviceList is bool))
            {
                List<string> pass1List = new List<string>();
                // Resolve contention with OS check
                if (!(platform is bool))
                {
                    _extra.Set(platform);


                    foreach (dynamic item in deviceList)
                    {
                        dynamic tryDevice = this.FindById(item);

                        if (_extra.VerifyPlatform(tryDevice["Device"]["hd_specs"]))
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
                    for (int index = 0; index < pass1List.Count; index++)
                    {
                        string id = pass1List[index];
                        dynamic tmp = FindRating(id, hwProps);

                        if (tmp.Count <= 0) continue;

                        tmp["_id"] = id;
                        result.Add(tmp);
                    }

                    // Sort the results
                    //usort($result, array($this, 'hd_sortByScore'));
                    Dictionary<string, dynamic> bestRatedDevice = GetDeviceFromRatingResult(result);
                    dynamic objDevice = this.FindById(bestRatedDevice["_id"]);
                    if (objDevice.Count > 0)
                    {
                        device = objDevice;
                    }
                }

            }

            // Overlay specs
            if (!(platform is bool))
            {
                SpecsOverlay("platform", ref device, platform["Extra"]);
            }
            if (!(browser is bool))
            {
                SpecsOverlay("browser", ref device, browser["Extra"]);
            }
            if (!(app is bool))
            {
                SpecsOverlay("app", ref device, app["Extra"]);
            }
            if (!(language is bool))
            {
                SpecsOverlay("language", ref device, language["Extra"]);
            }
            // Overlay hardware info result if required
            if (device["Device"]["hd_ops"]["overlay_result_specs"].ToString() == "1" && !string.IsNullOrEmpty(hardwareInfo))
                HardwareInfoOverlay(ref device, hwProps);

            Reply["hd_specs"] = device["Device"]["hd_specs"];
            return SetError(0, "OK");//for meantime
        }

        /// <summary>
        /// Determines if high accuracy checks are available on the device which was just detected
        /// 
        /// </summary>
        /// <returns>a list of candidate devices which have this detection rule or false otherwise.</returns>
        private dynamic GetHighAccuracyCandidates()
        {
            var branch = GetBranch<Dictionary<string, List<string>>>("hachecks");
            dynamic ruleKey = DetectedRuleKey["device"];
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
        public bool IsHelperUseful(Dictionary<string, dynamic> headers)
        {
            if (headers.Count == 0)
                return false;

            headers.Remove("ip");
            headers.Remove("host");

            if (!LocalDetect(headers))
                return false;

            return !(GetHighAccuracyCandidates() is bool);
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

            for (int index = 0; index < deviceResult.Count; index++)
            {
                Dictionary<string, dynamic> item = deviceResult[index];
                if (bestDevice == null)
                {
                    bestDevice = item;
                    continue;
                }

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

            return bestDevice;
        }

    }
}
