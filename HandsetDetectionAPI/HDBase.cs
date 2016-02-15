using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace HandsetDetectionAPI
{
    //#define DEBUG = 0
    public class HdBase
    {
        public static int MaxJsonLength = 40000000;

        protected static JavaScriptSerializer Jss = new JavaScriptSerializer { MaxJsonLength = MaxJsonLength };

        protected static Dictionary<string, dynamic> Config = new Dictionary<string, dynamic>() {{"username", ""},
		{"secret", ""},
		{"site_id", ""},
		{"mobile_site", ""},
		{"use_proxy", 0},
		{"proxy_server", ""},
		{"proxy_port", ""},
		{"proxy_user", ""},
		{"proxy_pass", ""},
		{"use_local", true},
		{"api_server", "api.handsetdetection.com"},
		{"timeout", 10},
		{"debug", false},
		{"filesdir", ""},
		{"retries", 3},
		{"cache_requests", true},
		{"geoip", false},
		{"log_unknown", true }};
        protected Dictionary<string, dynamic> DetectedRuleKey = new Dictionary<string, dynamic>();
        string _apiBase = "/apiv4/";
        string _deviceUaFilter = " _\\#-,./:\"'";
        string _extraUaFilter = " ";
        //string loggerHost = "logger.handsetdetection.com";
        //int loggerPort = 80;

        protected Dictionary<string, dynamic> DetectionConfig
        {
            get
            {
                Dictionary<string, dynamic> dicData = new Dictionary<string, dynamic>();

                dicData.Add("device-ua-order", new List<string>() { "x-operamini-phone-ua", "x-mobile-ua", "device-stock-ua", "user-agent", "agent" });
                dicData.Add("platform-ua-order", new List<string>() { "x-operamini-phone-ua", "x-mobile-ua", "device-stock-ua", "user-agent", "agent" });
                dicData.Add("browser-ua-order", new List<string>() { "user-agent", "agent", "device-stock-ua" });
                dicData.Add("app-ua-order", new List<string>() { "user-agent", "agent", "device-stock-ua" });
                dicData.Add("language-ua-order", new List<string>() { "user-agent", "agent", "device-stock-ua" });

                Dictionary<string, dynamic> dicDeviceBiOrder = new Dictionary<string, dynamic>();

                List<List<string>> dicAndroid = new List<List<string>>();
                dicAndroid.Add(new List<string>() { "ro.product.brand", "ro.product.model" });
                dicAndroid.Add(new List<string>() { "ro.product.manufacturer", "ro.product.model" });
                dicAndroid.Add(new List<string>() { "ro-product-brand", "ro-product-model" });
                dicAndroid.Add(new List<string>() { "ro-product-manufacturer", "ro-product-model" });

                dicDeviceBiOrder.Add("android", dicAndroid);

                List<List<string>> dicIos = new List<List<string>>();
                dicIos.Add(new List<string>() { "utsname.brand", "utsname.machine" });

                dicDeviceBiOrder.Add("ios", dicIos);

                List<List<string>> dicWindowPhone = new List<List<string>>();
                dicWindowPhone.Add(new List<string>() { "devicemanufacturer", "devicename" });
                dicDeviceBiOrder.Add("windows phone", dicWindowPhone);

                dicData.Add("device-bi-order", dicDeviceBiOrder);

                Dictionary<string, dynamic> dicPlatformBiOrder = new Dictionary<string, dynamic>();

                List<List<string>> dicPlatformAndroid = new List<List<string>>();
                dicPlatformAndroid.Add(new List<string>() { "ro.build.id", "ro.build.version.release" });
                dicPlatformAndroid.Add(new List<string>() { "ro-build-id", "ro-build-version-release" });

                dicPlatformBiOrder.Add("android", dicPlatformAndroid);

                List<List<string>> dicPlatformIos = new List<List<string>>();
                dicPlatformIos.Add(new List<string>() { "uidevice.systemName", "uidevice.systemversion" });

                dicPlatformBiOrder.Add("ios", dicPlatformIos);


                List<List<string>> dicPlatformWindowPhone = new List<List<string>>();
                dicPlatformWindowPhone.Add(new List<string>() { "osname", "osversion" });

                dicPlatformBiOrder.Add("windows phone", dicPlatformWindowPhone);

                dicData.Add("platform-bi-order", dicPlatformBiOrder);

                Dictionary<string, dynamic> dicBrowserBiOrder = new Dictionary<string, dynamic>();
                dicData.Add("browser-bi-order", dicBrowserBiOrder);
                Dictionary<string, dynamic> dicAppBiOrder = new Dictionary<string, dynamic>();
                dicData.Add("app-bi-order", dicAppBiOrder);

                return dicData;
            }
        }

        /// <summary>
        /// Getting languages list from Languages.json file
        /// </summary>
        public Dictionary<string, string> DetectionLanguages
        {
            get
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                string jsonText = System.IO.File.ReadAllText(ApplicationRootDirectory + "\\Languages.json");
                return serializer.Deserialize<Dictionary<string, string>>(jsonText);
            }
        }
        protected static Dictionary<string, dynamic> Reply = null;

        public HdBase()
        {
            DeviceUaFilterList = _deviceUaFilter.Split(new String[] { "//" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            ExtraUaFilterList = _extraUaFilter.Split(new String[] { "//" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            ReadTimeout = 120;
            _store = HdStore.Instance;
        }

        /// <summary>
        /// Get reply status
        /// </summary>
        /// <returns>int error status, 0 is Ok, anything else is probably not Ok</returns>
        public int GetStatus()
        {
            if (!Reply.ContainsKey("status"))
                return 301;
            return Convert.ToInt32(Reply["status"]);
        }

        /// <summary>
        /// Get reply message
        /// </summary>
        /// <returns>string A message</returns>
        public string GetMessage()
        {
            if (Reply.ContainsKey("status"))
                return Reply["message"];
            else
                return "Not Found";
        }

        /// <summary>
        /// Get reply payload in array assoc format
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, dynamic> GetReply()
        {
            return Reply;
        }

        /// <summary>
        /// Set a reply payload
        /// </summary>
        /// <param name="objReply"></param>
        public void SetReply(Dictionary<string, dynamic> objReply)
        {
            Reply = objReply;
        }

        /// <summary>
        /// Error handling helper. Sets a message and an error code.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="msg"></param>
        /// <returns>true if no error, or false otherwise.</returns>
        protected bool SetError(int status, string msg)
        {
            this.Error = msg;

            if (Reply.ContainsKey("status"))
            {
                Reply["status"] = status;
            }
            else
            {
                Reply.Add("status", status);
            }

            if (Reply.ContainsKey("message"))
            {
                Reply["message"] = msg;
            }
            else
            {
                Reply.Add("message", msg);
            }
            return (status <= 0);
        }

        /// <summary>
        /// String cleanse for extras matching.
        /// </summary>
        /// <param name="str"></param>
        /// <returns>string Cleansed string</returns>
        public string ExtraCleanStr(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;
            StringBuilder b = new StringBuilder(str.ToLower());
            foreach (char c in _extraUaFilter)
            {
                b.Replace(c.ToString(), string.Empty);
            }
            //for (int i = 0; i < ExtraUaFilterList.Count; i++)
            //{
            //    for (int j = 0; j < ExtraUaFilterList[i].Length; j++)
            //    {
            //        str = str.Replace(ExtraUaFilterList[i][j], ' ');
            //    }
            //}

            //str = _reg.Replace(str, "");
            //return Regex.Replace(str, @"\s+", "");
            return b.ToString();
        }
        static Regex _reg = new Regex("[^(\x20-\x7F)]*", RegexOptions.Compiled);

        /// <summary>
        /// Standard string cleanse for device matching
        /// </summary>
        /// <param name="str"></param>
        /// <returns>string cleansed string</returns>
        public string CleanStr(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;
            StringBuilder b = new StringBuilder(str.ToLower());
            foreach (char c in _deviceUaFilter)
            {
                b.Replace(c.ToString(), string.Empty);
            }
            //for (int i = 0; i < DeviceUaFilterList.Count; i++)
            //{
            //    for (int j = 0; j < DeviceUaFilterList[i].Length; j++)
            //    {
            //        str = str.Replace(DeviceUaFilterList[i][j], ' ');
            //    }
            //}

            //str = _reg.Replace(str, "");
            //return Regex.Replace(str, @"\s+", "");
            return b.ToString();
        }

        /// <summary>
        /// Pre processes the request and try different servers on error/timeout
        /// </summary>
        /// <param name="data"></param>
        /// <param name="service">Service strings vary depending on the information needed</param>
        /// <returns>JsonData</returns>
        protected bool Remote(string suburl, Dictionary<string, dynamic> data, string filetype = "json", bool authRequired = true)
        {
            Reply = new Dictionary<string, dynamic>();
            this.RawReply = new Dictionary<string, dynamic>();
            this.SetError(0, "OK");

            string request;
            string requestUrl = _apiBase + suburl;
            int attempts = Convert.ToInt32(Config["retries"]) + 1;
            int trys = 0;
            if (data == null || data.Count == 0)
                request = "";
            else
                request = Jss.Serialize(data);

            bool status = false;
            bool success = false;
            // Uri url = new Uri("http://" + ApiServer + "/apiv4" + service);

            try
            {
                while (trys++ < attempts && success == false)
                {
                    status = Post(Config["api_server"], requestUrl, request, authRequired);
                    if (status)
                    {
                        Reply = Jss.Deserialize<Dictionary<string, dynamic>>(Rawreply);
                        if (filetype.ToLower() == "json")
                        {
                            if (Reply.Count == 0)
                            {
                                SetError(299, "Error: Empty Reply.");
                            }
                            else if (!Reply.ContainsKey("status"))
                            {
                                SetError(299, "Error : No status set in reply");
                            }
                            else if (Convert.ToInt32(Reply["status"]) != 0)
                            {
                                SetError(Reply["status"], Reply["message"]);
                                trys = attempts + 1;
                            }
                            else
                            {
                                success = true;
                            }
                        }
                        else
                        {
                            success = true;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                this.SetError(299, "Exception : " + ex.Message + " " + ex.StackTrace);
            }
            return success;
        }

        /// <summary>
        /// Post data to remote server
        /// </summary>
        /// <param name="server"> Server name</param>
        /// <param name="service"> URL name</param>
        /// <param name="jsondata">Data in json format</param>
        /// <param name="authRequired">Is authentication reguired </param>
        /// <returns>false on failue (sets error), or string on success.</returns>
        private bool Post(string server, string service, string jsondata, bool authRequired = true)
        {
            try
            {
                service = service.Replace("//", "/");
                Uri uri = new Uri("http://" + server + service);
                IPAddress[] ipv4Addresses = Array.FindAll(Dns.GetHostEntry(server).AddressList, a => a.AddressFamily == AddressFamily.InterNetwork);
                // ToDo : Randomize the order of entries in ipList
                foreach (IPAddress ip in ipv4Addresses)
                {

                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uri);
                    req.ServicePoint.BindIPEndPointDelegate = delegate(ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retryCount)
                    {
                        return new IPEndPoint(IPAddress.Any, 0);
                    };

                    if (UseProxy)
                    {
                        WebProxy proxy = new WebProxy(ProxyServer, ProxyPort);
                        proxy.Credentials = new NetworkCredential(ProxyUser, ProxyPass);
                        req.Proxy = proxy;
                    }
                    req.Timeout = ReadTimeout * 1000;
                    req.AllowWriteStreamBuffering = false;
                    req.PreAuthenticate = true;
                    req.Method = "POST";
                    req.ContentType = "application/json";

                    // AuthDigest Components - 
                    // Precomputing the digest saves on the server having to issue a challenge so its much quicker (network wise)
                    // http://en.wikipedia.org/wiki/Digest_access_authentication
                    string realm = "APIv4";
                    string nc = "00000001";
                    string snonce = "APIv4";
                    string cnonce = _helperMD5Hash(DateTime.Now.ToString() + Secret);
                    string qop = "auth";
                    string ha1 = _helperMD5Hash(Username + ":" + realm + ":" + Secret);
                    string ha2 = _helperMD5Hash("POST:" + uri.PathAndQuery);
                    string response = _helperMD5Hash(ha1 + ":" + snonce + ":" + nc + ":" + cnonce + ":" + qop + ":" + ha2);
                    string digest = "Digest username=\"" + Username + "\", realm=\"" + realm + "\", nonce=\"" + snonce + "\", uri=\"" + uri.PathAndQuery + "\", qop=" + qop + ", nc=" + nc + ", cnonce=\"" + cnonce + "\", response=\"" + response + "\", opaque=\"" + realm + "\"";

                    byte[] payload = System.Text.Encoding.ASCII.GetBytes(jsondata);
                    req.ContentLength = payload.Length;
                    req.Headers.Add("Authorization", digest);

                    Stream dataStream = req.GetRequestStream();
                    dataStream.Write(payload, 0, payload.Length);
                    dataStream.Close();
                    HttpWebResponse httpResponse = (HttpWebResponse)req.GetResponse();
                    _responseStream = new MemoryStream();
                    _responseStream = httpResponse.GetResponseStream();
                    Reader = new BinaryReader(_responseStream);

                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
                        if (this.IsDownloadableFiles)
                        {
                            Rawreply = "{\"status\":0}";
                        }
                        else
                        {
                            Rawreply = new StreamReader(_responseStream).ReadToEnd();
                        }
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                this.SetError(299, "Exception : " + ex.Message + " " + ex.StackTrace);
            }
            return false;
        }

        /// <summary>
        /// Helper for determining if a header has BiKeys
        /// </summary>
        /// <param name="branchName"></param>
        /// <returns>platform name on success, false otherwise</returns>
        public dynamic HasBiKeys(Dictionary<string, dynamic> headers)
        {
            Dictionary<string, dynamic> biKeys = DetectionConfig["device-bi-order"];
            List<string> dataKeys = headers.Select(c => c.Key.ToLower()).ToList();

            // Fast check
            if (headers.ContainsKey("agent"))
            {
                return false;
            }

            if (headers.ContainsKey("user-agent"))
            {
                return false;
            }

            foreach (KeyValuePair<string, dynamic> platform in biKeys)
            {
                List<List<string>> set = platform.Value;
                for (int i = 0; i < set.Count; i++)
                {
                    List<string> tupleSet = set[i];
                    int count = 0;
                    int total = tupleSet.Count();
                    for (int index = 0; index < tupleSet.Count; index++)
                    {
                        string item = tupleSet[index];
                        if (dataKeys.Contains(item))
                        {
                            count++;
                        }

                        if (count == total)
                            return platform;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// The heart of the detection process
        /// </summary>
        /// <param name="header">The type of header we're matching against - user-agent type headers use a sieve matching, all others are hash matching.</param>
        /// <param name="value">The http header's value (could be a user-agent or some other x- header value)</param>
        /// <param name="subtree">The branch name eg : user-agent0, user-agent1, user-agentplatform, user-agentbrowser</param>
        /// <param name="actualHeader"></param>
        /// <param name="className"></param>
        /// <returns>node (which is an id) on success, false otherwise</returns>
        public dynamic GetMatch(string header, string value, string subtree = "0", string actualHeader = "", string className = "device")
        {
            //int f = 0;
            //int r = 0;
            string branchName;
            value = value.ToLower();
            if (string.Compare(className, "device", StringComparison.OrdinalIgnoreCase) == 0)
            {
                value = CleanStr(value);
                branchName = header + subtree;
            }
            else
            {
                value = ExtraCleanStr(value);
                branchName = header + subtree;
            }

            if (string.IsNullOrEmpty(value) || value.Length < 4)
            {
                return false;
            }
           

            if (string.Compare(header, "user-agent", StringComparison.OrdinalIgnoreCase) == 0)
            {
                Dictionary<string, Dictionary<string, Dictionary<string, string>>> branch = GetBranch<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(branchName);

                if (branch == null)
                {
                    return false;
                }

                foreach (KeyValuePair<string, Dictionary<string, Dictionary<string, string>>> orders in branch)
                {
                    foreach (KeyValuePair<string, Dictionary<string, string>> filters in orders.Value)
                    {
                        // f++;

                        if (value.IndexOf(filters.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            foreach (KeyValuePair<string, string> matches in filters.Value)
                            {
                                // r++;

                                if (value.IndexOf(matches.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    DetectedRuleKey[className] = CleanStr(header) + ":" + CleanStr(filters.Key) + ":" + CleanStr(matches.Key);
                                    return matches.Value;
                                }
                            }
                        }
                    }
                }

                //foreach (KeyValuePair<string, dynamic> filter in branch.Select(order => order.Value).OfType<Dictionary<string, dynamic>>().SelectMany(filters => filters))
                //{
                //    // ++f;
                //    if (!(value.IndexOf(filter.Key, StringComparison.OrdinalIgnoreCase) > 0)) continue;

                //   var matches = ((Dictionary<string, dynamic>)filter.Value).Where(match => value.IndexOf(match.Key, StringComparison.OrdinalIgnoreCase) > 0);
                //    foreach (KeyValuePair<string, dynamic> match in matches)
                //    {
                //        if (DetectedRuleKey.ContainsKey(className))
                //        {
                //            DetectedRuleKey.Add(className, CleanStr(header) + ":" + CleanStr(filter.Key) + ":" + CleanStr(match.Key));
                //        }
                //        else
                //        {
                //            DetectedRuleKey[className] = CleanStr(header) + ":" + CleanStr(filter.Key) + ":" + CleanStr(match.Key);
                //        }
                //        return match.Value;
                //    }
                //}
            }
            else
            {
                Dictionary<string, string> branch = GetBranch<Dictionary<string, string>>(branchName);
                if (branch == null)
                {
                    return false;
                }
                if (branch.ContainsKey(value)) return branch[value];
                return false;
            }


            return false;
        }

        /// <summary>
        /// Find a branch for the matching process
        /// </summary>
        /// <param name="branchName">The name of the branch to find</param>
        /// <returns>an assoc array on success, false otherwise.</returns>
        public T GetBranch<T>(string branchName)
        {
            if (Tree != null && (Tree.ContainsKey(branchName) && Tree[branchName] != null))
            {
                return (T)Convert.ChangeType(Tree[branchName], typeof(T));
            }

            T tmp = _store.Read<T>(branchName);
            if (tmp == null) return default(T);

            Tree[branchName] = tmp;
            return tmp;
        }


        /// <summary>
        /// TO get encrypted MD5 string
        /// From : http://blogs.msdn.com/b/csharpfaq/archive/2006/10/09/how-do-i-calculate-a-md5-hash-from-a-string_3f00_.aspx
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string _helperMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString().ToLower();
        }



        private HdStore _store = null;
        public bool UseProxy
        {
            get
            {
                return Convert.ToBoolean(Config["use_proxy"]);
            }
            set
            {
                Config["use_proxy"] = value;
            }
        }


        public bool Cacherequests
        {
            get
            {
                return Convert.ToBoolean(Config["cache_requests"]);
            }
            set
            {
                Config["cache_requests"] = value.ToString();
            }
        }

        public bool Debug
        {
            get
            {
                return Convert.ToBoolean(Config["debug"]);
            }
            set
            {
                Config["debug"] = value.ToString();
            }
        }

        public string Username
        {
            get
            {
                return Config["username"];
            }
            set
            {
                Config["username"] = value;
            }
        }
        public string Secret
        {
            get
            {
                return Config["secret"];
            }
            set
            {
                Config["secret"] = value;
            }
        }

        public string ProxyServer
        {
            get
            {
                return Config["proxy_server"];
            }
            set
            {
                Config["proxy_server"] = value;
            }
        }

        public int ProxyPort
        {
            get
            {
                return Config["proxy_port"];
            }
            set
            {
                Config["proxy_port"] = value;
            }
        }
        public string ProxyUser
        {
            get
            {
                return Config["proxy_user"];
            }
            set
            {
                Config["proxy_user"] = value;
            }
        }

        public string ProxyPass
        {
            get
            {
                return Config["proxy_pass"];
            }
            set
            {
                Config["proxy_pass"] = value;
            }
        }

        public Dictionary<string, dynamic> DetectRequest = new Dictionary<string, dynamic>();

        public void SetDetectVar(string key, string value) { DetectRequest[key.ToLower()] = value; }






        public int ReadTimeout
        {
            get
            {
                return Config["timeout"];
            }
            set
            {
                Config["timeout"] = value;
            }
        }
        private Stream _responseStream = null;
        protected static BinaryReader Reader { get; set; }
        public bool IsDownloadableFiles = false;

        public List<string> DeviceUaFilterList;
        public List<string> ExtraUaFilterList;
        protected static string Rawreply;
        protected string Log;
        protected Dictionary<string, dynamic> RawReply = null;
        protected Dictionary<string, dynamic> Tree = new Dictionary<string, dynamic>();


        protected string Error = "";



        public string ApplicationRootDirectory
        {
            get
            {
                string applicationPath = AppDomain.CurrentDomain.BaseDirectory;
                if (applicationPath.IndexOf("\\bin") >= 0)
                {
                    applicationPath = applicationPath.Substring(0, applicationPath.IndexOf("\\bin"));
                }
                return applicationPath;
            }
        }






        /// <summary>
        /// Return replay
        /// </summary>
        protected void SetRawReply()
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            jss.MaxJsonLength = MaxJsonLength;
            Rawreply = jss.Serialize(Reply);
        }

        public string GetRawReply() { return Rawreply; }
        public string ApiServer { get; set; }







    }
}
