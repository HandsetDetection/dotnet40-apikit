/*
** Copyright (c) 2009-2012
** Richard Uren <richard@teleport.com.au>
** All Rights Reserved
**
** --
**
** LICENSE: Redistribution and use in source and binary forms, with or
** without modification, are permitted provided that the following
** conditions are met: Redistributions of source code must retain the
** above copyright notice, this list of conditions and the following
** disclaimer. Redistributions in binary form must reproduce the above
** copyright notice, this list of conditions and the following disclaimer
** in the documentation and/or other materials provided with the
** distribution.
**
** THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESS OR IMPLIED
** WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
** MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN
** NO EVENT SHALL CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
** INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
** BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS
** OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
** ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR
** TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
** USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH
** DAMAGE.
**
** --
**
** This is a reference implementation for interfacing with www.handsetdetection.com apiv3
**
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Web;
using System.Drawing;
using System.Security.Cryptography;
using System.Globalization;
using System.Web.SessionState;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Configuration;
using System.Runtime.Caching;

namespace HD3 {

    public class HD3Cache {
        private int maxJsonLength = 20000000;
        string prefix = "hd32-";
        ObjectCache myCache;
        CacheItemPolicy policy = new CacheItemPolicy();
        public HD3Cache() {
            policy.AbsoluteExpiration = new DateTimeOffset(DateTime.Now.AddHours(24));
            NameValueCollection CacheSettings = new NameValueCollection(3);
            CacheSettings.Add("CacheMemoryLimitMegabytes", Convert.ToString(200));
            //myCache = new MemoryCache("HD3Cache", CacheSettings);
            this.myCache = MemoryCache.Default;
        }

        public void write(string key, Dictionary<string, dynamic> value) {
           if (value != null && key != "") {
                var jss = new JavaScriptSerializer();
                jss.MaxJsonLength = this.maxJsonLength;
                string storethis = jss.Serialize(value);
                this.myCache.Set(this.prefix+key, storethis, policy);
            }
        }

        public Dictionary<string, dynamic> read(string key) {
            try {
                //return myCache.Get(key) as Dictionary<string, dynamic>;
                string fromCache = this.myCache.Get(this.prefix+key) as string;
                var jss = new JavaScriptSerializer();
                jss.MaxJsonLength = this.maxJsonLength;
                return jss.Deserialize<Dictionary<string, dynamic>>(fromCache);
                //return myCache.Get(key) as Dictionary<string, dynamic>;
            } catch (Exception ex) {
                // Not in cache
                return null;
            }
        }
     }

    /// <summary>
    /// Main class for all handset detection API calls
    /// </summary>
    public class HD3 {
        int maxJsonLength = 20000000;
        int read_timeout = 10;
        int connect_timeout = 10;
        string username = "";
        string secret = "";
        string site_id = "";
        bool use_local = false;
        bool use_proxy = false;
        string proxy_server = "";
        int proxy_port = 80;
        string proxy_user = "";
        string proxy_pass = "";
        string match_filter = " _\\#-,./:\"'";
        string non_mobile = "^Feedfetcher|^FAST|^gsa_crawler|^Crawler|^goroam|^GameTracker|^http://|^Lynx|^Link|^LegalX|libwww|^LWP::Simple|FunWebProducts|^Nambu|^WordPress|^yacybot|^YahooFeedSeeker|^Yandex|^MovableType|^Baiduspider|SpamBlockerUtility|AOLBuild|Link Checker|Media Center|Creative ZENcast|GoogleToolbar|MEGAUPLOAD|Alexa Toolbar|^User-Agent|SIMBAR|Wazzup|PeoplePal|GTB5|Dealio Toolbar|Zango|MathPlayer|Hotbar|Comcast Install|WebMoney Advisor|OfficeLiveConnector|IEMB3|GTB6|Avant Browser|America Online Browser|SearchSystem|WinTSI|FBSMTWB|NET_lghpset";
        string api_server = "api.handsetdetection.com";
        string log_server = "log.handsetdetection.com";

        public int ReadTimeout { get; set; }
        public int ConnectTimeout { get; set; }
        public string Username { get; set; }
        public string Secret { get; set; }
        public string SiteId { get; set; }
        public bool UseLocal { get; set; }
        public bool UseProxy { get; set; }
        public string ProxyServer { get; set; }
        public string ProxyPort { get; set; }
        public string ProxyPass { get; set; }
        public string ProxyUser { get; set; }
        public string MatchFilter { get; set; }
        public string NonMobile { get; set; }
        public string ApiServer { get; set; }
        public string LogServer { get; set; }

        public string getRawReply() { return this.rawreply;  }
        public dynamic getReply() { return this.reply; }
        public string getError() { return this.error; }
        private void setError(string msg) { this.error = msg; _log("ERROR : "+msg);}
        private void setRawReply() {
            var jss = new JavaScriptSerializer();
            jss.MaxJsonLength = this.maxJsonLength;
            this.rawreply = jss.Serialize(this.reply);
        }

        private HD3Cache myCache = new HD3Cache();
        //Parameters to send for detection request
        private Dictionary<string, string> m_detectRequest = new Dictionary<string, string>();
        private string rawreply;
        private Dictionary<string, dynamic> reply = new Dictionary<string, dynamic>();
        private Dictionary<string, dynamic> tree = new Dictionary<string, dynamic>();
        private Dictionary<string, dynamic> specs = new Dictionary<string, dynamic>();
        private string error = "";
        private HttpRequest Request;
        public string log = "";

        #region "Init constructors"
        /// <summary>
        /// Initializes the necessary information for a lookup from request object
        /// Accepts Object inializers 
        /// </summary>
        /// <param name="request">HttpRequest object from page</param>
        public HD3(HttpRequest request) {
            this.Request = request;
            NameValueCollection appSettings = System.Configuration.ConfigurationManager.AppSettings;
            if (appSettings["username"] != null)
                this.username = appSettings["username"];
            if (appSettings["secret"] != null)
                this.secret = appSettings["secret"];
            if (appSettings["site_id"] != null)
                this.site_id = appSettings["site_id"];
            if (appSettings["use_local"] != null)
                this.use_local = Convert.ToBoolean(appSettings["use_local"]);
            if (appSettings["use_proxy"] != null)
                this.use_proxy = Convert.ToBoolean(appSettings["use_proxy"]);
            if (appSettings["match_filter"] != null)
                this.match_filter = appSettings["match_filter"];
            if (appSettings["api_server"] != null)
                this.api_server = appSettings["api_server"];
            if (appSettings["log_server"] != null)
                this.log_server = appSettings["log_server"];

            Regex reg = new Regex("^x|^http", RegexOptions.IgnoreCase);
            foreach (string header in request.Headers) {
                if (reg.IsMatch(header)) {
                    AddKey(header.ToLower(), request[header]);
                }
            }
            AddKey("user-agent", request.UserAgent);
            AddKey("ipaddress", request.UserHostAddress);
            AddKey("request_uri", request.Url.ToString());
        }

        /// <summary>Sets additional http headers for detection request, will override default headers.</summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        public void setDetectVar(string key, string val) { AddKey(key, val); }

        private void AddKey(string key, string value) {
            key = key.ToLower();
            if (this.m_detectRequest.ContainsKey(key)) {
                this.m_detectRequest.Remove(key);
            }
            _log("Added httpheader " + key + " " + value);
            this.m_detectRequest.Add(key, value);
        }

        public void resetLog() { this.log = ""; }
        private void _log(string msg) { this.log = this.log + "<br\\>\n" +msg; }
        public string getLog() { return this.log; }
        public void cleanUp() { this.rawreply = ""; this.reply = new Dictionary<string, dynamic>(); }
        #endregion

        #region "API functions"

        /// <summary>
        /// Pre processes the request and try different servers on error/timeout
        /// </summary>
        /// <param name="data"></param>
        /// <param name="service">Service strings vary depending on the information needed</param>
        /// <returns>JsonData</returns>
        private bool Remote(string service, Dictionary<string, string> data) {
            bool status;
            string request;
            this.reply = null;
            var jss = new JavaScriptSerializer();
            jss.MaxJsonLength = this.maxJsonLength;

            Uri url = new Uri("http://" + this.api_server + "/apiv3" + service);
            _log("Preparing to send to " + "http://" + this.api_server + "/apiv3" + service);
            if (data == null || data.Count == 0)
                request = "";
            else
                request = jss.Serialize(data);
            
            try {
                status = post(this.api_server, url, request);
                if (status) {
                    this.reply = jss.Deserialize<Dictionary<string, dynamic>>(this.rawreply);
                }
                if (this.reply == null || ! this.reply.ContainsKey("status")) {
                    this.setError("Empty Reply");
                    return false;
                }
                if (this.reply["status"] != 0) {
                    this.setError(this.reply["status"].ToString() + " : " + this.reply["message"]);
                    return false;
                }
                return true;
            } catch (Exception ex) {
                this.setError("Exception : " + ex.Message + " " + ex.StackTrace);
                return false;
            }
        }

        private bool post(string host, Uri url, string data) {
            try {
                IPAddress[] ipv4Addresses = Array.FindAll(Dns.GetHostEntry(this.api_server).AddressList, a => a.AddressFamily == AddressFamily.InterNetwork);
                // ToDo : Randomize the order of entries in ipList

                foreach (IPAddress ip in ipv4Addresses) {
                    //_log("Sending to server " + ip.ToString());
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                    req.ServicePoint.BindIPEndPointDelegate = delegate(ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retryCount) {
                        return new IPEndPoint(IPAddress.Any, 0);
                    };

                    if (this.use_proxy) {
                        WebProxy proxy = new WebProxy(this.proxy_server, this.proxy_port);
                        proxy.Credentials = new NetworkCredential(this.proxy_user, this.proxy_pass);
                        req.Proxy = proxy;
                    }
                    req.Timeout = this.read_timeout * 1000;
                    //req.PreAuthenticate = true;
                    req.Method = "POST";
                    req.ContentType = "application/json";

                    // AuthDigest Components - 
                    // Precomputing the digest saves on the server having to issue a challenge so its much quicker (network wise)
                    // http://en.wikipedia.org/wiki/Digest_access_authentication
                    string realm = "APIv3";
                    string nc = "00000001";
                    string snonce = "APIv3";
                    string cnonce = _helperMD5Hash(DateTime.Now.ToString() + this.secret);
                    string qop = "auth";
                    string ha1 = _helperMD5Hash(this.username + ":" + realm + ":" + this.secret);
                    string ha2 = _helperMD5Hash("POST:" + url.PathAndQuery);
                    string response = _helperMD5Hash(ha1 + ":" + snonce + ":" + nc + ":" + cnonce + ":" + qop + ":" + ha2);
                    string digest = "Digest username=\"" + username + "\", realm=\"" + realm + "\", nonce=\"" + snonce + "\", uri=\"" + url.PathAndQuery + "\", qop=" + qop + ", nc=" + nc + ", cnonce=\"" + cnonce + "\", response=\"" + response + "\", opaque=\"" + realm + "\"";
                    //_log("ha1 : " + ha1);
                    //_log("ha2 : " + ha2);
                    //_log("response : " + response);
                    //_log("digest : " + digest);
                    byte[] payload = System.Text.Encoding.ASCII.GetBytes(data);
                    req.ContentLength = payload.Length;
                    req.Headers.Add("Authorization", digest);
                    _log("Send Headers: " + req.ToString());
                    _log("Send Data: " + data);
                    Stream dataStream = req.GetRequestStream();
                    dataStream.Write(payload, 0, payload.Length);
                    dataStream.Close();

                    var httpResponse = (HttpWebResponse) req.GetResponse();
                    var streamReader = new StreamReader(httpResponse.GetResponseStream());
                    this.rawreply = streamReader.ReadToEnd();
                    _log("Received : " + this.rawreply);

                    streamReader.Close();
                    httpResponse.Close();
                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                        return true;
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                this.setError("Exception : " + ex.Message + " " + ex.StackTrace);
            }

            return false;
        }

        /// <summary>Fetches all supported Vendors available at handsetdetection.com</summary>
        /// <returns>true if successful, false otherwise</returns>
        public bool deviceVendors() {
            resetLog();
            try {
                if (this.use_local)
                    return _localDeviceVendors();
                else
                    return Remote("/device/vendors.json", null);
            } catch (Exception ex) {
                this.setError("Exception : " + ex.Message + " " + ex.StackTrace);
                return false;
            }
        }

        private bool _localDeviceVendors() {
            Dictionary <string, dynamic> data = _localGetSpecs();
            if (data == null)
                return false;

            // If _localGetSpecs bails return false here
            var temp = new HashSet<string>();
            foreach (Dictionary<string, dynamic> item in data["devices"]) {
                temp.Add(item["Device"]["hd_specs"]["general_vendor"].ToString());
            }
            this.reply = new Dictionary<string, dynamic>();
            this.reply["vendor"] = temp;
            this.reply["status"] = 0;
            this.reply["message"] = "OK";
            this.setRawReply();
            return true;
        }

        /// <summary>
        /// Fetches all available phone models in handsetdetection.com database. If a vendor is specified then
        /// only models for that vendor are returned. Call getModel() to get access to the returned list.
        /// </summary>
        /// <param name="vendor">all or a valid vendor name</param>
        /// <returns>true if successful, false otherwise</returns>
        public bool deviceModels(string vendor) {
            resetLog();
            try {
                if (this.use_local) {
                    return _localDeviceModels(vendor);
                } else {
                    return Remote("/device/models/" + vendor + ".json", null);
                }
            } catch (Exception ex) {
                this.setError("Exception : " + ex.Message + " " + ex.StackTrace);
                return false;
            }
        }

        private bool _localDeviceModels(string vendor) {
            Dictionary<string, dynamic> data = _localGetSpecs();
            if (data == null)
                return false;

            HashSet<string> temp = new HashSet<string>();
            foreach (Dictionary<string, dynamic> item in data["devices"]) {
                if (vendor == (item["Device"]["hd_specs"]["general_vendor"].ToString())) {
                    temp.Add(item["Device"]["hd_specs"]["general_model"].ToString());
                }
                string key = vendor + " ";
                if (item["Device"]["hd_specs"]["general_aliases"].ToString() != "") {
                    foreach (string alias_item in item["Device"]["hd_specs"]["general_aliases"]) {
                        int result = alias_item.IndexOf(key);
                        if (result == 0) {
                            temp.Add(alias_item.Replace(key, ""));
                        }
                    }
                }
            }
            this.reply = new Dictionary<string, dynamic>();
            this.reply["model"] = temp;
            this.reply["status"] = 0;
            this.reply["message"] = "OK";
            this.setRawReply();
            return true;
        }


        /// <summary>
        /// Provides information on a handset given the vendor and model.
        /// </summary>
        /// <param name="vendor">vendor</param>
        /// <param name="model">model</param>
        /// <returns>true if successful, false otherwise</returns>
        public bool deviceView(string vendor, string model) {
            resetLog();
            try {
                if (this.use_local) {
                    return _localDeviceView(vendor, model);
                } else {
                    return Remote("/device/view/" + vendor + "/" + model + ".json", null);
                }
            } catch (Exception ex) {
                this.setError("Exception : " + ex.Message + " " + ex.StackTrace);
                return false;
            }
        }

        private bool _localDeviceView(string vendor, string model) {
            Dictionary<string, dynamic> data = _localGetSpecs();
            if (data == null)
                return false;

            vendor = vendor.ToLower();
            model = model.ToLower();
            foreach (Dictionary<string, dynamic> item in data["devices"]) {
                if (vendor == (item["Device"]["hd_specs"]["general_vendor"].ToString().ToLower()) && model == item["Device"]["hd_specs"]["general_model"].ToString().ToLower()) {
                    this.reply = new Dictionary<string, dynamic>(); 
                    this.reply["device"] = item["Device"]["hd_specs"];
                    this.reply["status"] = 0;
                    this.reply["message"] = "OK";
                    this.setRawReply();
                    return true;
                }
            }
            this.reply = new Dictionary<string, dynamic>();
            this.reply["status"] = 301;
            this.reply["message"] = "Nothing found";
            this.setRawReply();
            return false;
        }

        /// <summary>
        /// Provides information on a handset given the key and value.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="value">value</param>
        /// <returns></returns>
        public bool deviceWhatHas(string key, string value) {
            resetLog();
            try {
                if (this.use_local) {
                    return _localDeviceWhatHas(key, value);
                } else {
                    return Remote("/device/whathas/" + key + "/" + value + ".json", null);
                }
            } catch (Exception ex) {
                this.setError("Exception : " + ex.Message + " " + ex.StackTrace);
                return false;
            }
        }

        private bool _localDeviceWhatHas(string key, string value) {
            Dictionary<string, dynamic> data = this._localGetSpecs();
            if (data == null)
                return false;

            value = value.ToLower();
            key = key.ToLower();
            string s="";
            Type sType = s.GetType();

            var temp = new ArrayList();
            foreach (Dictionary<string, dynamic> item in data["devices"]) {
                if (item["Device"]["hd_specs"][key].ToString() == "")
                    continue;
                
                var match = false;

                if (item["Device"]["hd_specs"][key].GetType() == sType) {
                    string check = item["Device"]["hd_specs"][key].ToString().ToLower();
                    if (check.IndexOf(value) >= 0)
                        match = true;
                } else {
                    foreach (string check in item["Device"]["hd_specs"][key]) {
                        string tmpcheck = check.ToLower();
                        if (tmpcheck.IndexOf(value) >= 0)
                            match = true;
                    }                    
                }
                
                if (match == true) {
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
            return true;
        }
        
        public bool siteDetect(string options="hd_specs") {
            resetLog();

            if (this.m_detectRequest.ContainsKey("user-agent")) {
                Regex reg = new Regex(this.non_mobile, RegexOptions.IgnoreCase);
                if (reg.IsMatch(this.m_detectRequest["user-agent"].ToString())) {
                    _log("FastFail : Probable bot, sprider, script, or desktop");
                    this.reply = new Dictionary<string, dynamic>();
                    this.reply["status"] = 301;
                    this.reply["message"] = "FastFail : Probable bot, sprider, script, or desktop";
                    this.setRawReply();
                    return false;
                } else {
                    _log("No fastfail found");
                }
            } else {
                _log("user-agent not set");
            }
 
            try {
                if (this.use_local) {
                    return _localSiteDetect(this.m_detectRequest);
                } else {
                    this.AddKey("options", options);
                    return Remote("/site/detect/" + this.site_id + ".json", this.m_detectRequest);
                }
            } catch (Exception ex) {
                this.setError("Exception : " + ex.Message + " " + ex.StackTrace);
                return false;
            }
        }

        private bool _localSiteDetect(Dictionary<string, string> headers) {
            Dictionary<string, dynamic> device = null;
            Dictionary<string, dynamic> platform = null;
            Dictionary<string, dynamic> browser = null;

            int id = _getDevice(headers);
            if (id > 0) {
                device = _getCacheSpecs(id, "device");
			    if (device == null) {
                    this.reply = new Dictionary<string, dynamic>();
				    this.reply["status"] = 225;
				    this.reply["class"] = "Unknown";
				    this.reply["message"] = "Unable to write cache or main datafile.";
				    this.setError(this.reply["message"].ToString());
                    this.setRawReply();
				    return false;
			    }

                // Perform Browser & OS (platform) detection
			    int platform_id = _getExtra("platform", headers);
			    int browser_id = _getExtra("browser", headers);
			    if (platform_id > 0) 
				    platform = _getCacheSpecs(platform_id, "extra");
			    if (browser_id > 0)
				    browser = _getCacheSpecs(browser_id, "extra");
				
			    // Selective merge
			    if (browser != null && browser.ContainsKey("general_browser")) {
				    platform["general_browser"] = browser["general_browser"];
				    platform["general_browser_version"] = browser["general_browser_version"];
			    }	
			    if (platform != null && platform.ContainsKey("general_platform")) {
				    device["general_platform"] = platform["general_platform"];
				    device["general_platform_version"] = platform["general_platform_version"];	
			    }
			    if (platform != null && platform.ContainsKey("general_browser")) {
				    device["general_browser"] = platform["general_browser"];
				    device["general_browser_version"] = platform["general_browser_version"];	
			    }
                if (!device.ContainsKey("general_browser")) {
                    device["general_browser"] = "";
                    device["general_browser_version"] = "";
                }
                if (!device.ContainsKey("general_platform")) {
                    device["general_platform"] = "";
                    device["general_platform_version"] = "";
                }
                var jss = new JavaScriptSerializer();
                jss.MaxJsonLength = this.maxJsonLength;
                _log(jss.Serialize(device));

                this.reply = new Dictionary<string, dynamic>();								
			    this.reply["hd_specs"] = device;
			    this.reply["status"] = 0;
			    this.reply["message"] = "OK";
			    this.reply["class"] = (device["general_type"].ToString() == "" ? "Unknown" : device["general_type"]);
                this.setRawReply();
			    return true;
		    }

		    if (this.reply == null || ! this.reply.ContainsKey("status")) {
                this.reply = new Dictionary<string, dynamic>();
			    this.reply["status"] = 301;
			    this.reply["class"] = "Unknown";
			    this.reply["message"] = "Nothing found";
			    this.setError("Error: 301, Nothing Found");
                this.setRawReply();
		    }
		    return false;
	    }

        private int _getDevice(Dictionary<string, string> headers) {
            int id;
		    // Remember the agent for generic matching later.
		    string genericAgent = "";
            if (headers.ContainsKey("user-agent"))
                genericAgent = headers["user-agent"];

		    this._log("Working with headers of " + _helperDictToString(headers));
		    this._log("Start Checking Opera Special headers");
		    // Opera mini puts the vendor # model in the header - nice! ... sometimes it puts ? # ? in as well :(
		    if (headers.ContainsKey("x-operamini-phone") && headers["x-operamini-phone"].ToString() != "? # ?") {
			    id = this._tryHeader(ref headers, "x-operamini-phone", "x-operamini-phone");
			    if (id > 0)
				    return id;
		    }
             // Profile header matching
            id = this._tryHeader(ref headers, "profile", "profile");
            if (id > 0) return id;
			id = this._tryHeader(ref headers, "profile", "x-wap-profile");
			if (id > 0) return id;
            // Various types of user-agent x-header matching, order is important here (for the first 3).
            id = this._tryHeader(ref headers, "user-agent", "x-operamini-phone-ua");
            if (id > 0) return id;
            id = this._tryHeader(ref headers, "user-agent", "x-mobile-ua");
            if (id > 0) return id;
            id = this._tryHeader(ref headers, "user-agent", "user-agent");
            if (id > 0) return id;
            // Try anything else thats left		
		    foreach(KeyValuePair <string, string> item in headers) {
                id = this._tryHeader(ref headers, "user-agent", item.Value);
                if (id > 0) return id;
		    }

		    // Generic matching - Match of last resort.
		    this._log("Trying Generic Match");
		    return this._matchDevice("user-agent", genericAgent, true);
	    }
        
        private int _tryHeader(ref Dictionary<string, string> headers, string field, string httpheader) {
            int id;
            this._log("Start device "+field+"/"+httpheader+" check against "+field);
		    if (headers.ContainsKey(httpheader)) {
			    id = this._matchDevice(field, headers[httpheader], false);
			    if (id > 0) {
				    this._log("End "+httpheader+" check against "+field+" Found");
				    return id;
			    }
			    headers.Remove(field);
		    }
            this._log("End device " + httpheader + " check against " + field + " Not Found");
            return 0;
        }

        private int _matchDevice(string header, string value, bool generic) {
		    // Strip unwanted chars from lower case version of value
            StringBuilder b = new StringBuilder(value.ToLower());
            foreach(char c in this.match_filter) {
                b.Replace(c.ToString(), string.Empty);
            }
            value = b.ToString();
            string treetag;
            if (generic == true)
                treetag = header+"1";
            else
                treetag = header+"0";
            return this._match(header, value, treetag);
	    }   

	    // Tries headers in diffferent orders depending on the extra $class.
	    private int _getExtra(string extraclass, Dictionary <string, string> headers) {
            int id;
		    if (extraclass == "platform") {
                id = this._tryExtra(ref headers, "user-agent", "x-operamini-phone-ua", extraclass);
                if (id > 0) return id;
                id = this._tryExtra(ref headers, "user-agent", "user-agent", extraclass);
                if (id > 0) return id;
                // Try anything else thats left		
		        foreach(KeyValuePair <string, string> item in headers) {
                    id = this._tryExtra(ref headers, "user-agent", item.Value, extraclass);
                    if (id > 0) return id;
		        }
		    } else if (extraclass == "browser") {
                id = this._tryExtra(ref headers, "user-agent", "user-agent", extraclass);
                if (id > 0) return id;
                // Try anything else thats left		
		        foreach(KeyValuePair <string, string> item in headers) {
                    id = this._tryExtra(ref headers, "user-agent", item.Value, extraclass);
                    if (id > 0) return id;
		        }
		    }
            return 0;
	    }
		
	    private int _tryExtra(ref Dictionary <string, string> headers, string matchfield, string httpheader, string extraclass) {
            int id;
            this._log("Start Extra "+matchfield+"/"+httpheader+" check");
		    if (headers.ContainsKey(httpheader)) {
                string value = headers[httpheader].ToLower().Replace(" ","");
                string treetag = matchfield + extraclass;
			    id = this._match(httpheader, value, treetag);
			    if (id > 0) {
				    this._log("End "+matchfield+" check. Found");
				    return id;
			    }
			    headers.Remove(matchfield);
		    }
		    this._log("End Extra "+matchfield+"check - not found");
            return 0;
        }

        private int _match(string header, string newvalue, string treetag) {
		
		    int f = 0,r = 0;		
		    this._log("Loading "+treetag+" match "+newvalue); 

		    if (newvalue == "") {
			    this._log("Value empty - returning false");
			    return 0;
		    }
		
		    if (newvalue.Length < 4) {
			    this._log("Value " +newvalue+ " too small - returning false");
			    return 0;
		    }

		    this._log("Loading match branch "+treetag); 
		    Dictionary<string, dynamic> branch = this._getBranch(treetag);
		    if (branch == null) {
			    this._log("Match branch "+treetag+" empty - returning false");
			    return 0;
		    }
		    this._log("Match branch loaded");		
		
		    if (header == "user-agent") {		
			    // Sieve matching strategy
			    foreach(KeyValuePair <string, dynamic> orders in branch) {
                    foreach(KeyValuePair <string, dynamic> filters in orders.Value) {
					    f++;
                        this._log("Looking for "+filters.Key+" in "+newvalue);
					    if (newvalue.IndexOf(filters.Key) >= 0) {
						    foreach(KeyValuePair <string, dynamic> matches in filters.Value) {
                                r++;
                                this._log("Looking for " + matches.Key + " in "+newvalue);
							    if (newvalue.IndexOf(matches.Key) >= 0) {
								    this._log("Match Found : "+filters.Key+ "/"+matches.Key+"/" +matches.Value+" wins on "+newvalue+" ("+f+"/"+r+")");
								    return Convert.ToInt32(matches.Value.ToString());
							    }
						    }
					    }
				    }
			    }
		    } else {
			    // Direct matching strategy
                try {
                    int id = Convert.ToInt32(branch[newvalue]);
                    this._log("Match found : " + treetag + " " + newvalue + " (" + f + "/" + r + ")");
                    return id;
                } catch (Exception ex) {
                }
		    }
		
		    this._log("No Match Found for "+treetag+" "+newvalue+"("+f+"/"+r+")");
		    return 0;
	    }

	    private Dictionary<string, dynamic> _getBranch(string treetag) {
		    // See if its in the class
            if (this.tree.ContainsKey(treetag)) {
			    this._log(treetag + " fetched from memory");
			    return this.tree[treetag];
		    }

            // Not in class - try Cache.
            Dictionary<string, dynamic> obj = myCache.read(treetag);
            if (obj != null && obj.Count != 0) {
                this._log(treetag + " fetched from cache. count : "+obj.Count);
                this.tree[treetag] = obj;
                return obj;
            }

            // Its in neither - so populate both.
            this._setCacheTrees();

            // If it doesnt exist after immediate refresh then something is wrong.
		    if (! this.tree.ContainsKey(treetag))
                this.tree[treetag] = new Dictionary<string, dynamic>();
      
		    this._log(treetag + " built and cached");
		    return this.tree[treetag];
	    }

        #endregion

        public bool siteFetchAll() {
            bool status = false;
            status = this.siteFetchSpecs();
            if (!status)
                return false;
            status = this.siteFetchTrees();
            if (!status)
                return false;
            return true;
        }

        public bool siteFetchTrees() {
            resetLog();
            bool status = this.Remote("/site/fetchtrees/" + this.site_id + ".json", null);
            if (!status)
                return false;
            try {
                if (! this.reply.ContainsKey("status") || this.reply["status"] != 0) {
                    this.setError("siteFetchSpecs API call failed: " + this.reply["message"].ToString());
                    return false;
                }
                // Write rawreply to file hd3trees.json file.
                _localPutTrees();
            } catch (Exception ex) {
                this.setError("Exception : " + ex.Message + " " + ex.StackTrace);
                return false;
            }
            return _setCacheTrees();
        }

        private bool _setCacheTrees() {
            Dictionary<string, dynamic> data = _localGetTrees();
            if (data == null || ! data.ContainsKey("trees")) {
                this.reply = new Dictionary<string, dynamic>();
                this.reply["status"] = 299;
                this.reply["message"] = "Unable to open specs file hd3trees.json";
                this.setError("Error : 299, Message : _setCacheTrees cannot open hd3trees.json. Is it there ? Is it world readable ?");
                this.setRawReply();
                return false;
            }
            foreach (KeyValuePair<string, dynamic> branch in data["trees"]) {
                this.tree[branch.Key] = branch.Value as Dictionary<string, dynamic>;
                // Write to memory cache
                myCache.write(branch.Key, this.tree[branch.Key]);
            }
            return true;
        }

        public bool siteFetchSpecs() {
            resetLog();
            bool status = this.Remote("/site/fetchspecs/" + this.site_id + ".json", null);
            if (!status)
                return false;

            try {
                if (!this.reply.ContainsKey("status") || this.reply["status"] != 0) {
                    this.setError("siteFetchSpecs API call failed: " + this.reply["message"].ToString());
                    return false;
                }
                // Write rawreply to file hd3specs.json file.
                _localPutSpecs();
            } catch (Exception ex) {
                this.setError("Exception : " + ex.Message + " " + ex.StackTrace);
                return false;
            }
            return _setCacheSpecs();
        }

        private bool _setCacheSpecs() {
            Dictionary<string, dynamic> data = _localGetSpecs();

            if (data == null) {
                this.reply = new Dictionary<string, dynamic>();
                this.reply["status"] = 299;
                this.reply["message"] = "Unable to open specs file hd3specs.json";
                this.setError("Error : 299, Message : _setCacheSpecs cannot open hd3specs.json. Is it there ? Is it world readable ?");
                this.setRawReply();
                return false;
            }
            // Cache Devices
            foreach (Dictionary<string, dynamic> device in data["devices"]) {
                string device_id = device["Device"]["_id"];
                string key = "device"+device_id;
                if (device != null && device["Device"] != null && device["Device"]["hd_specs"] != null && key != null) {
                    this.specs[key] = device["Device"]["hd_specs"];
                    // Save to Application Cache
                    myCache.write(key, this.specs[key]);
                }
            }
            // Cache Extras
            foreach (Dictionary<string, dynamic> extra in data["extras"]) {
                string extra_id = extra["Extra"]["_id"];
                string key = "extra" + extra_id;
                if (extra["Extra"] != null && extra["Extra"]["hd_specs"] != null) {
                    this.specs[key] = extra["Extra"]["hd_specs"] as Dictionary<string, dynamic>;
                    // Save to Applications Cache
                    myCache.write(key, this.specs[key]);
                }
			}	
            return true;
        }

        private Dictionary<string, dynamic> _getCacheSpecs(int id, string type) {
            // Read local first
            string key = type + Convert.ToInt32(id);
            if (this.specs.ContainsKey(key)) {
                this._log(key + " fetched from memory");
                return this.specs[key];
            }

            // Try Cache
            Dictionary<string, dynamic> obj = myCache.read(key);
            if (obj != null && obj.Count != 0) {
                this._log(key + " fetched from cache");
                this.specs[key] = obj;
                return obj;
            }

            // re-cache & re-read local.
            this._log(key + " not found - rebuilding");
            _setCacheSpecs();
            if (this.specs.ContainsKey(key)) {
                this._log(key + " found after rebuilding");
                return this.specs[key];
            }
            this._log(key + " not found");
            return null;
        }

        private Dictionary<string, dynamic> _localGetSpecs() {
            var jss = new JavaScriptSerializer();
            jss.MaxJsonLength = this.maxJsonLength;

            try {
                string jsonText = System.IO.File.ReadAllText(Request.PhysicalApplicationPath + "\\hd3specs.json");
                Dictionary<string, dynamic> data = jss.Deserialize<Dictionary<string, dynamic>>(jsonText);
                return data;
            } catch (Exception ex) {
                this.setError("Exception : " + ex.Message + " " + ex.StackTrace);
            }
            return null;
        }

        private Dictionary<string, dynamic> _localGetTrees() {
            var jss = new JavaScriptSerializer();
            jss.MaxJsonLength = this.maxJsonLength;

            try {
                string jsonText = System.IO.File.ReadAllText(Request.PhysicalApplicationPath + "\\hd3trees.json");
                Dictionary<string, dynamic> data = jss.Deserialize<Dictionary<string, dynamic>>(jsonText);
                return data;
            } catch (Exception ex) {
                this.setError("Exception : " + ex.Message + " " + ex.StackTrace);
            }
            return null;
        }

        private bool _localPutSpecs() {
            try {
                System.IO.File.WriteAllText(Request.PhysicalApplicationPath + "\\hd3specs.json", this.rawreply.ToString());
                return true;
            } catch (Exception ex) {
                this.setError("Exception : " + ex.Message + " " + ex.StackTrace);
            }
            return false;
        }

        private bool _localPutTrees() {
            try {
                System.IO.File.WriteAllText(Request.PhysicalApplicationPath + "\\hd3trees.json", this.rawreply.ToString());
                return true;
            } catch (Exception ex) {
                this.setError("Exception : " + ex.Message + " " + ex.StackTrace);
            }
            return false;
        }

        // From : http://www.dotnetperls.com/convert-dictionary-string
        private string _helperDictToString(Dictionary<string, string> d) {
	        // Build up each line one-by-one and them trim the end
	        StringBuilder builder = new StringBuilder();
	        foreach (KeyValuePair<string, string> pair in d) {
	            builder.Append(pair.Key).Append(":").Append(pair.Value).Append(',');
	        }
	        string result = builder.ToString();
	        // Remove the final delimiter
	        result = result.TrimEnd(',');
	        return result;
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
            for (int i = 0; i < hash.Length; i++) {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString().ToLower();
        }
    }
}