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
    public class HDBase
    {
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
        private BinaryReader reader { get; set; }
        public bool isDownloadableFiles = false;
        string apiBase = "/apiv4/";
        string deviceUAFilter = " _\\#-,./:\"'";
        string extraUAFilter = " ";
        public List<string> deviceUAFilterList;
        public List<string> extraUAFilterList;
        protected static string rawreply;
        protected string log;
        protected Dictionary<string, dynamic> reply = null;
        protected Dictionary<string, dynamic> rawReply = null;
        protected Dictionary<string, dynamic> tree = null;
        protected int maxJsonLength = 40000000;

        protected Dictionary<string, dynamic> detectedRuleKey = new Dictionary<string, dynamic>();

        protected string error = "";

        protected Dictionary<string, dynamic> config = new Dictionary<string, dynamic>() {{"username", ""},
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

        public string ApplicationRootDirectory
        {
            get
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        //TODO: To get data from json file
        protected Dictionary<string, dynamic> detectionConfig
        {
            get
            {

                Dictionary<string, dynamic> dicData = new Dictionary<string, dynamic>();

                dicData.Add("device-ua-order", new string[] { "x-operamini-phone-ua", "x - mobile - ua", "device-stock-ua", "user-agent", "agent" });
                dicData.Add("platform-ua-order", new string[] { "x-operamini-phone-ua", "x - mobile - ua", "device-stock-ua", "user-agent", "agent" });
                dicData.Add("browser-ua-order", new string[] { "user-agent", "agent", "device-stock-ua" });
                dicData.Add("app-ua-order", new string[] { "user-agent", "agent", "device-stock-ua" });
                dicData.Add("language-ua-order", new string[] { "user-agent", "agent", "device-stock-ua" });

                Dictionary<string, dynamic> dicDeviceBiOrder = new Dictionary<string, dynamic>();

                Dictionary<string, string> dicAndroid = new Dictionary<string, string>();
                dicAndroid.Add("ro.product.brand", "ro.product.model");
                dicAndroid.Add("ro.product.manufacturer", "ro.product.model");
                dicAndroid.Add("ro-product-brand", "ro-product-model");
                dicAndroid.Add("ro-product-manufacturer", "ro-product-model");
                dicDeviceBiOrder.Add("android", dicAndroid);

                Dictionary<string, string> dicIOS = new Dictionary<string, string>();
                dicIOS.Add("utsname.brand", "utsname.machine");

                dicDeviceBiOrder.Add("ios", dicIOS);

                Dictionary<string, string> dicWindowPhone = new Dictionary<string, string>();
                dicWindowPhone.Add("devicemanufacturer", "devicename");
                dicDeviceBiOrder.Add("windows phone", dicWindowPhone);

                dicData.Add("device-bi-order", dicDeviceBiOrder);

                Dictionary<string, dynamic> dicPlatformBiOrder = new Dictionary<string, dynamic>();

                Dictionary<string, string> dicPlatformAndroid = new Dictionary<string, string>();
                dicPlatformAndroid.Add("ro.build.id", "ro.build.version.release");
                dicPlatformAndroid.Add("ro-build-id", "ro-build-version-release");

                dicPlatformBiOrder.Add("android", dicPlatformAndroid);

                Dictionary<string, string> dicPlatformIOS = new Dictionary<string, string>();
                dicPlatformIOS.Add("uidevice.systemName", "uidevice.systemversion");

                dicPlatformBiOrder.Add("ios", dicPlatformIOS);

                Dictionary<string, string> dicPlatformWindowPhone = new Dictionary<string, string>();
                dicPlatformWindowPhone.Add("osname", "osversion");

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


        public HDBase()
        {
            deviceUAFilterList = deviceUAFilter.Split(new String[] { "//" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            extraUAFilterList = extraUAFilter.Split(new String[] { "//" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            ReadTimeout = 120;
            Store = HDStore.Instance;
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
            this.reply["status"] = status;
            this.reply["message"] = msg;
            return (status > 0 ? false : true);
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

        /// <summary>
        /// Pre processes the request and try different servers on error/timeout
        /// </summary>
        /// <param name="data"></param>
        /// <param name="service">Service strings vary depending on the information needed</param>
        /// <returns>JsonData</returns>
        protected bool Remote(string suburl, Dictionary<string, string> data, string filetype = "json", bool authRequired = true)
        {
            this.reply = new Dictionary<string, dynamic>();
            this.rawReply = new Dictionary<string, dynamic>();
            this.setError(0, "OK");

            var jss = new JavaScriptSerializer();
            jss.MaxJsonLength = this.maxJsonLength;

            string request;
            string requestUrl = apiBase + suburl;
            int attempts = this.config["retries"] + 1;
            int trys = 0;
            if (data == null || data.Count == 0)
                request = "";
            else
                request = jss.Serialize(data);

            bool status = false;

            // Uri url = new Uri("http://" + ApiServer + "/apiv4" + service);

            try
            {
                //while (trys++ < attempts && status == false)
                //{

                //}
                status = post(config["api_server"], requestUrl, request, authRequired);
                if (status)
                {
                    status = true;
                    this.reply = jss.Deserialize<Dictionary<string, dynamic>>(rawreply);
                }
            }
            catch (Exception ex)
            {
                this.setError(299, "Exception : " + ex.Message + " " + ex.StackTrace);
            }
            return status;
        }

        private bool post(string server, string service, string jsondata, bool authRequired = true)
        {
            try
            {

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
                    this.reader = new BinaryReader(responseStream);

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


        // From : http://blogs.msdn.com/b/csharpfaq/archive/2006/10/09/how-do-i-calculate-a-md5-hash-from-a-string_3f00_.aspx
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
        public string cleanStr(string str)
        {
            foreach (string item in deviceUAFilterList)
            {
                str = str.Replace(item, "");
            }
            Regex reg = new Regex("[^(\x20-\x7F)]*");
            str = reg.Replace(str, "");
            return str;
        }

        public string extraCleanStr(string str)
        {
            foreach (string item in extraUAFilterList)
            {
                str = str.Replace(item, "");
            }
            Regex reg = new Regex("[^(\x20-\x7F)]*");
            str = reg.Replace(str, "");
            return str;
        }

        public dynamic getMatch(string header, string value, string subtree = "0", string actualHeader = "", string className = "device")
        {
            int f = 0;
            int r = 0;
            string treetag;
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
            Dictionary<string, dynamic> node;
            if (branch == null)
            {
                return false;
            }

            if (header.ToLower() == "user-agent")
            {

                //// Sieve matching strategy
                //            foreach((array) $branch as $order => $filters) {
                //                foreach((array) $filters as $filter => $matches) {
                //                    ++$f;
                //                    if (strpos($value, (string) $filter) !== false) {
                //                        foreach((array) $matches as $match => $node) {
                //                            ++$r;
                //                            if (strpos($value, (string) $match) !== false) {
                //                                $this->detectedRuleKey[$class] = $this->cleanStr(@$header).':'.$this->cleanStr(@$filter).':'.$this->cleanStr(@$match);
                //                                return $node;
                //                            }
                //                        }
                //                    }
                //                }
                //            }
            }
            else
            {
                if (branch[value] != null)
                {
                    node = branch[value];
                    return node;
                }
            }


            return false;
        }


        public Dictionary<string, dynamic> getBranch(string branchName)
        {
            if (tree[branchName] != null)
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
                var set = (Dictionary<string, dynamic>)platform.Value;
                foreach (var tuple in set)
                {
                    var tupleSet = (Dictionary<string, dynamic>)tuple.Value;
                    count = 0;
                    total = tupleSet.Values.Count();
                    foreach (var item in tupleSet)
                    {
                        if (dataKeys.Contains(item.Key))
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


    }
}
