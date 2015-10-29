using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

namespace HandsetDetectionAPI
{
    [TestFixture]
    public class testHD4
    {
        private HD4 objHD4;
        string cloudConfig = "/hdCloudConfig.json"; //Cloud Config Name
        string ultimateConfig = "/hdUltimateConfig.json"; // Ultimate Config Name
        JavaScriptSerializer jss = new JavaScriptSerializer();

        Dictionary<string, dynamic> devices = new Dictionary<string, dynamic>();

        [SetUp]
        public void test0_initialSetup()
        {

            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, cloudConfig);
            jss.MaxJsonLength = objHD4.maxJsonLength;

            if (!devices.ContainsKey("NokiaN95"))
            {
                string noikaN95JsonText = "{\"general_vendor\":\"Nokia\",\"general_model\":\"N95\",\"general_platform\":\"Symbian\",\"general_platform_version\":\"9.2\",\"general_browser\":\"\",\"general_browser_version\":\"\",\"general_image\":\"nokian95-1403496370-0.gif\",\"general_aliases\":[],\"general_eusar\":\"0.50\",\"general_battery\":[\"Li-Ion 950 mAh\",\"BL-5F\"],\"general_type\":\"Mobile\",\"general_cpu\":[\"Dual ARM 11\",\"332MHz\"],\"design_formfactor\":\"Dual Slide\",\"design_dimensions\":\"99 x 53 x 21\",\"design_weight\":\"120\",\"design_antenna\":\"Internal\",\"design_keyboard\":\"Numeric\",\"design_softkeys\":\"2\",\"design_sidekeys\":[\"Volume\",\"Camera\"],\"display_type\":\"TFT\",\"display_color\":\"Yes\",\"display_colors\":\"16M\",\"display_size\":\"2.6\\\"\",\"display_x\":\"240\",\"display_y\":\"320\",\"display_other\":[],\"memory_internal\":[\"160MB\",\"64MB RAM\",\"256MB ROM\"],\"memory_slot\":[\"microSD\",\"8GB\",\"128MB\"],\"network\":[\"GSM850\",\"GSM900\",\"GSM1800\",\"GSM1900\",\"UMTS2100\",\"HSDPA2100\",\"Infrared\",\"Bluetooth 2.0\",\"802.11b\",\"802.11g\",\"GPRS Class 10\",\"EDGE Class 32\"],\"media_camera\":[\"5MP\",\"2592x1944\"],\"media_secondcamera\":[\"QVGA\"],\"media_videocapture\":[\"VGA@30fps\"],\"media_videoplayback\":[\"MPEG4\",\"H.263\",\"H.264\",\"3GPP\",\"RealVideo 8\",\"RealVideo 9\",\"RealVideo 10\"],\"media_audio\":[\"MP3\",\"AAC\",\"AAC+\",\"eAAC+\",\"WMA\"],\"media_other\":[\"Auto focus\",\"Video stabilizer\",\"Video calling\",\"Carl Zeiss optics\",\"LED Flash\"],\"features\":[\"Unlimited entries\",\"Multiple numbers per contact\",\"Picture ID\",\"Ring ID\",\"Calendar\",\"Alarm\",\"To-Do\",\"Document viewer\",\"Calculator\",\"Notes\",\"UPnP\",\"Computer sync\",\"VoIP\",\"Music ringtones (MP3)\",\"Vibration\",\"Phone profiles\",\"Speakerphone\",\"Accelerometer\",\"Voice dialing\",\"Voice commands\",\"Voice recording\",\"Push-to-Talk\",\"SMS\",\"MMS\",\"Email\",\"Instant Messaging\",\"Stereo FM radio\",\"Visual radio\",\"Dual slide design\",\"Organizer\",\"Word viewer\",\"Excel viewer\",\"PowerPoint viewer\",\"PDF viewer\",\"Predictive text input\",\"Push to talk\",\"Voice memo\",\"Games\"],\"connectors\":[\"USB\",\"MiniUSB\",\"3.5mm Audio\",\"TV Out\"],\"general_platform_version_max\":\"\",\"general_app\":\"\",\"general_app_version\":\"\",\"general_language\":\"\",\"display_ppi\":154,\"display_pixel_ratio\":\"1.0\",\"benchmark_min\":0,\"benchmark_max\":0,\"general_app_category\":\"\"}";
                devices.Add("NokiaN95", jss.Deserialize<Dictionary<string, dynamic>>(noikaN95JsonText));
            }

        }


        /// <summary>
        /// test for config file .. required for all cloud tests
        /// </summary>
        [Test]
        public void test1_cloudConfigExists()
        {
            string ApplicationPath = AppDomain.CurrentDomain.BaseDirectory;
            if (ApplicationPath.IndexOf("\\bin") >= 0)
            {
                ApplicationPath = ApplicationPath.Substring(0, ApplicationPath.IndexOf("\\bin"));
            }
            bool IsFileExist = File.Exists(ApplicationPath + "/" + cloudConfig);
            Assert.AreEqual(IsFileExist, true);
        }

