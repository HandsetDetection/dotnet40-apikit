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
    public class TestHdStore
    {
        JavaScriptSerializer _jss = new JavaScriptSerializer();
        private HdStore _store;
        private HdCache _objCache;
        Dictionary<string, dynamic> _testData = new Dictionary<string, dynamic>();

        [SetUp]
        public void TestSetupData()
        {
            _store = HdStore.Instance;
            _objCache = new HdCache();
            if (_testData.Count == 0)
            {
                _testData.Add("roses", "red");
                _testData.Add("fish", "blue");
                _testData.Add("sugar", "sweet");
                _testData.Add("number", "4");
            }
        }

        /// <summary>
        /// Writes to store & cache
        /// </summary>
        [Test]
        public void test50_ReadWrite()
        {
            string key = "storeKey" + DateTime.Now.Ticks;
            _store.Write(key, _testData);

            var data = _store.Read(key);

            Assert.AreEqual(_testData, data);

            var cacheData = _objCache.Read(key);

            Assert.AreEqual(_testData, cacheData);

            bool isExists = System.IO.File.Exists(_store.StoreDirectory + "/" + key + ".json");
            Assert.IsTrue(isExists);

        }

        /// <summary>
        ///  Writes to store & not cache
        /// </summary>
        [Test]
        public void test51_StoreFetch()
        {
            string key = "storeKey2" + DateTime.Now.Ticks;
            _store.store(key, _testData);

            var cahceData = _objCache.Read(key);
            Assert.AreNotEqual(_testData, cahceData);

            var storeData = _store.Fetch(key);
            Assert.AreEqual(_testData, storeData);

            bool isExists = System.IO.File.Exists(_store.StoreDirectory + "/" + key + ".json");
            Assert.IsTrue(isExists);
        }

        /// <summary>
        ///  Test purge
        /// </summary>
        [Test]
        public void test52_Purge()
        {
            var lstFiles = Directory.GetFiles(_store.StoreDirectory, "*.json");
            Assert.IsNotEmpty(lstFiles);
            _store.Purge();
            var lstFiles1 = Directory.GetFiles(_store.StoreDirectory, "*.json");
            Assert.IsEmpty(lstFiles1);
        }

        /// <summary>
        /// Reads all devices from Disk (Keys need to be in Device*json format)
        /// </summary>
        [Test]
        public void test53_FetchDevices()
        {
            string key = "Device" + DateTime.Now.Ticks;
            _store.store(key, _testData);
            var devices = _store.FetchDevices();
            Assert.AreEqual(devices["devices"][0], _testData);
            _store.Purge();

        }

        /// <summary>
        /// Moves a file from disk into store (vanishes from previous location).
        /// </summary>
        [Test]
        public void test54_MoveIn()
        {
            var jsonString = _jss.Serialize(_testData);
            string filesuffix = DateTime.Now.Ticks.ToString();
            string filePathFirst = _store.StoreDirectory + "/TemDevice" + filesuffix + ".json";
            string filePathSecond = _store.StoreDirectory + "/Temp/TemDevice" + filesuffix + ".json";
            File.WriteAllText(filePathFirst, jsonString);
            if (!Directory.Exists(_store.StoreDirectory + "/Temp"))
            {
                Directory.CreateDirectory(_store.StoreDirectory + "/Temp");
            }
            bool isFileExist = File.Exists(filePathFirst);
            bool isSecondFileExist = File.Exists(filePathSecond);
            Assert.IsTrue(isFileExist);
            Assert.IsFalse(isSecondFileExist);
            _store.MoveIn(filePathFirst, filePathSecond);
            isFileExist = File.Exists(filePathFirst);
            isSecondFileExist = File.Exists(filePathSecond);
            Assert.IsFalse(isFileExist);
            Assert.IsTrue(isSecondFileExist);

        }

        /// <summary>
        /// Test singleton'ship
        /// </summary>
        [Test]
        public void test55_Singleton()
        {
            var store1 = HdStore.Instance;
            var store2 = HdStore.Instance;
            store1.SetPath("tmp", true);
            Assert.AreEqual(store2.StoreDirectory, store1.StoreDirectory);
        }

    }
}
