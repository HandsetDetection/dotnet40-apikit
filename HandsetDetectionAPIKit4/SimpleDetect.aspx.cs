using HandsetDetectionAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Web
{
     
    public partial class SimpleDetect : System.Web.UI.Page
    {
        public string configFile = "//hdconfig.json";
        public Dictionary<string, dynamic> config = new Dictionary<string, dynamic>();
        public HD4 objHD4;
        protected void Page_Load(object sender, EventArgs e)
        {
              // Ensure config file is setup
            if (!File.Exists(Server.MapPath(configFile)))
            {
                throw new Exception("Config file not found");
            }

            var serializer = new JavaScriptSerializer();
            string jsonText = System.IO.File.ReadAllText(Server.MapPath(configFile));
            config = serializer.Deserialize<Dictionary<string, dynamic>>(jsonText);

            if (config["username"] == "your_api_username")
            {
                throw new Exception("Please configure your username, secret and site_id");
            }

            objHD4 = new HD4(Request, configFile);

            // This manually sets the headers that a Nokia N95 would set.
            // Other agents you also might like to try
            // Mozilla/5.0 (BlackBerry; U; BlackBerry 9300; es) AppleWebKit/534.8+ (KHTML, like Gecko) Version/6.0.0.534 Mobile Safari/534.8+
            // Mozilla/5.0 (SymbianOS/9.2; U; Series60/3.1 NokiaN95-3/20.2.011 Profile/MIDP-2.0 Configuration/CLDC-1.1 ) AppleWebKit/413
            // Mozilla/5.0 (iPhone; U; CPU iPhone OS 4_3_3 like Mac OS X; en-us) AppleWebKit/533.17.9 (KHTML, like Gecko) Version/5.0.2 Mobile/8J2 Safari/6533.18.5
            Response.Write("<h1>Simple Detection - Setting Headers for an N95</h1><p>");
         //   objHD4.setDetectVar("user-agent","Mozilla/5.0 (SymbianOS/9.2; U; Series60/3.1 NokiaN95-3/20.2.011 Profile/MIDP-2.0 Configuration/CLDC-1.1 ) AppleWebKit/413");
//objHD4.setDetectVar("x-wap-profile","http://nds1.nds.nokia.com/uaprof/NN95-1r100.xml");
            //if (objHD4.siteDetect())
            //{
            //    Response.Write(objHD4.getRawReply());
            //}
            //else
            //{
            //    Response.Write(objHD4.getError());
            //}
            Response.Write("</p>");

        }
    }
}