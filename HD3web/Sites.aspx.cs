using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using HD3;

//
// This example illuatrates how to use HD3 sites* calls
// The main use-case is to perform device detection however
// you can also use it to download deteciton trees (if you've purchased a license)
// and perform detections locally. 
//
// set use-local="true" in the web config to perform local detections.
//
public partial class Sites : System.Web.UI.Page {
    protected void Page_Load(object sender, EventArgs e) {
        var hd3 = new HD3.HD3(Request);

        Response.Write("<b>Simple Detection - Using web browser standard headers (expect NotFound)</b><br/>");
        if (hd3.siteDetect()) {
            string rawreply = hd3.getRawReply();
            Response.Write(rawreply + "<br/>");
        } else {
            Response.Write(hd3.getError() + "<br/>");
            Response.Write(hd3.getLog());
        }
        hd3.cleanUp();

        Response.Write("<b>Simple Detection - Setting Headers for an N95</b><br/>");
        hd3.setDetectVar("user-agent", "Mozilla/5.0 (SymbianOS/9.2; U; Series60/3.1 NokiaN95-3/20.2.011 Profile/MIDP-2.0 Configuration/CLDC-1.1 ) AppleWebKit/413");
        hd3.setDetectVar("x-wap-profile", "http://nds1.nds.nokia.com/uaprof/NN95-1r100.xml");
        if (hd3.siteDetect()) {
            string rawreply = hd3.getRawReply();
            Response.Write("JSON object dump "+rawreply + "<br/>");
            dynamic reply = hd3.getReply();
            Response.Write("Vendor " + reply["hd_specs"]["general_vendor"] + "<br/>");
            Response.Write("Model " + reply["hd_specs"]["general_model"] + "<br/>");
            Response.Write("Browser " + reply["hd_specs"]["general_browser"] + "<br/>");
            Response.Write("Platform " + reply["hd_specs"]["general_platform"] + "<br/>");
            Response.Write(hd3.getLog());
        } else {
            Response.Write(hd3.getError() + "<br/>");
            Response.Write(hd3.getLog());
        }
        hd3.cleanUp();

        Response.Write("<b>Simple Detection - Passing a different ip address (only works against web service)</b><br/>");
        // Query for some other information (remember the N95 headers are still set).
        // Add detection options to query for additional reply information such as geoip information
        // Note : We use the ipaddress to get the geoip location.
        hd3.setDetectVar("ipaddress", "64.34.165.180");
        hd3.setDetectVar("user-agent", "Mozilla/5.0 (iPhone; U; CPU iPhone OS 4_3_3 like Mac OS X; en-us) AppleWebKit/533.17.9 (KHTML, like Gecko) Version/5.0.2 Mobile/8J2 Safari/6533.18.5");
        hd3.setDetectVar("x-wap-profile", "http://nds1.nds.nokia.com/uaprof/NN95-1r100.xml");
        if (hd3.siteDetect("hd_specs, geoip")) {
            string rawreply = hd3.getRawReply();
            Response.Write("JSON object dump " + rawreply + "<br/>");
            dynamic reply = hd3.getReply();
            Response.Write("Vendor " + reply["hd_specs"]["general_vendor"] + "<br/>");
            Response.Write("Model " + reply["hd_specs"]["general_model"] + "<br/>");
            Response.Write("Browser " + reply["hd_specs"]["general_browser"] + "<br/>");
            Response.Write("Platform " + reply["hd_specs"]["general_platform"] + "<br/>");
            Response.Write(hd3.getLog());
        } else {
            Response.Write(hd3.getError() + "<br/>");
            Response.Write(hd3.getLog());
        }
        hd3.cleanUp();

        //===========================================================
        Response.Write("<b>All Detection Information</b><br/>");
        hd3.ReadTimeout = 600;    // Increse the read timeout on long running requests
        if (hd3.siteFetchTrees()) {
            string rawreply = hd3.getRawReply();
            Response.Write("Size returned : "+ rawreply.Length + "<br/>");
        } else {
            Response.Write(hd3.getError() + "<br/>");
            Response.Write(hd3.getLog());
        }
        hd3.cleanUp();

        //=========================================
        Response.Write("<b>All Handset Information</b><br/>");
        hd3.ReadTimeout = 600;    // Increse the read timeout on long running requests
        if (hd3.siteFetchSpecs()) {
            string rawreply = hd3.getRawReply();
            Response.Write("Size returned : " + rawreply.Length + "<br/>");
        } else {
            Response.Write(hd3.getError() + "<br/>");
            Response.Write(hd3.getLog());
        }
        hd3.cleanUp();
    }
}