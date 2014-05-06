using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HD3;
using SecretConfiguraton;
using System.Diagnostics;
using System.IO;
using System.Configuration;
using System.Reflection;
using System.Xml;
using System.Collections;

namespace HD3.Test
{
    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public class HD3Test
    {
        private HD3 hd3;
        private SecretConfig secretConfig;
        
        /// <summary>
        /// 
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            hd3 = new HD3();
            secretConfig = new SecretConfig();
        }

        /// <summary>
        /// Test HD3 with wrong user config.
        /// </summary>
        [TestMethod]
        public void Test_HD3WrongCredentials()
        {            
            Assert.AreEqual<string>(hd3.Username, "your_api_username");
            Assert.AreEqual<string>(hd3.Secret, "your_api_secret");
            Assert.AreEqual<string>(hd3.SiteId, "your_api_siteId");
        }

        /// <summary>
        /// Test HD3 with correct user config.
        /// </summary>
        [TestMethod]
        public void Test_HD3CorrectCredentials()
        {
            Assert.AreEqual<string>(hd3.Username, 
                secretConfig.GetConfigUsername());
            Assert.AreEqual<string>(hd3.Secret, 
                secretConfig.GetConfigSecret());
            Assert.AreEqual<string>(hd3.SiteId, 
                secretConfig.GetConfigSiteId());
        }

        /// <summary>
        /// Test Site detect
        /// </summary>
        [TestMethod]
        public void Test_SiteDetect()
        {
            Assert.IsFalse(hd3.siteDetect());
        }

        /// <summary>
        /// Test Site detect in local mode
        /// </summary>
        [TestMethod]
        public void Test_SiteDetectLocal()
        {
            Assert.IsTrue(hd3.siteDetect());
        }

        /// <summary>
        /// Test Device Vendors with wrong username.
        /// </summary>
        [TestMethod]
        public void Test_DeviceVendorsWithWrongUsername()
        {
            Assert.AreEqual(hd3.Username, "your_api_username");   
            Assert.IsFalse(hd3.deviceVendors());                  
        } 

        /// <summary>
        /// Test Device Vendors with correct username.
        /// </summary>
        [TestMethod]
        public void Test_DeviceVendorsWithCorrectUsername()
        {
            Assert.AreEqual(hd3.Username, secretConfig.GetConfigUsername());            
            Assert.IsTrue(hd3.deviceVendors());            
        } 

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void Test_DeviceModelsNokia()
        {
            hd3.deviceModels("Nokia");
            Assert.IsTrue(hd3.getRawReply().Contains("model"));
        } 

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void Test_DeviceModelsLorem()
        {
            hd3.deviceModels("Nokia");
            Assert.IsFalse(hd3.getRawReply().Contains("model"));
        } 
       
        /*static void Main(string[] args)
        {          
            var hd3 = new HD3();            
        } */
    }    
}
