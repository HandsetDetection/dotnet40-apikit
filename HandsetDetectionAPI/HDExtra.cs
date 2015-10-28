using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HandsetDetectionAPI
{
    public class HDExtra : HDBase
    {
        private HDStore Store = null;
        Dictionary<string, dynamic> data = null;
        public HDExtra()
            : base()
        {
            this.Store = HDStore.Instance;
        }

        public void set(Dictionary<string, dynamic> data)
        {
            this.data = data;
        }

        /// <summary>
        /// Matches all HTTP header extras - platform, browser and app
        /// </summary>
        /// <param name="className">className Is 'platform','browser' or 'app'</param>
        /// <param name="headers"></param>
        /// <returns>an Extra on success, false otherwise</returns>
        public dynamic matchExtra(string className, Dictionary<string, dynamic> headers)
        {
            headers.Remove("profile");
            List<string> orders = detectionConfig[string.Format("{0}-ua-order", className)];
            var keys = detectionConfig.Keys;

            Regex reg = new Regex("^x-", RegexOptions.IgnoreCase);

            foreach (var key in keys)
            {
                if (reg.IsMatch(key))
                {
                    if (!orders.Contains(key))
                    {
                        orders.Add(key);
                    }
                }
            }

            foreach (var item in orders)
            {
                if (headers.ContainsKey(item))
                {
                    dynamic id = getMatch("user-agent", headers[item], className, item, className);
                    if (!(id is Boolean))
                    {
                        var extra = findById(id);
                        if (extra != null)
                        {
                            return extra;
                        }
                    }
                }
            }
            return false;

        }

        /// <summary>
        /// Find a device by its id
        /// </summary>
        /// <param name="id">string id</param>
        /// <returns>array device on success, false otherwise</returns>
        public dynamic findById(string id)
        {
            return Store.read(string.Format("Extra_{0}", id));
        }


        /// <summary>
        /// Can learn language from language header or agent
        /// </summary>
        /// <param name="headers">headers A key => value array of sanitized http headers</param>
        /// <returns>array Extra on success, false otherwise</returns>
        public dynamic matchLanguage(Dictionary<string, dynamic> headers)
        {
            var extra = new Dictionary<string, dynamic>();
            // Mock up a fake Extra for merge into detection reply.
            extra["_id"] = 0;
            extra.Add("Extra", new Dictionary<string, dynamic>());
            extra["Extra"].Add("hd_specs", new Dictionary<string, dynamic>());
            extra["Extra"]["hd_specs"]["general_language"] = "";
            extra["Extra"]["hd_specs"]["general_language_full"] = "";
            // Try directly from http header first
            if (headers.ContainsKey("language"))
            {
                var candidate = headers["language"];
                if (detectionLanguages[candidate])
                {
                    extra["Extra"]["hd_specs"]["general_language"] = candidate;
                    extra["Extra"]["hd_specs"]["general_language_full"] = detectionLanguages[candidate];
                    return extra;
                }
            }

            List<string> checkOrder = detectionConfig["language-ua-order"];
            foreach (var item in headers)
            {
                checkOrder.Add(item.Key);
            }

            var languageList = detectionLanguages;
            foreach (var item in checkOrder)
            {
                if (headers.ContainsKey(item))
                {
                    var agent = headers[item];
                    if (!string.IsNullOrEmpty(agent))
                    {
                        foreach (var languageItem in languageList)
                        {
                            if (Regex.IsMatch(agent, string.Format("[; \\(]{0}[; \\)]", languageItem.Key)))
                            {
                                extra["Extra"]["hd_specs"]["general_language"] = languageItem.Key;
                                extra["Extra"]["hd_specs"]["general_language_full"] = languageItem.Value;
                                return extra;
                            }
                        }
                    }
                }
            }
            return false;
        }


        /// <summary>
        /// Returns false if this device definitively cannot run this platform and platform version.
        /// Returns true if its possible of if there is any doubt.
        /// 
        /// Note : The detected platform must match the device platform. This is the stock OS as shipped
        /// on the device. If someone is running a variant (eg CyanogenMod) then all bets are off.
        /// 
        /// 
        /// </summary>
        /// <param name="specs">string specs The specs we want to check.</param>
        /// <returns>boolean false if these specs can not run the detected OS, true otherwise.</returns>
        public bool verifyPlatform(dynamic specs = null)
        {
            var platform = data;

            var platformName = platform["Extra"]["hd_specs"]["general_platform"].Trim().ToLower();
            var platformVersion = platform["Extra"]["hd_specs"]["general_platform_version"].Trim().ToLower();

            var devicePlatformName = specs["general_platform"].Trim().ToLower();
            var devicePlatformVersionMin = specs["general_platform_version"].Trim().ToLower();
            var devicePlatformVersionMax = specs["general_platform_version_max"].Trim().ToLower();

            // Its possible that we didnt pickup the platform correctly or the device has no platform info
            // Return true in this case because we cant give a concrete false (it might run this version).
            if (platform.Count == 0 || string.IsNullOrEmpty(platformName) || string.IsNullOrEmpty(devicePlatformName))
                return true;

            // Make sure device is running stock OS / Platform
            // Return true in this case because its possible the device can run a different OS (mods / hacks etc..)
            if (platformName != devicePlatformName)
                return true;


            // Detected version is lower than the min version - so definetly false.
            if (!string.IsNullOrEmpty(platformVersion) && !string.IsNullOrEmpty(devicePlatformVersionMin) && comparePlatformVersions(platformVersion, devicePlatformVersionMin) <= -1)
                return false;

            // Detected version is greater than the max version - so definetly false.
            if (!string.IsNullOrEmpty(platformVersion) && !string.IsNullOrEmpty(devicePlatformVersionMax) && comparePlatformVersions(platformVersion, devicePlatformVersionMax) >= 1)
                return false;

            // Maybe Ok ..
            return true;

        }

        /// <summary>
        /// Breaks a version number apart into its Major, minor and point release numbers for comparison.
        /// 
        /// Big Assumption : That version numbers separate their release bits by '.' !!!
        /// might need to do some analysis on the string to rip it up right.
        /// </summary>
        /// <param name="versionNumber">string versionNumber</param>
        /// <returns>array of ('major' => x, 'minor' => y and 'point' => z) on success, null otherwise</returns>
        public Dictionary<string, dynamic> breakVersionApart(string versionNumber)
        {
            var tmp = (versionNumber + ".0.0.0").Split(new string[] { "." }, 4, StringSplitOptions.RemoveEmptyEntries);
            reply = new Dictionary<string, dynamic>();
            reply["major"] = !string.IsNullOrEmpty(tmp[0]) ? tmp[0] : "0";
            reply["minor"] = !string.IsNullOrEmpty(tmp[1]) ? tmp[1] : "0";
            reply["point"] = !string.IsNullOrEmpty(tmp[2]) ? tmp[2] : "0";
            return reply;
        }

        /// <summary>
        ///  Helper for comparing two strings (numerically if possible)
        /// </summary>
        /// <param name="a">string $a Generally a number, but might be a string</param>
        /// <param name="b">string $b Generally a number, but might be a string</param>
        /// <returns>int</returns>
        public int compareSmartly(string a, string b)
        {
            return (IsNumeric(a) && IsNumeric(b)) ? (Convert.ToInt32(a) - Convert.ToInt32(b)) : string.Compare(a, b);
        }

        static readonly Regex _isNumericRegex =
    new Regex("^(" +
            /*Hex*/ @"0x[0-9a-f]+" + "|" +
            /*Bin*/ @"0b[01]+" + "|" +
            /*Oct*/ @"0[0-7]*" + "|" +
            /*Dec*/ @"((?!0)|[-+]|(?=0+\.))(\d*\.)?\d+(e\d+)?" +
                ")$");
        bool IsNumeric(string value)
        {
            return _isNumericRegex.IsMatch(value);
        }


        /// <summary>
        /// Compares two platform version numbers
        /// </summary>
        /// <param name="va">string $va Version A</param>
        /// <param name="vb">string $vb Version B</param>
        /// <returns>< 0 if a < b, 0 if a == b and > 0 if a > b : Also returns 0 if data is absent from either.</returns>
        public int comparePlatformVersions(string va, string vb)
        {
            if (string.IsNullOrEmpty(va) || string.IsNullOrEmpty(vb))
            {
                return 0;
            }
            var versionA = breakVersionApart(va);
            var versionB = breakVersionApart(vb);

            var major = compareSmartly(versionA["major"], versionB["major"]);
            var minor = compareSmartly(versionA["minor"], versionB["minor"]);
            var point = compareSmartly(versionA["point"], versionB["point"]);

            if (major > 0) return major;
            if (minor > 0) return minor;
            if (point > 0) return point;
            return 0;
        }

    }
}