        /// <summary>
        /// device vendors test
        /// </summary>
        [Test]
        public void test2_deviceVendors()
        {
            var result = objHD4.deviceVendors();
            var reply = objHD4.getReply();

            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);
            Assert.Contains("Nokia", reply["vendor"]);
            Assert.Contains("Samsung", reply["vendor"]);
        }

        /// <summary>
        /// device Models test
        /// </summary>
        [Test]
        public void test3_deviceModels()
        {
            var result = objHD4.deviceModels("Nokia");
            var reply = objHD4.getReply();
            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);
            Assert.IsNotEmpty(reply["message"]);
            Assert.Greater(reply["model"].Count, 700);
        }

        /// <summary>
        /// device view test
        /// </summary>
        [Test]
        public void test4_deviceView()
        {
            var result = objHD4.deviceView("Nokia", "N95");
            var reply = objHD4.getReply();
            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);
            Assert.AreEqual(jss.Serialize(devices["NokiaN95"]), jss.Serialize(reply["device"]));
        }

        /// <summary>
        /// device whatHas test
        /// </summary>
        [Test]
        public void test5_deviceDeviceWhatHas()
        {
            var result = objHD4.deviceWhatHas("design_dimensions", "101 x 44 x 16");
            var reply = objHD4.getReply();

            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);

            var jsonString = jss.Serialize(reply["devices"]);

            Assert.AreEqual(true, Regex.IsMatch(jsonString, "Asus"));
            Assert.AreEqual(true, Regex.IsMatch(jsonString, "V80"));
            Assert.AreEqual(true, Regex.IsMatch(jsonString, "Spice"));
            Assert.AreEqual(true, Regex.IsMatch(jsonString, "S900"));
            Assert.AreEqual(true, Regex.IsMatch(jsonString, "Voxtel"));
            Assert.AreEqual(true, Regex.IsMatch(jsonString, "RX800"));
        }

        /// <summary>
        /// Detection test Windows PC running Chrome
        /// </summary>
        [Test]
        public void test6_deviceDetectHTTPDesktop()
        {
            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36"}
            };
            var result = objHD4.deviceDetect(header);
            var reply = objHD4.getReply();
            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);
            Assert.AreEqual("Computer", reply["hd_specs"]["general_type"]);
        }

        /// <summary>
        /// Detection test Junk user-agent
        /// </summary>
        [Test]
        public void test7_deviceDetectHTTPDesktopJunk()
        {
            var header = new Dictionary<string, dynamic>(){
            {
                "User-Agent","aksjakdjkjdaiwdidjkjdkawjdijwidawjdiajwdkawdjiwjdiawjdwidjwakdjajdkad"}
            };
            var result = objHD4.deviceDetect(header);
            var reply = objHD4.getReply();
            Assert.IsFalse(result);
            Assert.AreEqual(301, reply["status"]);
            Assert.AreEqual("Not Found", reply["message"]);

        }

        /// <summary>
        ///  Detection test Wii
        /// </summary>
        [Test]
        public void test8_deviceDetectHTTPWii()
        {
            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","Opera/9.30 (Nintendo Wii; U; ; 2047-7; es-Es)"}
            
            };
            var result = objHD4.deviceDetect(header);
            var reply = objHD4.getReply();
            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);
            Assert.AreEqual("Console", reply["hd_specs"]["general_type"]);//Console
        }

        /// <summary>
        /// Detection test iPhone
        /// </summary>
        [Test]
        public void test9_deviceDetectHTTP()
        {
            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","Mozilla/5.0 (iPhone; U; CPU iPhone OS 4_3 like Mac OS X; en-gb) AppleWebKit/533.17.9 (KHTML, like Gecko)"}
            };
            var result = objHD4.deviceDetect(header);
            var reply = objHD4.getReply();

            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);

            Assert.AreEqual("Mobile", reply["hd_specs"]["general_type"]);
            Assert.AreEqual("Apple", reply["hd_specs"]["general_vendor"]);
            Assert.AreEqual("iPhone", reply["hd_specs"]["general_model"]);
            Assert.AreEqual("iOS", reply["hd_specs"]["general_platform"]);
            Assert.AreEqual("4.3", reply["hd_specs"]["general_platform_version"]);
            Assert.AreEqual("en-gb", reply["hd_specs"]["general_language"]);

            Dictionary<string, dynamic> handsetSpecs = reply["hd_specs"];
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_pixel_ratio"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_ppi"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_min"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_max"));

        }

        /// <summary>
        /// Detection test iPhone in weird headers
        /// </summary>
        [Test]
        public void test10_deviceDetectHTTPOtherHeader()
        {
            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","blahblahblah"}
            };
            header.Add("x-fish-header", "Mozilla/5.0 (iPhone; U; CPU iPhone OS 4_3 like Mac OS X; en-gb) AppleWebKit/533.17.9 (KHTML, like Gecko)");

            var result = objHD4.deviceDetect(header);
            var reply = objHD4.getReply();

            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);

            Assert.AreEqual("Mobile", reply["hd_specs"]["general_type"]);
            Assert.AreEqual("Apple", reply["hd_specs"]["general_vendor"]);
            Assert.AreEqual("iPhone", reply["hd_specs"]["general_model"]);
            Assert.AreEqual("iOS", reply["hd_specs"]["general_platform"]);
            Assert.AreEqual("4.3", reply["hd_specs"]["general_platform_version"]);
            Assert.AreEqual("en-gb", reply["hd_specs"]["general_language"]);

            Dictionary<string, dynamic> handsetSpecs = reply["hd_specs"];
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_pixel_ratio"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_ppi"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_min"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_max"));

        }

        /// <summary>
        /// Detection test iPhone 3GS (same UA as iPhone 3G, different x-local-hardwareinfo header)
        /// </summary>
        [Test]
        public void test11_deviceDetectHTTPHardwareInfo()
        {
            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","Mozilla/5.0 (iPhone; U; CPU iPhone OS 4_2_1 like Mac OS X; en-gb) AppleWebKit/533.17.9 (KHTML, like Gecko)"}
            };
            header.Add("x-local-hardwareinfo", "320:480:100:100");

            var result = objHD4.deviceDetect(header);
            var reply = objHD4.getReply();


            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);


            Assert.AreEqual("Mobile", reply["hd_specs"]["general_type"]);
            Assert.AreEqual("Apple", reply["hd_specs"]["general_vendor"]);
            Assert.AreEqual("iPhone 3GS", reply["hd_specs"]["general_model"]);
            Assert.AreEqual("iOS", reply["hd_specs"]["general_platform"]);
            Assert.AreEqual("4.2.1", reply["hd_specs"]["general_platform_version"]);
            Assert.AreEqual("en-gb", reply["hd_specs"]["general_language"]);

            Dictionary<string, dynamic> handsetSpecs = reply["hd_specs"];
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_pixel_ratio"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_ppi"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_min"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_max"));

        }

        /// <summary>
        /// Detection test iPhone 3G (same UA as iPhone 3GS, different x-local-hardwareinfo header)
        /// </summary>
        [Test]
        public void test12_deviceDetectHTTPHardwareInfoB()
        {
            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","Mozilla/5.0 (iPhone; U; CPU iPhone OS 4_2_1 like Mac OS X; en-gb) AppleWebKit/533.17.9 (KHTML, like Gecko)"}
            };
            header.Add("x-local-hardwareinfo", "320:480:100:72");

            var result = objHD4.deviceDetect(header);
            var reply = objHD4.getReply();


            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);


            Assert.AreEqual("Mobile", reply["hd_specs"]["general_type"]);
            Assert.AreEqual("Apple", reply["hd_specs"]["general_vendor"]);
            Assert.AreEqual("iPhone 3G", reply["hd_specs"]["general_model"]);
            Assert.AreEqual("iOS", reply["hd_specs"]["general_platform"]);
            Assert.AreEqual("4.2.1", reply["hd_specs"]["general_platform_version"]);
            Assert.AreEqual("en-gb", reply["hd_specs"]["general_language"]);

            Dictionary<string, dynamic> handsetSpecs = reply["hd_specs"];
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_pixel_ratio"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_ppi"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_min"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_max"));
        }

        /// <summary>
        /// Detection test iPhone - Crazy benchmark (eg from emulated desktop) with outdated OS
        /// </summary>
        [Test]
        public void test13_deviceDetectHTTPHardwareInfoC()
        {
            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","Mozilla/5.0 (iPhone; U; CPU iPhone OS 2_0 like Mac OS X; en-gb) AppleWebKit/533.17.9 (KHTML, like Gecko)"}
            };
            header.Add("x-local-hardwareinfo", "320:480:200:1200");

            var result = objHD4.deviceDetect(header);
            var reply = objHD4.getReply();


            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);


            Assert.AreEqual("Mobile", reply["hd_specs"]["general_type"]);
            Assert.AreEqual("Apple", reply["hd_specs"]["general_vendor"]);
            Assert.AreEqual("iPhone 3G", reply["hd_specs"]["general_model"]);
            Assert.AreEqual("iOS", reply["hd_specs"]["general_platform"]);
            Assert.AreEqual("2.0", reply["hd_specs"]["general_platform_version"]);
            Assert.AreEqual("en-gb", reply["hd_specs"]["general_language"]);

            Dictionary<string, dynamic> handsetSpecs = reply["hd_specs"];
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_pixel_ratio"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_ppi"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_min"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_max"));
        }

        /// <summary>
        /// Detection test iPhone 5s running Facebook 9.0 app (hence no general_browser set).
        /// </summary>
        [Test]
        public void test14_deviceDetectHTTPFBiOS()
        {
            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","Mozilla/5.0 (iPhone; CPU iPhone OS 7_1_1 like Mac OS X) AppleWebKit/537.51.2 (KHTML, like Gecko) Mobile/11D201 [FBAN/FBIOS;FBAV/9.0.0.25.31;FBBV/2102024;FBDV/iPhone6,2;FBMD/iPhone;FBSN/iPhone OS;FBSV/7.1.1;FBSS/2; FBCR/vodafoneIE;FBID/phone;FBLC/en_US;FBOP/5]"}
            };
            header.Add("Accept-Language", "da, en-gb;q=0.8, en;q=0.7");

            var result = objHD4.deviceDetect(header);
            var reply = objHD4.getReply();


            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);


            Assert.AreEqual("Mobile", reply["hd_specs"]["general_type"]);
            Assert.AreEqual("Apple", reply["hd_specs"]["general_vendor"]);
            Assert.AreEqual("iPhone 5S", reply["hd_specs"]["general_model"]);
            Assert.AreEqual("iOS", reply["hd_specs"]["general_platform"]);
            Assert.AreEqual("7.1.1", reply["hd_specs"]["general_platform_version"]);
            Assert.AreEqual("da", reply["hd_specs"]["general_language"]);
            Assert.AreEqual("Danish", reply["hd_specs"]["general_language_full"]);
            Assert.AreEqual("Facebook", reply["hd_specs"]["general_app"]);
            Assert.AreEqual("9.0", reply["hd_specs"]["general_app_version"]);
            Assert.AreEqual("", reply["hd_specs"]["general_browser"]);
            Assert.AreEqual("", reply["hd_specs"]["general_browser_version"]);


            Dictionary<string, dynamic> handsetSpecs = reply["hd_specs"];
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_pixel_ratio"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_ppi"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_min"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_max"));
        }

        /// <summary>
        /// Detection test Samsung GT-I9500 Native - Note : Device shipped with Android 4.2.2, so this device has been updated.
        /// </summary>
        [Test]
        public void test15_deviceDetectBIAndroid()
        {
            Dictionary<string, dynamic> buildInfo = new Dictionary<string, dynamic>();
            buildInfo.Add("ro.build.PDA", "I9500XXUFNE7");
            buildInfo.Add("ro.build.changelist", "699287");
            buildInfo.Add("ro.build.characteristics", "phone");
            buildInfo.Add("ro.build.date.utc", "1401287026");
            buildInfo.Add("ro.build.date", "Wed May 28 23:23:46 KST 2014");
            buildInfo.Add("ro.build.description", "ja3gxx-user 4.4.2 KOT49H I9500XXUFNE7 release-keys");
            buildInfo.Add("ro.build.display.id", "KOT49H.I9500XXUFNE7");
            buildInfo.Add("ro.build.fingerprint", "samsung/ja3gxx/ja3g:4.4.2/KOT49H/I9500XXUFNE7:user/release-keys");
            buildInfo.Add("ro.build.hidden_ver", "I9500XXUFNE7");
            buildInfo.Add("ro.build.host", "SWDD5723");
            buildInfo.Add("ro.build.id", "KOT49H");
            buildInfo.Add("ro.build.product", "ja3g");
            buildInfo.Add("ro.build.tags", "release-keys");
            buildInfo.Add("ro.build.type", "user");
            buildInfo.Add("ro.build.user", "dpi");
            buildInfo.Add("ro.build.version.codename", "REL");
            buildInfo.Add("ro.build.version.incremental", "I9500XXUFNE7");
            buildInfo.Add("ro.build.version.release", "4.4.2");
            buildInfo.Add("ro.build.version.sdk", "19");
            buildInfo.Add("ro.product.board", "universal5410");
            buildInfo.Add("ro.product.brand", "samsung");
            buildInfo.Add("ro.product.cpu.abi2", "armeabi");
            buildInfo.Add("ro.product.cpu.abi", "armeabi-v7a");
            buildInfo.Add("ro.product.device", "ja3g");
            buildInfo.Add("ro.product.locale.language", "en");
            buildInfo.Add("ro.product.locale.region", "GB");
            buildInfo.Add("ro.product.manufacturer", "samsung");
            buildInfo.Add("ro.product.model", "GT-I9500");
            buildInfo.Add("ro.product.name", "ja3gxx");
            buildInfo.Add("ro.product_ship", "true");


            var result = objHD4.deviceDetect(buildInfo);
            var reply = objHD4.getReply();

            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);


            Assert.AreEqual("Mobile", reply["hd_specs"]["general_type"]);
            Assert.AreEqual("Samsung", reply["hd_specs"]["general_vendor"]);
            Assert.AreEqual("GT-I9500", reply["hd_specs"]["general_model"]);
            Assert.AreEqual("Android", reply["hd_specs"]["general_platform"]);
            // Assert.AreEqual("4.4.2", reply["hd_specs"]["general_platform_version"]);
            Assert.AreEqual("Samsung Galaxy S4", reply["hd_specs"]["general_aliases"][0]);
        }

        /// <summary>
        /// Detection test iPhone 4S Native
        /// </summary>
        [Test]
        public void test16_deviceDetectBIiOS()
        {
            Dictionary<string, dynamic> buildInfo = new Dictionary<string, dynamic>();
            buildInfo.Add("utsname.machine", "iphone4,1");
            buildInfo.Add("utsname.brand", "Apple");

            var result = objHD4.deviceDetect(buildInfo);
            var reply = objHD4.getReply();


            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);

            Assert.AreEqual("Mobile", reply["hd_specs"]["general_type"]);
            Assert.AreEqual("Apple", reply["hd_specs"]["general_vendor"]);
            Assert.AreEqual("iPhone 4S", reply["hd_specs"]["general_model"]);
            Assert.AreEqual("iOS", reply["hd_specs"]["general_platform"]);
            // Note : Default shipped version in the absence of any version information
            Assert.AreEqual("5.0", reply["hd_specs"]["general_platform_version"]);


        }

        /// <summary>
        ///  Detection test Windows Phone Native Nokia Lumia 1020
        /// </summary>
        [Test]
        public void test17_deviceDetectWindowsPhone()
        {
            Dictionary<string, dynamic> buildInfo = new Dictionary<string, dynamic>();
            buildInfo.Add("devicemanufacturer", "nokia");
            buildInfo.Add("devicename", "RM-875");

            var result = objHD4.deviceDetect(buildInfo);
            var reply = objHD4.getReply();


            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);

            Assert.AreEqual("Mobile", reply["hd_specs"]["general_type"]);
            Assert.AreEqual("Nokia", reply["hd_specs"]["general_vendor"]);
            Assert.AreEqual("Lumia 1020", reply["hd_specs"]["general_model"]);
            Assert.AreEqual("Windows Phone", reply["hd_specs"]["general_platform"]);
            Assert.AreEqual(332, reply["hd_specs"]["display_ppi"]);



        }


        // ***************************************************************************************************
        // ***************************************** Ultimate Tests ******************************************
        // ***************************************************************************************************

        /// <summary>
        /// Fetch Archive Test
        /// </summary>
        [Test]
        public void test18_fetchArchive()
        {
            // Note : request storage dir to be created if it does not exist. (with TRUE as 2nd param)

            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);

            var result = objHD4.deviceFetchArchive();
            var reply = objHD4.getReply();

            Assert.IsTrue(result);
            //TODO: to get no. bytes
        }

        /// <summary>
        /// device vendors test
        /// </summary>
        [Test]
        public void test19_ultimate_deviceVendors()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);
            

            var result = objHD4.deviceVendors();
            var reply = objHD4.getReply();

            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);
            Assert.Contains("Nokia", reply["vendor"]);
            Assert.Contains("Samsung", reply["vendor"]);
        }

        /// <summary>
        /// device models test
        /// </summary>
        [Test]
        public void test20_ultimate_deviceModels()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);

            var result = objHD4.deviceModels("Nokia");
            var reply = objHD4.getReply();

            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);
            Assert.IsNotEmpty(reply["message"]);
            Assert.Greater(reply["model"].Count, 700);
        }

        /// <summary>
        /// device view test
        /// </summary>
        [Test]
        public void test21_ultimate_deviceView()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);

            var result = objHD4.deviceView("Nokia", "N95");
            var reply = objHD4.getReply();

            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);
            Assert.AreEqual(jss.Serialize(devices["NokiaN95"]), jss.Serialize(reply["device"]));

        }

        /// <summary>
        /// device whatHas test
        /// </summary>
        [Test]
        public void test22_ultimate_deviceDeviceWhatHas()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);

            var result = objHD4.deviceWhatHas("design_dimensions", "101 x 44 x 16");
            var reply = objHD4.getReply();

            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);

            var jsonString = jss.Serialize(reply["devices"]);

            Assert.AreEqual(true, Regex.IsMatch(jsonString, "Asus"));
            Assert.AreEqual(true, Regex.IsMatch(jsonString, "V80"));
            Assert.AreEqual(true, Regex.IsMatch(jsonString, "Spice"));
            Assert.AreEqual(true, Regex.IsMatch(jsonString, "S900"));
            Assert.AreEqual(true, Regex.IsMatch(jsonString, "Voxtel"));
            Assert.AreEqual(true, Regex.IsMatch(jsonString, "RX800"));
        }

        /// <summary>
        /// Windows PC running Chrome
        /// </summary>
        [Test]
        public void test23_ultimate_deviceDetectHTTPDesktop()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);

            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36"}
            };
            var result = objHD4.deviceDetect(header);
            var reply = objHD4.getReply();
            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);
            Assert.AreEqual("Computer", reply["hd_specs"]["general_type"]);
        }

        /// <summary>
        /// Junk user-agent
        /// </summary>
        [Test]
        public void test24_ultimate_deviceDetectHTTPDesktopJunk()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);

            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","aksjakdjkjdaiwdidjkjdkawjdijwidawjdiajwdkawdjiwjdiawjdwidjwakdjajdkad"}
            
            };
            var result = objHD4.deviceDetect(header);
            var reply = objHD4.getReply();
            Assert.IsFalse(result);
            Assert.AreEqual(301, reply["status"]);
            Assert.AreEqual("Not Found", reply["message"]);
        }

        /// <summary>
        /// Wii
        /// </summary>
        [Test]
        public void test25_ultimate_deviceDetectHTTPWii()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);

            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","Opera/9.30 (Nintendo Wii; U; ; 2047-7; es-Es)"}
            };

            var result = objHD4.deviceDetect(header);
            var reply = objHD4.getReply();

            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);
            Assert.AreEqual("Console", reply["hd_specs"]["general_type"]);
        }

        /// <summary>
        /// iPhone
        /// </summary>
        [Test]
        public void test26_ultimate_deviceDetectHTTP()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);

            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","Mozilla/5.0 (iPhone; U; CPU iPhone OS 4_3 like Mac OS X; en-gb) AppleWebKit/533.17.9 (KHTML, like Gecko)"}
            };
            var result = objHD4.deviceDetect(header);
            var reply = objHD4.getReply();

            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);

            Assert.AreEqual("Mobile", reply["hd_specs"]["general_type"]);
            Assert.AreEqual("Apple", reply["hd_specs"]["general_vendor"]);
            Assert.AreEqual("iPhone", reply["hd_specs"]["general_model"]);
            Assert.AreEqual("iOS", reply["hd_specs"]["general_platform"]);
            Assert.AreEqual("4.3", reply["hd_specs"]["general_platform_version"]);
            Assert.AreEqual("en-gb", reply["hd_specs"]["general_language"]);

            Dictionary<string, dynamic> handsetSpecs = reply["hd_specs"];
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_pixel_ratio"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_ppi"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_min"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_max"));

        }

        /// <summary>
        /// iPhone - user-agent in random other header
        /// </summary>
        [Test]
        public void test27_ultimate_deviceDetectHTTPOtherHeader()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);

            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","blahblahblah"}
            };
            header.Add("x-fish-header", "Mozilla/5.0 (iPhone; U; CPU iPhone OS 4_3 like Mac OS X; en-gb) AppleWebKit/533.17.9 (KHTML, like Gecko)");

            var result = objHD4.deviceDetect(header);
            var reply = objHD4.getReply();

            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);

            Assert.AreEqual("Mobile", reply["hd_specs"]["general_type"]);
            Assert.AreEqual("Apple", reply["hd_specs"]["general_vendor"]);
            Assert.AreEqual("iPhone", reply["hd_specs"]["general_model"]);
            Assert.AreEqual("iOS", reply["hd_specs"]["general_platform"]);
            Assert.AreEqual("4.3", reply["hd_specs"]["general_platform_version"]);
            Assert.AreEqual("en-gb", reply["hd_specs"]["general_language"]);

            Dictionary<string, dynamic> handsetSpecs = reply["hd_specs"];
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_pixel_ratio"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_ppi"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_min"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_max"));

        }

        /// <summary>
        /// iPhone 3GS (same UA as iPhone 3G, different x-local-hardwareinfo header)
        /// </summary>
        [Test]
        public void test28_ultimate_deviceDetectHTTPHardwareInfo()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);

            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","Mozilla/5.0 (iPhone; U; CPU iPhone OS 4_2_1 like Mac OS X; en-gb) AppleWebKit/533.17.9 (KHTML, like Gecko)"}
            };
            header.Add("x-local-hardwareinfo", "320:480:100:100");

            var result = objHD4.deviceDetect(header);
            var reply = objHD4.getReply();


            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);


            Assert.AreEqual("Mobile", reply["hd_specs"]["general_type"]);
            Assert.AreEqual("Apple", reply["hd_specs"]["general_vendor"]);
            Assert.AreEqual("iPhone 3GS", reply["hd_specs"]["general_model"]);
            Assert.AreEqual("iOS", reply["hd_specs"]["general_platform"]);
            Assert.AreEqual("4.2.1", reply["hd_specs"]["general_platform_version"]);
            Assert.AreEqual("en-gb", reply["hd_specs"]["general_language"]);

            Dictionary<string, dynamic> handsetSpecs = reply["hd_specs"];
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_pixel_ratio"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_ppi"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_min"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_max"));

        }

        /// <summary>
        /// iPhone 3G (same UA as iPhone 3GS, different x-local-hardwareinfo header)
        /// </summary>
        [Test]
        public void test29_ultimate_deviceDetectHTTPHardwareInfoB()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);

            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","Mozilla/5.0 (iPhone; U; CPU iPhone OS 4_2_1 like Mac OS X; en-gb) AppleWebKit/533.17.9 (KHTML, like Gecko)"}
            };
            header.Add("x-local-hardwareinfo", "320:480:100:72");

            var result = objHD4.deviceDetect(header);
            var reply = objHD4.getReply();


            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);


            Assert.AreEqual("Mobile", reply["hd_specs"]["general_type"]);
            Assert.AreEqual("Apple", reply["hd_specs"]["general_vendor"]);
            Assert.AreEqual("iPhone 3G", reply["hd_specs"]["general_model"]);
            Assert.AreEqual("iOS", reply["hd_specs"]["general_platform"]);
            Assert.AreEqual("4.2.1", reply["hd_specs"]["general_platform_version"]);
            Assert.AreEqual("en-gb", reply["hd_specs"]["general_language"]);

            Dictionary<string, dynamic> handsetSpecs = reply["hd_specs"];
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_pixel_ratio"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_ppi"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_min"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_max"));
        }

        /// <summary>
        /// iPhone - Crazy benchmark (eg from emulated desktop) with outdated OS
        /// </summary>
        [Test]
        public void test30_ultimate_deviceDetectHTTPHardwareInfoC()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);

            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","Mozilla/5.0 (iPhone; U; CPU iPhone OS 2_0 like Mac OS X; en-gb) AppleWebKit/533.17.9 (KHTML, like Gecko)"}
            };
            header.Add("x-local-hardwareinfo", "320:480:200:1200");

            var result = objHD4.deviceDetect(header);
            var reply = objHD4.getReply();


            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);


            Assert.AreEqual("Mobile", reply["hd_specs"]["general_type"]);
            Assert.AreEqual("Apple", reply["hd_specs"]["general_vendor"]);
            Assert.AreEqual("iPhone 3G", reply["hd_specs"]["general_model"]);
            Assert.AreEqual("iOS", reply["hd_specs"]["general_platform"]);
            Assert.AreEqual("2.0", reply["hd_specs"]["general_platform_version"]);
            Assert.AreEqual("en-gb", reply["hd_specs"]["general_language"]);

            Dictionary<string, dynamic> handsetSpecs = reply["hd_specs"];
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_pixel_ratio"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_ppi"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_min"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_max"));
        }

        /// <summary>
        /// iPhone 5s running Facebook 9.0 app (hence no general_browser set).
        /// </summary>
        [Test]
        public void test31_ultimate_deviceDetectHTTPFBiOS()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);

            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","Mozilla/5.0 (iPhone; CPU iPhone OS 7_1_1 like Mac OS X) AppleWebKit/537.51.2 (KHTML, like Gecko) Mobile/11D201 [FBAN/FBIOS;FBAV/9.0.0.25.31;FBBV/2102024;FBDV/iPhone6,2;FBMD/iPhone;FBSN/iPhone OS;FBSV/7.1.1;FBSS/2; FBCR/vodafoneIE;FBID/phone;FBLC/en_US;FBOP/5]"}
            };
            header.Add("Accept-Language", "da, en-gb;q=0.8, en;q=0.7");

            var result = objHD4.deviceDetect(header);
            var reply = objHD4.getReply();


            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);


            Assert.AreEqual("Mobile", reply["hd_specs"]["general_type"]);
            Assert.AreEqual("Apple", reply["hd_specs"]["general_vendor"]);
            Assert.AreEqual("iPhone 5S", reply["hd_specs"]["general_model"]);
            Assert.AreEqual("iOS", reply["hd_specs"]["general_platform"]);
            Assert.AreEqual("7.1.1", reply["hd_specs"]["general_platform_version"]);
            Assert.AreEqual("da", reply["hd_specs"]["general_language"]);
            Assert.AreEqual("Danish", reply["hd_specs"]["general_language_full"]);
            Assert.AreEqual("Facebook", reply["hd_specs"]["general_app"]);
            Assert.AreEqual("9.0", reply["hd_specs"]["general_app_version"]);
            Assert.AreEqual("", reply["hd_specs"]["general_browser"]);
            Assert.AreEqual("", reply["hd_specs"]["general_browser_version"]);


            Dictionary<string, dynamic> handsetSpecs = reply["hd_specs"];
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_pixel_ratio"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_ppi"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_min"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_max"));
        }

        /// <summary>
        /// Samsung GT-I9500 Native - Note : Device shipped with Android 4.2.2, so this device has been updated.
        /// </summary>
        [Test]
        public void test32_ultimate_deviceDetectBIAndroid()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);

            Dictionary<string, dynamic> buildInfo = new Dictionary<string, dynamic>();
            buildInfo.Add("ro.build.PDA", "I9500XXUFNE7");
            buildInfo.Add("ro.build.changelist", "699287");
            buildInfo.Add("ro.build.characteristics", "phone");
            buildInfo.Add("ro.build.date.utc", "1401287026");
            buildInfo.Add("ro.build.date", "Wed May 28 23:23:46 KST 2014");
            buildInfo.Add("ro.build.description", "ja3gxx-user 4.4.2 KOT49H I9500XXUFNE7 release-keys");
            buildInfo.Add("ro.build.display.id", "KOT49H.I9500XXUFNE7");
            buildInfo.Add("ro.build.fingerprint", "samsung/ja3gxx/ja3g:4.4.2/KOT49H/I9500XXUFNE7:user/release-keys");
            buildInfo.Add("ro.build.hidden_ver", "I9500XXUFNE7");
            buildInfo.Add("ro.build.host", "SWDD5723");
            buildInfo.Add("ro.build.id", "KOT49H");
            buildInfo.Add("ro.build.product", "ja3g");
            buildInfo.Add("ro.build.tags", "release-keys");
            buildInfo.Add("ro.build.type", "user");
            buildInfo.Add("ro.build.user", "dpi");
            buildInfo.Add("ro.build.version.codename", "REL");
            buildInfo.Add("ro.build.version.incremental", "I9500XXUFNE7");
            buildInfo.Add("ro.build.version.release", "4.4.2");
            buildInfo.Add("ro.build.version.sdk", "19");
            buildInfo.Add("ro.product.board", "universal5410");
            buildInfo.Add("ro.product.brand", "samsung");
            buildInfo.Add("ro.product.cpu.abi2", "armeabi");
            buildInfo.Add("ro.product.cpu.abi", "armeabi-v7a");
            buildInfo.Add("ro.product.device", "ja3g");
            buildInfo.Add("ro.product.locale.language", "en");
            buildInfo.Add("ro.product.locale.region", "GB");
            buildInfo.Add("ro.product.manufacturer", "samsung");
            buildInfo.Add("ro.product.model", "GT-I9500");
            buildInfo.Add("ro.product.name", "ja3gxx");
            buildInfo.Add("ro.product_ship", "true");


            var result = objHD4.deviceDetect(buildInfo);
            var reply = objHD4.getReply();

            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);


            Assert.AreEqual("Mobile", reply["hd_specs"]["general_type"]);
            Assert.AreEqual("Samsung", reply["hd_specs"]["general_vendor"]);
            Assert.AreEqual("GT-I9500", reply["hd_specs"]["general_model"]);
            Assert.AreEqual("Android", reply["hd_specs"]["general_platform"]);
            // Assert.AreEqual("4.4.2", reply["hd_specs"]["general_platform_version"]);
            Assert.AreEqual("Samsung Galaxy S4", reply["hd_specs"]["general_aliases"][0]);
        }

        /// <summary>
        /// iPhone 4S Native
        /// </summary>
        [Test]
        public void test33_ultimate_deviceDetectBIiOS()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);

            Dictionary<string, dynamic> buildInfo = new Dictionary<string, dynamic>();
            buildInfo.Add("utsname.machine", "iphone4,1");
            buildInfo.Add("utsname.brand", "Apple");

            var result = objHD4.deviceDetect(buildInfo);
            var reply = objHD4.getReply();


            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);

            Assert.AreEqual("Mobile", reply["hd_specs"]["general_type"]);
            Assert.AreEqual("Apple", reply["hd_specs"]["general_vendor"]);
            Assert.AreEqual("iPhone 4S", reply["hd_specs"]["general_model"]);
            Assert.AreEqual("iOS", reply["hd_specs"]["general_platform"]);
            Assert.AreEqual("5.0", reply["hd_specs"]["general_platform_version"]);


        }

        /// <summary>
        /// Windows Phone Native Nokia Lumia 1020
        /// </summary>
        [Test]
        public void test34_ultimate_deviceDetectWindowsPhone()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);

            Dictionary<string, dynamic> buildInfo = new Dictionary<string, dynamic>();
            buildInfo.Add("devicemanufacturer", "nokia");
            buildInfo.Add("devicename", "RM-875");

            var result = objHD4.deviceDetect(buildInfo);
            var reply = objHD4.getReply();


            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);

            Assert.AreEqual("Mobile", reply["hd_specs"]["general_type"]);
            Assert.AreEqual("Nokia", reply["hd_specs"]["general_vendor"]);
            Assert.AreEqual("Lumia 1020", reply["hd_specs"]["general_model"]);
            Assert.AreEqual("Windows Phone", reply["hd_specs"]["general_platform"]);
            Assert.AreEqual(332, reply["hd_specs"]["display_ppi"]);
        }

        // ***************************************************************************************************
        // *********************************** Ultimate Community Tests **************************************
        // ***************************************************************************************************

        /**
         * Fetch Archive Test
         *
         * The community fetchArchive version contains a cut down version of the device specs.
         * It has general_vendor, general_model, display_x, display_y, general_platform, general_platform_version,
         * general_browser, general_browser_version, general_app, general_app_version, general_language,
         * general_language_full, benahmark_min & benchmark_max
         *
         * @group community
         **/


        [Test]
        public void test35_ultimate_community_fetchArchive()
        {

            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);
            HDStore Store = HDStore.Instance;

            objHD4.isDownloadableFiles = true;
            var result = objHD4.communityFetchArchive();
            var data = objHD4.getReply();

            Assert.IsTrue(result);

            //TODO: to check and show bytes get

        }

        /// <summary>
        /// Windows PC running Chrome
        /// </summary>
        [Test]
        public void test36_ultimate_community_deviceDetectHTTPDesktop()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);


            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36"}
            };

            var result = objHD4.deviceDetect(header);
            var data = objHD4.getReply();

            Assert.IsTrue(result);
            Assert.AreEqual(0, data["status"]);
            Assert.AreEqual("OK", data["message"]);
            Assert.AreEqual("", data["hd_specs"]["general_type"]);
        }

        /// <summary>
        /// Junk user-agent
        /// </summary>
        [Test]
        public void test37_ultimate_community_deviceDetectHTTPDesktopJunk()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);

            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","aksjakdjkjdaiwdidjkjdkawjdijwidawjdiajwdkawdjiwjdiawjdwidjwakdjajdkad"+DateTime.Now.Ticks.ToString()}
            };

            var result = objHD4.deviceDetect(header);
            var data = objHD4.getReply();

            Assert.IsFalse(result);
            Assert.AreEqual(301, data["status"]);
            Assert.AreEqual("Not Found", data["message"]);

        }

        /// <summary>
        ///  Wii
        /// </summary>
        [Test]
        public void test38_ultimate_community_deviceDetectHTTPWii()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);

            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","Opera/9.30 (Nintendo Wii; U; ; 2047-7; es-Es)"}
            };

            var result = objHD4.deviceDetect(header);
            var data = objHD4.getReply();

            Assert.IsTrue(result);
            Assert.AreEqual(0, data["status"]);
            Assert.AreEqual("OK", data["message"]);
            Assert.AreEqual("", data["hd_specs"]["general_type"]);

        }

        /// <summary>
        /// iPhone
        /// </summary>
        [Test]
        public void test39_ultimate_community_deviceDetectHTTP()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);

            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","Mozilla/5.0 (iPhone; U; CPU iPhone OS 4_3 like Mac OS X; en-gb) AppleWebKit/533.17.9 (KHTML, like Gecko)"}
            };

            var result = objHD4.deviceDetect(header);
            var data = objHD4.getReply();

            Assert.IsTrue(result);
            Assert.AreEqual(0, data["status"]);
            Assert.AreEqual("OK", data["message"]);
            Assert.AreEqual("", data["hd_specs"]["general_type"]);

            Assert.AreEqual("", data["hd_specs"]["general_type"]);
            Assert.AreEqual("Apple", data["hd_specs"]["general_vendor"]);
            Assert.AreEqual("iPhone", data["hd_specs"]["general_model"]);
            Assert.AreEqual("iOS", data["hd_specs"]["general_platform"]);
            Assert.AreEqual("4.3", data["hd_specs"]["general_platform_version"]);
            Assert.AreEqual("en-gb", data["hd_specs"]["general_language"]);

            Dictionary<string, dynamic> handsetSpecs = data["hd_specs"];
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_pixel_ratio"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_ppi"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_min"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_max"));

        }

        /// <summary>
        /// iPhone - user-agent in random other header
        /// </summary>
        [Test]
        public void test40_ultimate_community_deviceDetectHTTPOtherHeader()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);

            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","blahblahblah"}
            };
            header.Add("x-fish-header", "Mozilla/5.0 (iPhone; U; CPU iPhone OS 4_3 like Mac OS X; en-gb) AppleWebKit/533.17.9 (KHTML, like Gecko)");

            var result = objHD4.deviceDetect(header);
            var data = objHD4.getReply();

            Assert.IsTrue(result);
            Assert.AreEqual(0, data["status"]);
            Assert.AreEqual("OK", data["message"]);
            Assert.AreEqual("", data["hd_specs"]["general_type"]);

            Assert.AreEqual("", data["hd_specs"]["general_type"]);
            Assert.AreEqual("Apple", data["hd_specs"]["general_vendor"]);
            Assert.AreEqual("iPhone", data["hd_specs"]["general_model"]);
            Assert.AreEqual("iOS", data["hd_specs"]["general_platform"]);
            Assert.AreEqual("4.3", data["hd_specs"]["general_platform_version"]);
            Assert.AreEqual("en-gb", data["hd_specs"]["general_language"]);
            Assert.AreEqual("", data["hd_specs"]["general_type"]);

            Dictionary<string, dynamic> handsetSpecs = data["hd_specs"];
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_pixel_ratio"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_ppi"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_min"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_max"));

        }

        /// <summary>
        /// iPhone 3GS (same UA as iPhone 3G, different x-local-hardwareinfo header)
        /// </summary>
        [Test]
        public void test41_ultimate_community_deviceDetectHTTPHardwareInfo()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);

            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","Mozilla/5.0 (iPhone; U; CPU iPhone OS 4_2_1 like Mac OS X; en-gb) AppleWebKit/533.17.9 (KHTML, like Gecko)"}
            };
            header.Add("x-fish-header", "320:480:100:100");

            var result = objHD4.deviceDetect(header);
            var data = objHD4.getReply();

            Assert.IsTrue(result);
            Assert.AreEqual(0, data["status"]);
            Assert.AreEqual("OK", data["message"]);
            Assert.AreEqual("", data["hd_specs"]["general_type"]);

            Assert.AreEqual("", data["hd_specs"]["general_type"]);
            Assert.AreEqual("Apple", data["hd_specs"]["general_vendor"]);
            Assert.AreEqual("iPhone 3GS", data["hd_specs"]["general_model"]);
            Assert.AreEqual("iOS", data["hd_specs"]["general_platform"]);
            Assert.AreEqual("4.2.1", data["hd_specs"]["general_platform_version"]);
            Assert.AreEqual("en-gb", data["hd_specs"]["general_language"]);
            Assert.AreEqual("", data["hd_specs"]["general_type"]);

            Dictionary<string, dynamic> handsetSpecs = data["hd_specs"];
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_pixel_ratio"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_ppi"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_min"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_max"));

        }

        /// <summary>
        /// iPhone 3G (same UA as iPhone 3GS, different x-local-hardwareinfo header)
        /// </summary>
        [Test]
        public void test42_ultimate_community_deviceDetectHTTPHardwareInfoB()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);

            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","Mozilla/5.0 (iPhone; U; CPU iPhone OS 4_2_1 like Mac OS X; en-gb) AppleWebKit/533.17.9 (KHTML, like Gecko)"}
            };
            header.Add("x-local-hardwareinfo", "320:480:100:72");

            var result = objHD4.deviceDetect(header);
            var data = objHD4.getReply();

            Assert.IsTrue(result);
            Assert.AreEqual(0, data["status"]);
            Assert.AreEqual("OK", data["message"]);
            Assert.AreEqual("", data["hd_specs"]["general_type"]);

            Assert.AreEqual("Apple", data["hd_specs"]["general_vendor"]);
            Assert.AreEqual("iPhone 3G", data["hd_specs"]["general_model"]);
            Assert.AreEqual("iOS", data["hd_specs"]["general_platform"]);
            Assert.AreEqual("4.2.1", data["hd_specs"]["general_platform_version"]);
            Assert.AreEqual("en-gb", data["hd_specs"]["general_language"]);

            Dictionary<string, dynamic> handsetSpecs = data["hd_specs"];
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_pixel_ratio"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_ppi"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_min"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_max"));

        }

        /// <summary>
        /// iPhone - Crazy benchmark (eg from emulated desktop) with outdated OS
        /// </summary>
        [Test]
        public void test43_ultimate_community_deviceDetectHTTPHardwareInfoC()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);

            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","Mozilla/5.0 (iPhone; U; CPU iPhone OS 2_0 like Mac OS X; en-gb) AppleWebKit/533.17.9 (KHTML, like Gecko)"}
            };
            header.Add("x-local-hardwareinfo", "320:480:200:1200");

            var result = objHD4.deviceDetect(header);
            var data = objHD4.getReply();

            Assert.IsTrue(result);
            Assert.AreEqual(0, data["status"]);
            Assert.AreEqual("OK", data["message"]);
            Assert.AreEqual("", data["hd_specs"]["general_type"]);

            Assert.AreEqual("Apple", data["hd_specs"]["general_vendor"]);
            Assert.AreEqual("iPhone 3G", data["hd_specs"]["general_model"]);
            Assert.AreEqual("iOS", data["hd_specs"]["general_platform"]);
            Assert.AreEqual("2.0", data["hd_specs"]["general_platform_version"]);
            Assert.AreEqual("en-gb", data["hd_specs"]["general_language"]);

            Dictionary<string, dynamic> handsetSpecs = data["hd_specs"];
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_pixel_ratio"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_ppi"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_min"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_max"));

        }

        /// <summary>
        /// iPhone 5s running Facebook 9.0 app (hence no general_browser set).
        /// </summary>
        [Test]
        public void test44_ultimate_community_deviceDetectHTTPFBiOS()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);

            var header = new Dictionary<string, dynamic>(){
            {"User-Agent","Mozilla/5.0 (iPhone; CPU iPhone OS 7_1_1 like Mac OS X) AppleWebKit/537.51.2 (KHTML, like Gecko) Mobile/11D201 [FBAN/FBIOS;FBAV/9.0.0.25.31;FBBV/2102024;FBDV/iPhone6,2;FBMD/iPhone;FBSN/iPhone OS;FBSV/7.1.1;FBSS/2; FBCR/vodafoneIE;FBID/phone;FBLC/en_US;FBOP/5]"}
            };
            header.Add("Accept-Language", "da, en-gb;q=0.8, en;q=0.7");

            var result = objHD4.deviceDetect(header);
            var data = objHD4.getReply();

            Assert.IsTrue(result);
            Assert.AreEqual(0, data["status"]);
            Assert.AreEqual("OK", data["message"]);
            Assert.AreEqual("", data["hd_specs"]["general_type"]);

            Assert.AreEqual("Apple", data["hd_specs"]["general_vendor"]);
            Assert.AreEqual("iPhone 5s", data["hd_specs"]["general_model"]);
            Assert.AreEqual("iOS", data["hd_specs"]["general_platform"]);
            Assert.AreEqual("7.1.1", data["hd_specs"]["general_platform_version"]);
            Assert.AreEqual("da", data["hd_specs"]["general_language"]);
            Assert.AreEqual("Danish", data["hd_specs"]["general_language_full"]);
            Assert.AreEqual("", data["hd_specs"]["general_type"]);


            Assert.AreEqual("9.0", data["hd_specs"]["general_app_version"]);
            Assert.AreEqual("", data["hd_specs"]["general_browser"]);
            Assert.AreEqual("", data["hd_specs"]["general_browser_version"]);

            Dictionary<string, dynamic> handsetSpecs = data["hd_specs"];
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_pixel_ratio"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("display_ppi"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_min"));
            Assert.AreEqual(true, handsetSpecs.ContainsKey("benchmark_max"));

        }

        /// <summary>
        /// Samsung GT-I9500 Native - Note : Device shipped with Android 4.2.2, so this device has been updated.
        /// </summary>
        [Test]
        public void test45_ultimate_community_deviceDetectBIAndroid()
        {
            Dictionary<string, dynamic> buildInfo = new Dictionary<string, dynamic>();
            buildInfo.Add("ro.build.PDA", "I9500XXUFNE7");
            buildInfo.Add("ro.build.changelist", "699287");
            buildInfo.Add("ro.build.characteristics", "phone");
            buildInfo.Add("ro.build.date.utc", "1401287026");
            buildInfo.Add("ro.build.date", "Wed May 28 23:23:46 KST 2014");
            buildInfo.Add("ro.build.description", "ja3gxx-user 4.4.2 KOT49H I9500XXUFNE7 release-keys");
            buildInfo.Add("ro.build.display.id", "KOT49H.I9500XXUFNE7");
            buildInfo.Add("ro.build.fingerprint", "samsung/ja3gxx/ja3g:4.4.2/KOT49H/I9500XXUFNE7:user/release-keys");
            buildInfo.Add("ro.build.hidden_ver", "I9500XXUFNE7");
            buildInfo.Add("ro.build.host", "SWDD5723");
            buildInfo.Add("ro.build.id", "KOT49H");
            buildInfo.Add("ro.build.product", "ja3g");
            buildInfo.Add("ro.build.tags", "release-keys");
            buildInfo.Add("ro.build.type", "user");
            buildInfo.Add("ro.build.user", "dpi");
            buildInfo.Add("ro.build.version.codename", "REL");
            buildInfo.Add("ro.build.version.incremental", "I9500XXUFNE7");
            buildInfo.Add("ro.build.version.release", "4.4.2");
            buildInfo.Add("ro.build.version.sdk", "19");
            buildInfo.Add("ro.product.board", "universal5410");
            buildInfo.Add("ro.product.brand", "samsung");
            buildInfo.Add("ro.product.cpu.abi2", "armeabi");
            buildInfo.Add("ro.product.cpu.abi", "armeabi-v7a");
            buildInfo.Add("ro.product.device", "ja3g");
            buildInfo.Add("ro.product.locale.language", "en");
            buildInfo.Add("ro.product.locale.region", "GB");
            buildInfo.Add("ro.product.manufacturer", "samsung");
            buildInfo.Add("ro.product.model", "GT-I9500");
            buildInfo.Add("ro.product.name", "ja3gxx");
            buildInfo.Add("ro.product_ship", "true");


            var result = objHD4.deviceDetect(buildInfo);
            var reply = objHD4.getReply();

            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);


            Assert.AreEqual("", reply["hd_specs"]["general_type"]);
            Assert.AreEqual("Samsung", reply["hd_specs"]["general_vendor"]);
            Assert.AreEqual("GT-I9500", reply["hd_specs"]["general_model"]);
            Assert.AreEqual("Android", reply["hd_specs"]["general_platform"]);
            //Assert.AreEqual("4.4.2", reply["hd_specs"]["general_platform_version"]);
            Assert.AreEqual("", reply["hd_specs"]["general_aliases"][0]);
        }

        /// <summary>
        ///  iPhone 4S Native
        /// </summary>
        [Test]
        public void test46_ultimate_community_deviceDetectBIiOS()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);


            Dictionary<string, dynamic> buildInfo = new Dictionary<string, dynamic>();
            buildInfo.Add("utsname.machine", "iphone4,1");
            buildInfo.Add("utsname.brand", "Apple");

            var result = objHD4.deviceDetect(buildInfo);
            var reply = objHD4.getReply();


            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);

            Assert.AreEqual("", reply["hd_specs"]["general_type"]);
            Assert.AreEqual("Apple", reply["hd_specs"]["general_vendor"]);
            Assert.AreEqual("iPhone 4S", reply["hd_specs"]["general_model"]);
            Assert.AreEqual("iOS", reply["hd_specs"]["general_platform"]);
            Assert.AreEqual("5.0", reply["hd_specs"]["general_platform_version"]);


        }

        /// <summary>
        /// Windows Phone Native Nokia Lumia 1020
        /// </summary>
        [Test]
        public void test47_ultimate_community_deviceDetectWindowsPhone()
        {
            HttpRequest request = new HttpRequest(null, "http://localhost", null);
            objHD4 = new HD4(request, ultimateConfig);

            Dictionary<string, dynamic> buildInfo = new Dictionary<string, dynamic>();
            buildInfo.Add("devicemanufacturer", "nokia");
            buildInfo.Add("devicename", "RM-875");

            var result = objHD4.deviceDetect(buildInfo);
            var reply = objHD4.getReply();


            Assert.IsTrue(result);
            Assert.AreEqual(0, reply["status"]);
            Assert.AreEqual("OK", reply["message"]);

            Assert.AreEqual("", reply["hd_specs"]["general_type"]);
            Assert.AreEqual("Nokia", reply["hd_specs"]["general_vendor"]);
            Assert.AreEqual("Lumia 1020", reply["hd_specs"]["general_model"]);
            Assert.AreEqual("Windows Phone", reply["hd_specs"]["general_platform"]);
            Assert.AreEqual(0, reply["hd_specs"]["display_ppi"]);
        }
    }
}


