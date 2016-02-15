using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HandsetDetectionAPI
{
    public class HdExtra : HdBase
    {
        private HdStore _store = null;
        Dictionary<string, dynamic> _data = null;
        public HdExtra()
            : base()
        {
            this._store = HdStore.Instance;
        }

        public void Set(Dictionary<string, dynamic> data)
        {
            this._data = data;
        }

        /// <summary>
        /// Matches all HTTP header extras - platform, browser and app
        /// </summary>
        /// <param name="className">className Is 'platform','browser' or 'app'</param>
        /// <param name="headers"></param>
        /// <returns>an Extra on success, false otherwise</returns>
        public dynamic MatchExtra(string className, Dictionary<string, dynamic> headers)
        {
            headers.Remove("profile");
            List<string> orders = DetectionConfig[string.Format("{0}-ua-order", className)];
            Dictionary<string, dynamic>.KeyCollection keys = headers.Keys;

            Regex reg = new Regex("^x-", RegexOptions.IgnoreCase);

            foreach (string key in keys.Where(key => reg.IsMatch(key)).Where(key => !orders.Contains(key)))
            {
                orders.Add(key);
            }

            for (int index = 0; index < orders.Count; index++)
            {
                string item = orders[index];
                if (!headers.ContainsKey(item)) continue;
                dynamic id = GetMatch("user-agent", headers[item], className, item, className);
                if (id is bool) continue;
                dynamic extra = FindById(id);
                if (extra != null)
                {
                    return extra;
                }
            }
            return null;

        }

        /// <summary>
        /// Find a device by its id
        /// </summary>
        /// <param name="id">string id</param>
        /// <returns>array device on success, false otherwise</returns>
        public dynamic FindById(string id)
        {
            return _store.Read<Dictionary<string, dynamic>>(string.Format("Extra_{0}", id));
        }


        /// <summary>
        /// Can learn language from language header or agent
        /// </summary>
        /// <param name="headers">headers A key => value array of sanitized http headers</param>
        /// <returns>array Extra on success, false otherwise</returns>
        public Dictionary<string, dynamic> MatchLanguage(Dictionary<string, dynamic> headers)
        {
            Dictionary<string, dynamic> extra = new Dictionary<string, dynamic>();
            // Mock up a fake Extra for merge into detection reply.
            extra["_id"] = 0;
            extra.Add("Extra", new Dictionary<string, dynamic>());
            extra["Extra"].Add("hd_specs", new Dictionary<string, dynamic>());
            extra["Extra"]["hd_specs"]["general_language"] = "";
            extra["Extra"]["hd_specs"]["general_language_full"] = "";
            // Try directly from http header first
            if (headers.ContainsKey("language"))
            {
                dynamic candidate = headers["language"];
                if (DetectionLanguages.ContainsKey(candidate))
                {
                    extra["Extra"]["hd_specs"]["general_language"] = candidate;
                    extra["Extra"]["hd_specs"]["general_language_full"] = DetectionLanguages[candidate];
                    return extra;
                }
            }

            List<string> checkOrder = DetectionConfig["language-ua-order"];
            checkOrder.AddRange(headers.Select(item => item.Key));

            Dictionary<string, string> languageList = DetectionLanguages;
            foreach (KeyValuePair<string, string> languageItem in from item in checkOrder where headers.ContainsKey(item) select headers[item] into agent where !string.IsNullOrEmpty(agent) from languageItem in languageList where Regex.IsMatch(agent, string.Format("[; \\(]{0}[; \\)]", languageItem.Key)) select languageItem)
            {
                extra["Extra"]["hd_specs"]["general_language"] = languageItem.Key;
                extra["Extra"]["hd_specs"]["general_language_full"] = languageItem.Value;
                return extra;
            }
            return extra;
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
        public bool VerifyPlatform(dynamic specs = null)
        {
            Dictionary<string, dynamic> platform = _data;

            dynamic platformName = platform["Extra"]["hd_specs"]["general_platform"].Trim().ToLower();
            dynamic platformVersion = platform["Extra"]["hd_specs"]["general_platform_version"].Trim().ToLower();

            dynamic devicePlatformName = specs["general_platform"].Trim().ToLower();
            dynamic devicePlatformVersionMin = specs["general_platform_version"].Trim().ToLower();
            dynamic devicePlatformVersionMax = specs["general_platform_version_max"].Trim().ToLower();

            // Its possible that we didnt pickup the platform correctly or the device has no platform info
            // Return true in this case because we cant give a concrete false (it might run this version).
            if (platform.Count == 0 || string.IsNullOrEmpty(platformName) || string.IsNullOrEmpty(devicePlatformName))
                return true;

            // Make sure device is running stock OS / Platform
            // Return true in this case because its possible the device can run a different OS (mods / hacks etc..)
            if (platformName != devicePlatformName)
                return true;


            // Detected version is lower than the min version - so definetly false.
            if (!string.IsNullOrEmpty(platformVersion) && !string.IsNullOrEmpty(devicePlatformVersionMin) && ComparePlatformVersions(platformVersion, devicePlatformVersionMin) <= -1)
                return false;

            // Detected version is greater than the max version - so definetly false.
            if (!string.IsNullOrEmpty(platformVersion) && !string.IsNullOrEmpty(devicePlatformVersionMax) && ComparePlatformVersions(platformVersion, devicePlatformVersionMax) >= 1)
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
        public Dictionary<string, dynamic> BreakVersionApart(string versionNumber)
        {
            string[] tmp = (versionNumber + ".0.0.0").Split(new string[] { "." }, 4, StringSplitOptions.RemoveEmptyEntries);
            Reply = new Dictionary<string, dynamic>();
            Reply["major"] = !string.IsNullOrEmpty(tmp[0]) ? tmp[0] : "0";
            Reply["minor"] = !string.IsNullOrEmpty(tmp[1]) ? tmp[1] : "0";
            Reply["point"] = !string.IsNullOrEmpty(tmp[2]) ? tmp[2] : "0";
            return Reply;
        }

        /// <summary>
        ///  Helper for comparing two strings (numerically if possible)
        /// </summary>
        /// <param name="a">string $a Generally a number, but might be a string</param>
        /// <param name="b">string $b Generally a number, but might be a string</param>
        /// <returns>int</returns>
        public int CompareSmartly(string a, string b)
        {
            return (IsNumeric(a) && IsNumeric(b)) ? (Convert.ToInt32(a) - Convert.ToInt32(b)) : string.Compare(a, b);
        }

        static readonly Regex IsNumericRegex =
    new Regex("^(" +
            /*Hex*/ @"0x[0-9a-f]+" + "|" +
            /*Bin*/ @"0b[01]+" + "|" +
            /*Oct*/ @"0[0-7]*" + "|" +
            /*Dec*/ @"((?!0)|[-+]|(?=0+\.))(\d*\.)?\d+(e\d+)?" +
                ")$");

        static bool IsNumeric(string value)
        {
            return IsNumericRegex.IsMatch(value);
        }


        /// <summary>
        /// Compares two platform version numbers
        /// </summary>
        /// <param name="va">string $va Version A</param>
        /// <param name="vb">string $vb Version B</param>
        /// <returns>< 0 if a < b, 0 if a == b and > 0 if a > b : Also returns 0 if data is absent from either.</returns>
        public int ComparePlatformVersions(string va, string vb)
        {
            if (string.IsNullOrEmpty(va) || string.IsNullOrEmpty(vb))
            {
                return 0;
            }
            Dictionary<string, dynamic> versionA = BreakVersionApart(va);
            Dictionary<string, dynamic> versionB = BreakVersionApart(vb);

            dynamic major = CompareSmartly(versionA["major"], versionB["major"]);
            dynamic minor = CompareSmartly(versionA["minor"], versionB["minor"]);
            dynamic point = CompareSmartly(versionA["point"], versionB["point"]);

            if (major != 0) return major;
            if (minor != 0) return minor;
            if (point != 0) return point;
            return 0;
        }

    }
}
