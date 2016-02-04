using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace HandsetDetectionAPI
{
    // The device class performs the same functions as our Cloud API, but locally.
    // It is only used when use_local is set to true in the config file.
    // To perform tests we need to setup the environment by populating the the Storage layer with device specs.
    // So install the latest community edition so there is something to work with.

    public class TestHdDevice
    {
        private HdStore _store;
        private HdDevice _device;

        private static bool _isCommunitySetupDone = true;
        Dictionary<string, dynamic> _headers = new Dictionary<string, dynamic>() {
        {"User-Agent","Mozilla/5.0 (iPhone; U; CPU iPhone OS 4_3 like Mac OS X; en-gb) AppleWebKit/533.17.9 (KHTML, like Gecko)"}};

        /// <summary>
        /// Setup community edition for tests. Takes 60s or so to download and install.
        /// </summary>
        [SetUp]
        public void SetUpBeforeClass()
        {
            if (_isCommunitySetupDone)
            {
                HttpRequest request = new HttpRequest(null, "http://localhost", null);
                Hd4 objHd4 = new Hd4(request, "/hdCloudConfig.json");
                string directoryPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string filePath = directoryPath + "\\" + "communityTest.zip";
                _store = HdStore.Instance;
                _store.SetPath(directoryPath, true);
                  objHd4.CommunityFetchArchive();
                _isCommunitySetupDone = false;
            }
        }


        /// <summary>
        ///  Remove community edition
        /// </summary>
       [Test]
        public void test57_tearDownAfterClass()
        {
            _store = HdStore.Instance;
            _store.Purge();
        }

        [Test]
        public void test55_IsHelperUsefulTrue()
        {
            _device = new HdDevice();
            _store.SetPath("C://APIData");
          
            var result = _device.IsHelperUseful(_headers);
            Assert.IsTrue(result);
        }

        [Test]
        public void test56_IsHelperUsefulFalse()
        {
            _device = new HdDevice();
            _store.SetPath("C://APIData");
            _headers["User-Agent"] = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36";
            var result = _device.IsHelperUseful(_headers);
            Assert.IsFalse(result);
        }

    }
}
