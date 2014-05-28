using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using HD3;

public partial class Tests : System.Web.UI.Page
{

    protected void Page_Load(object sender, EventArgs e)
    {
        singleInstance();
    }

    private void singleInstance()
    {
        string file = Request.PhysicalApplicationPath + "\\headers.txt";
        char[] separator = new char[] { '|' };
        var hd3 = new HD3.HD3(Request);
        int totalCount = 0;
        header();
        var timer = System.Diagnostics.Stopwatch.StartNew();
        using (StreamReader sr = File.OpenText(file))
        {
            string s = String.Empty;
            while ((s = sr.ReadLine()) != null)
            {
                string[] strSplitArr = s.Split(separator);
                String userAgent = strSplitArr[0];
                String profile = strSplitArr[1];
                for (int j = 0; j < 10; j++)
                {
                    Response.Write("<tr>");
                    hd3.setDetectVar("user-agent", userAgent);
                    hd3.setDetectVar("x-wap-profile", profile);
                    if (hd3.siteDetect())
                    {
                        dynamic reply = hd3.getReply();
                        Response.Write("<td>" + totalCount + "</td>");
                        Response.Write("<td>" + reply["hd_specs"]["general_vendor"] + "</td>");
                        Response.Write("<td>" + reply["hd_specs"]["general_model"] + "</td>");
                        Response.Write("<td>" + reply["hd_specs"]["general_platform"] + "</td>");
                        Response.Write("<td>" + reply["hd_specs"]["general_platform_version"] + "</td>");
                        Response.Write("<td>" + reply["hd_specs"]["general_browser"] + "</td>");
                        Response.Write("<td>" + reply["hd_specs"]["general_browser_version"] + "</td>");
                        Response.Write("<td>" + strSplitArr[0] + "</td>");
                    }
                    else
                    {
                        dynamic reply = hd3.getReply();
                        Response.Write("<td>" + totalCount + "</td>");
                        Response.Write("<td colspan=\"7\">Got " + reply["status"] + " on " + s + "</td>");
                    }
                    totalCount++;
                    hd3.cleanUp();
                    Response.Write("</tr>");
                }
            }
        }
        timer.Stop();
        hd3.cleanUp();
        Response.Write("</table>");
        Response.Write("<h1>Test complete</h1>");
        float elapsedTimeSec = (float)timer.Elapsed.TotalMilliseconds / 1000F;
        int dps = (int)((int)totalCount / elapsedTimeSec);
        Response.Write("<h3>Elapsed Time " + elapsedTimeSec + "ms, Total detections: " + totalCount + ", Detections per second: " + dps + "</h3>");
    }


    private void multipleInstance()
    {
        string file = Request.PhysicalApplicationPath + "\\headers.txt";
        char[] separator = new char[] { '|' };
        int totalCount = 0;
        header();
        var timer = System.Diagnostics.Stopwatch.StartNew();
        using (StreamReader sr = File.OpenText(file))
        {
            string s = String.Empty;
            while ((s = sr.ReadLine()) != null)
            {
                string[] strSplitArr = s.Split(separator);
                String userAgent = strSplitArr[0];
                String profile = strSplitArr[1];
                for (int j = 0; j < 10; j++)
                {
                    Response.Write("<tr>");
                    var hd3 = new HD3.HD3(Request);
                    hd3.setDetectVar("user-agent", userAgent);
                    hd3.setDetectVar("x-wap-profile", profile);
                    if (hd3.siteDetect())
                    {
                        dynamic reply = hd3.getReply();
                        Response.Write("<td>" + totalCount + "</td>");
                        Response.Write("<td>" + reply["hd_specs"]["general_vendor"] + "</td>");
                        Response.Write("<td>" + reply["hd_specs"]["general_model"] + "</td>");
                        Response.Write("<td>" + reply["hd_specs"]["general_platform"] + "</td>");
                        Response.Write("<td>" + reply["hd_specs"]["general_platform_version"] + "</td>");
                        Response.Write("<td>" + reply["hd_specs"]["general_browser"] + "</td>");
                        Response.Write("<td>" + reply["hd_specs"]["general_browser_version"] + "</td>");
                        Response.Write("<td>" + strSplitArr[0] + "</td>");
                    }
                    else
                    {
                        dynamic reply = hd3.getReply();
                        Response.Write("<td>" + totalCount + "</td>");
                        Response.Write("<td colspan=\"7\">Got " + reply["status"] + " on " + s + "</td>");
                    }
                    totalCount++;
                    hd3.cleanUp();
                    Response.Write("</tr>");
                }
            }
        }
        timer.Stop();
        Response.Write("</table>");
        Response.Write("<h1>Test complete</h1>");
        float elapsedTimeSec = (float)timer.Elapsed.TotalMilliseconds / 1000F;
        int dps = (int)((int)totalCount / elapsedTimeSec);
        Response.Write("<h3>Elapsed Time " + elapsedTimeSec + "ms, Total detections: " + totalCount + ", Detections per second: " + dps + "</h3>");
    }

    private void header()
    {
        Response.Write("<table style='font-size:12px'><tr><th>Count</th><th>Vendor</th><th>Model</th><th>Platform</th><th>Platform Version</th><th>Browser</th><th>Browser Version</th><th>HTTP Headers</th></tr>");
    }

}
