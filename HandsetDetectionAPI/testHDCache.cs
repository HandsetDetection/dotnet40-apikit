using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandsetDetectionAPI
{
    public class testHDCache
    {
        private HDCache objCache;

        Dictionary<string, dynamic> testData = new Dictionary<string, dynamic>();

        [SetUp]
        public void testSetupData()
        {
            objCache = new HDCache();
            if (testData.Count == 0)
            {
                testData.Add("roses", "red");
                testData.Add("fish", "blue");
                testData.Add("sugar", "sweet");
                testData.Add("number", "4");
            }
        }

        [Test]
        public void testA()
        {
            string key = DateTime.Now.Ticks.ToString();
            objCache.write(key, testData);

            var reply = objCache.read(key);

            Assert.AreEqual(testData, reply);
        }

        [Test]
        public void testVolume()
        {
            string key = "";

            List<string> lstkeys = new List<string>();
            for (int i = 0; i < 10000; i++)
            {
                key = string.Format("test{0}_{1}", DateTime.Now.Ticks, i);
                objCache.write(key, testData);
                lstkeys.Add(key);
            }

            foreach (var objKey in lstkeys)
            {
                var reply = objCache.read(objKey);
                Assert.AreEqual(reply, testData);
            }
        }
    }
}
