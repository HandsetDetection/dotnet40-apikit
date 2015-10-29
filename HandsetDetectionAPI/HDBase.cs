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
    public class HDBase
    {
        protected static Dictionary<string, dynamic> config = new Dictionary<string, dynamic>() {{"username", ""},
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
        protected Dictionary<string, dynamic> detectedRuleKey = new Dictionary<string, dynamic>();
        string apiBase = "/apiv4/";
        string deviceUAFilter = " _\\#-,./:\"'";
        string extraUAFilter = " ";
        string loggerHost = "logger.handsetdetection.com";
        int loggerPort = 80;

        protected Dictionary<string, dynamic> detectionConfig
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

                List<List<string>> dicIOS = new List<List<string>>();
                dicIOS.Add(new List<string>() { "utsname.brand", "utsname.machine" });

                dicDeviceBiOrder.Add("ios", dicIOS);

                List<List<string>> dicWindowPhone = new List<List<string>>();
                dicWindowPhone.Add(new List<string>() { "devicemanufacturer", "devicename" });
                dicDeviceBiOrder.Add("windows phone", dicWindowPhone);

                dicData.Add("device-bi-order", dicDeviceBiOrder);

                Dictionary<string, dynamic> dicPlatformBiOrder = new Dictionary<string, dynamic>();

                List<List<string>> dicPlatformAndroid = new List<List<string>>();
                dicPlatformAndroid.Add(new List<string>() { "ro.build.id", "ro.build.version.release" });
                dicPlatformAndroid.Add(new List<string>() { "ro-build-id", "ro-build-version-release" });

                dicPlatformBiOrder.Add("android", dicPlatformAndroid);

                List<List<string>> dicPlatformIOS = new List<List<string>>();
                dicPlatformIOS.Add(new List<string>() { "uidevice.systemName", "uidevice.systemversion" });

                dicPlatformBiOrder.Add("ios", dicPlatformIOS);


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
        public Dictionary<string, string> detectionLanguages
        {
            get
            {
                var serializer = new JavaScriptSerializer();
                string jsonText = System.IO.File.ReadAllText(ApplicationRootDirectory + "\\Languages.json");
                return serializer.Deserialize<Dictionary<string, string>>(jsonText);
            }
        }
        protected static Dictionary<string, dynamic> reply = null;

        public HDBase()
        {
            deviceUAFilterList = deviceUAFilter.Split(new String[] { "//" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            extraUAFilterList = extraUAFilter.Split(new String[] { "//" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            ReadTimeout = 120;
            Store = HDStore.Instance;
        }

        /// <summary>
        /// Get reply status
        /// </summary>
        /// <returns>int error status, 0 is Ok, anything else is probably not Ok</returns>
        public int getStatus()
        {
            if (reply.ContainsKey("status"))
                return Convert.ToInt32(reply["status"]);
            else
                return 301;
        }

        /// <summary>
        /// Get reply message
        /// </summary>
        /// <returns>string A message</returns>
        public string getMessage()
        {
            if (reply.ContainsKey("status"))
                return reply["message"];
            else
                return "Not Found";
        }

        /// <summary>
        /// Get reply payload in array assoc format
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, dynamic> getReply()
        {
            return reply;
        }

        /// <summary>
        /// Set a reply payload
        /// </summary>
        /// <param name="objReply"></param>
        public void setReply(Dictionary<string, dynamic> objReply)
        {
            reply = objReply;
        }

        /// <summary>
        /// Error handling helper. Sets a message and an error code.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="msg"></param>
        /// <returns>true if no error, or false otherwise.</returns>
        protected bool setError(int status, string msg)
        {
            this.error = msg;
            reply["status"] = status;
            reply["message"] = msg;
            return (status > 0 ? false : true);
        }

        /// <summary>
        /// String cleanse for extras matching.
        /// </summary>
        /// <param name="str"></param>
        /// <returns>string Cleansed string</returns>
        public string extraCleanStr(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            foreach (string item in extraUAFilterList)
            {
                foreach (var itemChar in item)
                {
                    str = str.Replace(itemChar, ' ');
                }
            }
            Regex reg = new Regex("[^(\x20-\x7F)]*");
            str = reg.Replace(str, "");
            return Regex.Replace(str, @"\s+", "");
        }

        /// <summary>
        /// Standard string cleanse for device matching
        /// </summary>
        /// <param name="str"></param>
        /// <returns>string cleansed string</returns>
        public string cleanStr(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;
            foreach (string item in deviceUAFilterList)
            {
                foreach (var itemChar in item)
                {
                    str = str.Replace(itemChar, ' ');
                }

            }
            Regex reg = new Regex("[^(\x20-\x7F)]*");
            str = reg.Replace(str, "");
            return Regex.Replace(str, @"\s+", "");
        }

        /// <summary>
        /// Pre processes the request and try different servers on error/timeout
        /// </summary>
        /// <param name="data"></param>
        /// <param name="service">Service strings vary depending on the information needed</param>
        /// <returns>JsonData</returns>
        protected bool Remote(string suburl, Dictionary<string, dynamic> data, string filetype = "json", bool authRequired = true)
        {
            reply = new Dictionary<string, dynamic>();
            this.rawReply = new Dictionary<string, dynamic>();
            this.setError(0, "OK");

            var jss = new JavaScriptSerializer();
            jss.MaxJsonLength = this.maxJsonLength;

            string request;
            string requestUrl = apiBase + suburl;
            int attempts = config["retries"] + 1;
            int trys = 0;
            if (data == null || data.Count == 0)
                request = "";
            else
                request = jss.Serialize(data);

            bool status = false;
            bool success = false;
            // Uri url = new Uri("http://" + ApiServer + "/apiv4" + service);

            try
            {
                while (trys++ < attempts && success == false)
                {
                    status = post(config["api_server"], requestUrl, request, authRequired);
                    if (status)
                    {
                        reply = jss.Deserialize<Dictionary<string, dynamic>>(rawreply);
                        if (filetype.ToLower() == "json")
                        {
                            if (reply.Count == 0)
                            {
                                setError(299, "Error: Empty Reply.");
                            }
                            else if (!reply.ContainsKey("status"))
                            {
                                setError(299, "Error : No status set in reply");
                            }
                            else if (Convert.ToInt32(reply["status"]) != 0)
                            {
                                setError(reply["status"], reply["message"]);
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
                this.setError(299, "Exception : " + ex.Message + " " + ex.StackTrace);
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
        private bool post(string server, string service, string jsondata, bool authRequired = true)
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
                    var httpResponse = (HttpWebResponse)req.GetResponse();
                    responseStream = new MemoryStream();
                    responseStream = httpResponse.GetResponseStream();
                    reader = new BinaryReader(responseStream);

                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
                        if (this.isDownloadableFiles)
                        {
                            rawreply = "{\"status\":0}";
                        }
                        else
                        {
                            rawreply = new StreamReader(responseStream).ReadToEnd();
                        }
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                this.setError(299, "Exception : " + ex.Message + " " + ex.StackTrace);
            }
            return false;
        }

        /// <summary>
        /// Helper for determining if a header has BiKeys
        /// </summary>
        /// <param name="branchName"></param>
        /// <returns>platform name on success, false otherwise</returns>
        public dynamic hasBiKeys(Dictionary<string, dynamic> headers)
        {
            Dictionary<string, dynamic> biKeys = detectionConfig["device-bi-order"];
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

            int count = 0;
            int total = 0;
            foreach (KeyValuePair<string, dynamic> platform in biKeys)
            {
                List<List<string>> set = platform.Value;
                foreach (var tuple in set)
                {
                    List<string> tupleSet = tuple;
                    count = 0;
                    total = tupleSet.Count();
                    foreach (var item in tupleSet)
                    {
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
        public dynamic getMatch(string header, string value, string subtree = "0", string actualHeader = "", string className = "device")
        {
            int f = 0;
            int r = 0;
            string treetag;
            value = value.ToLower();
            if (className.ToLower() == "device")
            {
                value = cleanStr(value);
                treetag = header + subtree;
            }
            else
            {
                value = extraCleanStr(value);
                treetag = header + subtree;
            }

            if (string.IsNullOrEmpty(value) || value.Length < 4)
            {
                return false;
            }
            Dictionary<string, dynamic> branch = getBranch(treetag);
            string node = string.Empty;
            if (branch == null)
            {
                return false;
            }

            if (header.ToLower() == "user-agent")
            {

                foreach (var order in branch)
                {
                    Dictionary<string, dynamic> filters = order.Value;
                    foreach (var filter in filters)
                    {
                        ++f;
                        Dictionary<string, dynamic> matches = filter.Value;
                        if (value.Contains(filter.Key))
                        {
                            foreach (var match in matches)
                            {
                                ++r;
                                if (value.Contains(match.Key))
                                {
                                    detectedRuleKey[className] = cleanStr(header) + ":" + cleanStr(filter.Key) + ":" + cleanStr(match.Key);
                                    return match.Value;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (branch.ContainsKey(value))
                {
                    node = branch[value];
                    return node;
                }
            }


            return false;
        }

        /// <summary>
        /// Find a branch for the matching process
        /// </summary>
        /// <param name="branchName">The name of the branch to find</param>
        /// <returns>an assoc array on success, false otherwise.</returns>
        public Dictionary<string, dynamic> getBranch(string branchName)
        {
            if (tree.ContainsKey(branchName) && tree[branchName] != null)
            {
                return tree[branchName];
            }

            Dictionary<string, dynamic> tmp = Store.read(branchName);
            if (tmp != null)
            {
                tree[branchName] = tmp;
                return tmp;
            }
            return null;
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



        private HDStore Store = null;
        public bool UseProxy
        {
            get
            {
                return config["use_proxy"];
            }
            set
            {
                config["use_proxy"] = value;
            }
        }
        public string Username
        {
            get
            {
                return config["username"];
            }
            set
            {
                config["username"] = value;
            }
        }
        public string Secret
        {
            get
            {
                return config["secret"];
            }
            set
            {
                config["secret"] = value;
            }
        }

        public string ProxyServer
        {
            get
            {
                return config["proxy_server"];
            }
            set
            {
                config["proxy_server"] = value;
            }
        }

        public int ProxyPort
        {
            get
            {
                return config["proxy_port"];
            }
            set
            {
                config["proxy_port"] = value;
            }
        }
        public string ProxyUser
        {
            get
            {
                return config["proxy_user"];
            }
            set
            {
                config["proxy_user"] = value;
            }
        }

        public string ProxyPass
        {
            get
            {
                return config["proxy_pass"];
            }
            set
            {
                config["proxy_pass"] = value;
            }
        }

        public Dictionary<string, dynamic> detectRequest = new Dictionary<string, dynamic>();

        public void setDetectVar(string key, string value) { detectRequest[key.ToLower()] = value; }






        public int ReadTimeout
        {
            get
            {
                return config["timeout"];
            }
            set
            {
                config["timeout"] = value;
            }
        }
        private Stream responseStream = null;
        protected static BinaryReader reader { get; set; }
        public bool isDownloadableFiles = false;

        public List<string> deviceUAFilterList;
        public List<string> extraUAFilterList;
        protected static string rawreply;
        protected string log;
        protected Dictionary<string, dynamic> rawReply = null;
        protected Dictionary<string, dynamic> tree = new Dictionary<string, dynamic>();
        public int maxJsonLength = 40000000;


        protected string error = "";



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
        protected void setRawReply()
        {
            var jss = new JavaScriptSerializer();
            jss.MaxJsonLength = maxJsonLength;
            rawreply = jss.Serialize(reply);
        }

        public string getRawReply() { return rawreply; }
        public string ApiServer { get; set; }







    }
}
