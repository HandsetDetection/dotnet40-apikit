using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Xml;

namespace HD3.Test
{
    /// <summary>
    /// 
    /// </summary>
    public class SecretConfig : Hashtable
    {                
        /// <summary>
        /// 
        /// </summary>
        public SecretConfig()
        {
            NameValueCollection appSettings = System.Configuration.ConfigurationManager.AppSettings;
            string path = System.IO.Path.GetFullPath("HD3Web/Web.config");
            XmlDocument document = new XmlDocument();
            document.Load(new StreamReader(path));
            foreach (XmlNode node in document["configuration"]["appSettings"])
            {
                if ((node.NodeType != XmlNodeType.Comment) && !this.Contains(node.Attributes["key"].Value))
                {                    
                    this[node.Attributes["key"].Value] = node.Attributes["value"].Value;
                }
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public String GetConfigUsername() {
            return this["username"].ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public String GetConfigSecret() {
            return this["secret"].ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public String GetConfigSiteId()
        {
            return this["site_id"].ToString();
        }
    }
}