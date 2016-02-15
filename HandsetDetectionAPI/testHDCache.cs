using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandsetDetectionAPI
{
    public class TestHdCache
    {
        private HdCache _objCache;

        Dictionary<string, dynamic> _testData = new Dictionary<string, dynamic>();

        [SetUp]
        public void TestSetupData()
        {
            _objCache = new HdCache();
            if (_testData.Count == 0)
            {
                _testData.Add("roses", "red");
                _testData.Add("fish", "blue");
                _testData.Add("sugar", "sweet");
                _testData.Add("number", "4");
            }
        }

        [Test]
        public void test48_A()
        {
            string key = DateTime.Now.Ticks.ToString();
            _objCache.Write(key, _testData);

            var reply = _objCache.Read<Dictionary<string,dynamic>>(key);

            Assert.AreEqual(_testData, reply);
        }

        [Test]
        public void test49_Volume()
        {
            string key = "";

            List<string> lstkeys = new List<string>();
            for (int i = 0; i < 10000; i++)
            {
                key = string.Format("test{0}_{1}", DateTime.Now.Ticks, i);
                _objCache.Write(key, _testData);
                lstkeys.Add(key);
            }

            foreach (var objKey in lstkeys)
            {
                var reply = _objCache.Read<Dictionary<string,dynamic>>(objKey);
                Assert.AreEqual(reply, _testData);
            }
        }
    }
}
