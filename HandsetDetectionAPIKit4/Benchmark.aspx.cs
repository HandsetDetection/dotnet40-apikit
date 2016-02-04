using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Web.UI;
using HandsetDetectionAPI;

namespace Web
{
    public partial class Benchmark : Page
    {
        private List<Dictionary<string, dynamic>> _headers = new List<Dictionary<string, dynamic>>();
        public string ConfigFile = "//hdUltimateConfig.json";
        public string DataFile = "benchmarkData.txt";
        public long TotalMilliSec { get; set; }
        public DateTime EndTime { get; set; }
        public int TotalCount { get; set; }

        protected void Page_Load(dynamic sender, EventArgs e)
        {
            var fileName = "/benchmarkData.txt";
            try
            {
                if (File.Exists(Server.MapPath(fileName)))
                {
                    using (var sr = new StreamReader(Server.MapPath(fileName)))
                    {
                        string line;

                        while ((line = sr.ReadLine()) != null)
                        {
                            var item = line.Trim().Split(new[] { "|" }, StringSplitOptions.None);
                            var requestBody = new Dictionary<string, dynamic>();
                            requestBody["user-agent"] = item[0];
                            if (item.Length > 1)
                            {
                                requestBody["x-wap-profile"] = item[1];
                            }
                            else
                            {
                                requestBody["x-wap-profile"] = string.Empty;
                            }
                            _headers.Add(requestBody);

                        }
                    }
                }
                else
                {
                    Response.Write("File not exist.");
                }
            }
            catch (Exception ex)
            {
                Response.Write("File error: " + ex.Message);
            }
            var objHd = new Hd4(Request);

            FlyThrough(objHd);
            lblTotDetect.Text = TotalCount.ToString();
            lblTimeElapsed.Text = (TotalMilliSec / 1000).ToString(CultureInfo.InvariantCulture) + " sec.";
            lblDetectPerSec.Text = ((Convert.ToDouble(TotalCount) * 1000) / TotalMilliSec).ToString(CultureInfo.InvariantCulture);
        }

        public void FlyThrough(Hd4 objHd)
        {
            var deviceModelList = new List<DeviceModel>();
            TotalCount = 1;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Reset();
            stopwatch.Start();
            foreach (var header in _headers)
            {
                var result = objHd.DeviceDetect(header);
                TotalCount++;

            }

            stopwatch.Stop();
            TotalMilliSec = stopwatch.ElapsedMilliseconds;

            grdDeviceModel.DataSource = deviceModelList;
            grdDeviceModel.DataBind();

        }
    }


    public class DeviceModel
    {
        public string Count { get; set; }
        public string Vendor { get; set; }
        public string Model { get; set; }
        public string Platform { get; set; }
        public string PlatformVersion { get; set; }
        public string Browser { get; set; }
        public string BrowserVersion { get; set; }
        public string HttpHeaders { get; set; }
    }
}