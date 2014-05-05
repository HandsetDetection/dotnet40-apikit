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

public partial class Tests : System.Web.UI.Page {
    
    protected void Page_Load(object sender, EventArgs e) {
        string file = Request.PhysicalApplicationPath + "\\normal.txt";
        string[] lines = System.IO.File.ReadAllLines(file);
        char[] separator = new char[] { '|' };
        Console.WriteLine(Request);
        var hd3 = new HD3.HD3(Request);
        int i=0;
        // Display the file contents by using a foreach loop.
        Response.Write("Expect all normal agents to return 301 - Test begins.<br/>");
        foreach (string line in lines) {
            if (i++ > 30) break ;
            string[] strSplitArr = line.Split(separator);
            if (strSplitArr.Length > 1) {
                hd3.setDetectVar("user-agent", strSplitArr[0]);
                hd3.setDetectVar("x-wap-profile", strSplitArr[1]);
            } else {
                hd3.setDetectVar("user-agent", strSplitArr[0]);
            }

            if (hd3.siteDetect()) {
                string rawreply = hd3.getRawReply();
                Response.Write("JSON object dump " + rawreply + "<br/>");
                dynamic reply = hd3.getReply();
                Response.Write("Vendor " + reply["hd_specs"]["general_vendor"] + "<br/>");
                Response.Write("Model " + reply["hd_specs"]["general_model"] + "<br/>");
                Response.Write("Browser " + reply["hd_specs"]["general_browser"] + "<br/>");
                Response.Write("Platform " + reply["hd_specs"]["general_platform"] + "<br/>");
                Response.Write(hd3.getLog());
            } else {
                dynamic reply = hd3.getReply();
                Response.Write("Count "+i+" got "+reply["status"]+" on "+line+"<br/>");
                //Response.Write(hd3.getLog());
            }
            hd3.cleanUp();       
        }
        Response.Write("Test complete.<br/>");


        string file2 = Request.PhysicalApplicationPath + "\\mobile.txt";
        string[] lines2 = System.IO.File.ReadAllLines(file2);
        i = 0;
        // Display the file contents by using a foreach loop.
        Response.Write("Expect all mobile agents to return JSON and display some device info.<br/>");
        foreach (string line in lines2) {
            if (i++ > 30) break;
            string[] strSplitArr = line.Split(separator);
            if (strSplitArr.Length > 1) {
                hd3.setDetectVar("user-agent", strSplitArr[0]);
                hd3.setDetectVar("x-wap-profile", strSplitArr[1]);
            } else {
                hd3.setDetectVar("user-agent", strSplitArr[0]);
            }

            if (hd3.siteDetect()) {
                string rawreply = hd3.getRawReply();
                dynamic reply = hd3.getReply();
                Response.Write("<b>Vendor " + reply["hd_specs"]["general_vendor"]);
                Response.Write(",Model " + reply["hd_specs"]["general_model"]);
                Response.Write(",Browser " + reply["hd_specs"]["general_browser"]);
                Response.Write(",Platform " + reply["hd_specs"]["general_platform"] + "</b><br/>");
                Response.Write("JSON object dump " + rawreply + "<br/>");    
                //Response.Write(hd3.getLog());
            } else {
                dynamic reply = hd3.getReply();
                Response.Write("<h2>FAIL</h2>");
                Response.Write(hd3.getLog());
                //Response.Write("Count " + i + " got " + reply["status"] + " on " + line + "<br/>");
            }
            hd3.cleanUp();
        }
        Response.Write("Test complete.<br/>");
    }
}
