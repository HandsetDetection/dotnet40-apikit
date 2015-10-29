using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using System.IO;
using System.Web.Script.Serialization;

namespace HandsetDetectionAPI
{
    public class testHDStore
    {
        JavaScriptSerializer jss = new JavaScriptSerializer();
        private HDStore Store;
        private HDCache objCache;
        Dictionary<string, dynamic> testData = new Dictionary<string, dynamic>();

        [SetUp]
        public void testSetupData()
        {
            Store = HDStore.Instance;
            objCache = new HDCache();
            if (testData.Count == 0)
            {
                testData.Add("roses", "red");
                testData.Add("fish", "blue");
                testData.Add("sugar", "sweet");
                testData.Add("number", "4");
            }
        }

        /// <summary>
        /// Writes to store & cache
        /// </summary>
        [Test]
        public void testReadWrite()
        {
            string key = "storeKey" + DateTime.Now.Ticks;
            Store.write(key, testData);

            var data = Store.read(key);

            Assert.AreEqual(testData, data);

            var cacheData = objCache.read(key);

            Assert.AreEqual(testData, cacheData);

            bool IsExists = System.IO.File.Exists(Store.StoreDirectory + "/" + key + ".json");
            Assert.IsTrue(IsExists);

        }

        /// <summary>
        ///  Writes to store & not cache
        /// </summary>
        [Test]
        public void testStoreFetch()
        {
            string key = "storeKey2" + DateTime.Now.Ticks;
            Store.store(key, testData);

            var cahceData = objCache.read(key);
            Assert.AreNotEqual(testData, cahceData);

            var storeData = Store.fetch(key);
            Assert.AreEqual(testData, storeData);

            bool IsExists = System.IO.File.Exists(Store.StoreDirectory + "/" + key + ".json");
            Assert.IsTrue(IsExists);
        }

        /// <summary>
        ///  Test purge
        /// </summary>
        [Test]
        public void testPurge()
        {
            var lstFiles = Directory.GetFiles(Store.StoreDirectory, "*.json");
            Assert.IsNotEmpty(lstFiles);
            Store.purge();
            var lstFiles1 = Directory.GetFiles(Store.StoreDirectory, "*.json");
            Assert.IsEmpty(lstFiles1);
        }

        /// <summary>
        /// Reads all devices from Disk (Keys need to be in Device*json format)
        /// </summary>
        [Test]
        public void testFetchDevices()
        {
            string key = "Device" + DateTime.Now.Ticks;
            Store.store(key, testData);
            var devices = Store.fetchDevices();
            Assert.AreEqual(devices["devices"][0], testData);
            Store.purge();

        }

        /// <summary>
        /// Moves a file from disk into store (vanishes from previous location).
        /// </summary>
        [Test]
        public void testMoveIn()
        {
            var jsonString = jss.Serialize(testData);
            string filesuffix = DateTime.Now.Ticks.ToString();
            string filePathFirst = Store.StoreDirectory + "/TemDevice" + filesuffix + ".json";
            string filePathSecond = Store.StoreDirectory + "/Temp/TemDevice" + filesuffix + ".json";
            File.WriteAllText(filePathFirst, jsonString);
            if (!Directory.Exists(Store.StoreDirectory + "/Temp"))
            {
                Directory.CreateDirectory(Store.StoreDirectory + "/Temp");
            }
            bool IsFileExist = File.Exists(filePathFirst);
            bool IsSecondFileExist = File.Exists(filePathSecond);
            Assert.IsTrue(IsFileExist);
            Assert.IsFalse(IsSecondFileExist);
            Store.moveIn(filePathFirst, filePathSecond);
            IsFileExist = File.Exists(filePathFirst);
            IsSecondFileExist = File.Exists(filePathSecond);
            Assert.IsFalse(IsFileExist);
            Assert.IsTrue(IsSecondFileExist);

        }

        /// <summary>
        /// Test singleton'ship
        /// </summary>
        [Test]
        public void testSingleton()
        {
            var store1 = HDStore.Instance;
            var store2 = HDStore.Instance;
            store1.setPath("tmp", true);
            Assert.AreEqual(store2.StoreDirectory, store1.StoreDirectory);
        }

    }
}
