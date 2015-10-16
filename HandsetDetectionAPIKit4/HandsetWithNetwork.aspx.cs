﻿using HandsetDetectionAPI;
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
    public partial class HandsetWithNetwork : System.Web.UI.Page
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

            // What handset have this attribute ?
            Response.Write("<h1>Nokia Models</h1><p>");
            if (objHD4.deviceWhatHas("network","CDMA"))
            {
                Response.Write(objHD4.getRawReply());
            }
            else
            {
                Response.Write(objHD4.getError());
            }
            Response.Write("</p>");
        }
    }
}