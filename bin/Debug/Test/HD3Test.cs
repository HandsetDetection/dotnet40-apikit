using System;
using HD3;
using System.Diagnostics;
using System.IO;
using System.Configuration;
using System.Reflection;
using System.Xml;
using System.Collections;

namespace HD3.Test
{
    using NUnit.Framework;

    [TestFixture]
    public class HD3Test
    {
        private HD3 hd3;

        [TestFixtureSetUp]
        public void Initialize()
        {
            hd3 = new HD3
            {
                Username = "your_api_username",
                Secret = "your_api_secret",
                SiteId = "your_api_siteId",
                UseLocal = true
            };
        } 

        [Test]
        public void Test_HD3UserCredentials()
        {            
            Assert.AreEqual(hd3.Username, "your_api_username");
            Assert.AreEqual(hd3.Secret, "your_api_secret");
            Assert.AreEqual(hd3.SiteId, "your_api_siteId");            
        }

        [Test]
        public void Test_SiteDetect()
        {
            Assert.IsFalse(hd3.siteDetect());
        }

        [Test]
        public void Test_NokiaSiteDetect()
        {
            hd3.setDetectVar("user-agent", "Mozilla/5.0 (SymbianOS/9.2; U; Series60/3.1 NokiaN95-3/20.2.011 Profile/MIDP-2.0 Configuration/CLDC-1.1 ) AppleWebKit/413");
            hd3.setDetectVar("x-wap-profile", "http://nds1.nds.nokia.com/uaprof/NN95-1r100.xml");
            hd3.siteDetect();
            dynamic reply = hd3.getReply();
            Assert.AreEqual("Nokia", reply["hd_specs"]["general_vendor"]);
            Assert.AreEqual("Symbian", reply["hd_specs"]["general_platform"]);
        }

        [Test]
        public void Test_GeoipSiteDetect()
        {
            hd3.setDetectVar("ipaddress", "64.34.165.180");
            Hashtable openWith = new Hashtable();
            openWith.Add("options", "geoip,hd_specs");
            hd3.siteDetect(openWith["options"].ToString());            
            dynamic reply = hd3.getReply();
            Assert.AreEqual("38.9266", reply["geoip"]["latitude"]);
            Assert.AreEqual("US", reply["geoip"]["countrycode"]);
        }

        [Test]
        public void Test_SiteDetectLocal()
        {
            Assert.IsTrue(hd3.siteDetect());
        }

        [Test]
        public void Test_DeviceVendorsFound()
        {
            hd3.deviceVendors();
            var reply = hd3.getReply();
            string key = "vendor";
            Assert.IsTrue(InJsonList("Asus", key, reply));
            Assert.IsTrue(InJsonList("Satellite", key, reply));
            Assert.IsTrue(InJsonList("Tecno", key, reply));
        }

        [Test]
        public void Test_DeviceVendorsNotFound()
        {
            hd3.deviceVendors();
            var reply = hd3.getReply();
            string key = "vendor";
            Assert.IsFalse(InJsonList("Flame", key, reply));
            Assert.IsFalse(InJsonList("Xeon", key, reply));
            Assert.IsFalse(InJsonList("Advance", key, reply));
        }

        [Test]
        public void Test_DeviceVendorsWithUsername()
        {
            Assert.AreEqual(hd3.Username, "your_api_username");   
            Assert.IsFalse(hd3.deviceVendors());                  
        }

        [Test]
        public void Test_DeviceModelsNokiaPass()
        {
            hd3.deviceModels("Nokia");
            var reply = hd3.getReply();
            string key = "model";
            Assert.IsTrue(InJsonList("3310i", key, reply));
            Assert.IsTrue(InJsonList("Lumia 610 NFC", key, reply));
            Assert.IsTrue(InJsonList("2720 Fold", key, reply));
            Assert.IsTrue(InJsonList("1110i", key, reply));            
        }

