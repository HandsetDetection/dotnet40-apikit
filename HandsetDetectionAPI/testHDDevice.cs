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

    public class testHDDevice
    {
        private HDStore Store;
        private HDDevice Device;

        private static bool IsCommunitySetupDone = true;
        Dictionary<string, dynamic> headers = new Dictionary<string, dynamic>() {
        {"User-Agent","Mozilla/5.0 (iPhone; U; CPU iPhone OS 4_3 like Mac OS X; en-gb) AppleWebKit/533.17.9 (KHTML, like Gecko)"}};

        /// <summary>
        /// Setup community edition for tests. Takes 60s or so to download and install.
        /// </summary>
        [SetUp]
        public void setUpBeforeClass()
        {
            if (IsCommunitySetupDone)
            {
                HttpRequest request = new HttpRequest(null, "http://localhost", null);
                HD4 objHD4 = new HD4(request, "/hdCloudConfig.json");
                string directoryPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string filePath = directoryPath + "\\" + "communityTest.zip";
                Store = HDStore.Instance;
                Store.setPath(directoryPath, true);
                  objHD4.communityFetchArchive();
                IsCommunitySetupDone = false;
            }
        }


        /// <summary>
        ///  Remove community edition
        /// </summary>
        public void tearDownAfterClass()
        {
            Store = HDStore.Instance;
            Store.purge();
        }

        [Test]
        public void testIsHelperUsefulTrue()
        {
            Device = new HDDevice();
            var result = Device.isHelperUseful(headers);
            Assert.IsTrue(result);
        }

        [Test]
        public void testIsHelperUsefulFalse()
        {
            Device = new HDDevice();
            headers["User-Agent"] = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36";
            var result = Device.isHelperUseful(headers);
            Assert.IsFalse(result);
        }

    }
}
