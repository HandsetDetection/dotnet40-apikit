using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace HandsetDetectionAPI
{
    public class testHDDevice
    {
        private HDStore Store;
        private HDDevice Device;

        private static bool IsCommunitySetupDone = true;
        Dictionary<string, dynamic> headers = new Dictionary<string, dynamic>() {
        {"User-Agent","Mozilla/5.0 (iPhone; U; CPU iPhone OS 4_3 like Mac OS X; en-gb) AppleWebKit/533.17.9 (KHTML, like Gecko)"}};

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


        // Remove community edition
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