        [Test]
        public void Test_DeviceModelsNokiaFail()
        {
            hd3.deviceModels("Nokia");
            var reply = hd3.getReply();
            string key = "model";
            Assert.IsFalse(InJsonList("5050i", key, reply));
            Assert.IsFalse(InJsonList("x120", key, reply));
            Assert.IsFalse(InJsonList("10101", key, reply));
            Assert.IsFalse(InJsonList("abc123", key, reply));
        }

        [Test]
        public void Test_DeviceViewNokia95()
        {
            Assert.IsTrue(hd3.deviceView("Nokia", "N95"));
            dynamic reply = hd3.getReply();
            Assert.AreEqual(reply["device"]["general_vendor"], "Nokia");
            Assert.AreEqual(reply["device"]["general_model"], "N95");
            Assert.AreEqual(reply["device"]["general_platform"], "Symbian");
            Assert.IsTrue(InJsonMultiList("Alarm", "device", "features", reply));
            Assert.IsTrue(InJsonMultiList("Push-to-Talk", "device", "features", reply));
            Assert.IsTrue(InJsonMultiList("Computer sync", "device", "features", reply));
            Assert.IsTrue(InJsonMultiList("VoIP", "device", "features", reply));
        }
        [Test]
        public void Test_DeviceViewAppleIPhone5s()
        {
            Assert.IsTrue(hd3.deviceView("Apple", "IPhone 5s"));
            dynamic reply = hd3.getReply();
            Assert.AreEqual(reply["device"]["general_vendor"], "Apple");
            Assert.AreEqual(reply["device"]["general_model"], "iPhone 5S");
            Assert.IsTrue(InJsonMultiList("Video Call", "device", "features", reply));
            Assert.IsTrue(InJsonMultiList("AGPS", "device", "features", reply));
            Assert.IsTrue(InJsonMultiList("LED Flash", "device", "features", reply));
            Assert.IsTrue(InJsonMultiList("Electronic Compass", "device", "features", reply));
        }

        [Test]
        public void Test_DeviceViewXCode()
        {
            Assert.IsFalse(hd3.deviceView("XCode", "XC14"));
            dynamic reply = hd3.getReply();
            Assert.AreEqual(reply["device"]["general_vendor"], "Apple");
            Assert.AreEqual(reply["device"]["general_model"], "XC14");
            Assert.AreEqual(reply["device"]["general_platform"], "iOS");
        }

        [Test]
        public void Test_DeviceWhatHas()
        {
            hd3.ReadTimeout = 600;
            hd3.deviceWhatHas("network", "cdma");
            dynamic reply = hd3.getReply();
            Assert.AreEqual(reply["devices"][0]["id"], 10);
            Assert.AreEqual(reply["devices"][0]["general_vendor"], "Samsung");
            Assert.AreEqual(reply["devices"][0]["general_model"], "SPH-A680");
            Assert.AreEqual(reply["devices"][1]["id"], 1003);
            Assert.AreEqual(reply["devices"][1]["general_vendor"], "LG");
            Assert.AreEqual(reply["devices"][1]["general_model"], "CU6060");
            Assert.AreEqual(reply["devices"][2]["id"], 1020);
            Assert.AreEqual(reply["devices"][2]["general_vendor"], "Nokia");
            Assert.AreEqual(reply["devices"][2]["general_model"], "2270");
            Assert.AreEqual(reply["status"], 0);
        }

        [Test]
        public void Test_DeviceWhatHasFalse()
        {
            Assert.IsFalse(hd3.deviceWhatHas("cloud", "wifi"));            
        }

        [TestFixtureTearDown]
        public void TearDown() { hd3.cleanUp(); }

        [Ignore]
        public bool InJsonList(string value, string key, dynamic reply)
        {
            foreach (var data in reply[key])
            {
                if (data == value)
                    return true;
            }
            return false;
        }

        [Ignore]
        public bool InJsonMultiList(string value, string key1, string key2, dynamic reply)
        {
            foreach (var data in reply[key1][key2])
            {
                if (data == value)
                    return true;
            }
            return false;
        }

    }    
}
