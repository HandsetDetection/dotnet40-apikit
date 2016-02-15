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
    public partial class Sites : System.Web.UI.Page
    {
        public string ConfigFile = "//hdUltimateConfig.json";
        public Dictionary<string, dynamic> Config = new Dictionary<string, dynamic>();
        public Hd4 ObjHd4;
        protected void Page_Load(object sender, EventArgs e)
        {
            // Ensure config file is setup
            if (!File.Exists(Server.MapPath(ConfigFile)))
            {
                throw new Exception("Config file not found");
            }

            var serializer = new JavaScriptSerializer();
            string jsonText = System.IO.File.ReadAllText(Server.MapPath(ConfigFile));
            Config = serializer.Deserialize<Dictionary<string, dynamic>>(jsonText);

            if (Config["username"] == "your_api_username")
            {
                throw new Exception("Please configure your username, secret and site_id");
            }

            ObjHd4 = new Hd4(Request, ConfigFile);

            /// Vendors example : Get a list of all vendors
            /// 

            Response.Write("<h1>Vendors</h1><p>");
            if (ObjHd4.DeviceVendors())
            {
                Response.Write(ObjHd4.GetRawReply());
            }
            else
            {
                Response.Write(ObjHd4.GetError());
            }
            Response.Write("</p>");
        }
    }
}