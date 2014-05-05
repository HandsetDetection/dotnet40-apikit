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
/// <summary>
/// 
/// </summary>
public partial class Devices : System.Web.UI.Page {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void Page_Load(object sender, EventArgs e) {
        var hd3 = new HD3.HD3(Request);

        Response.Write("<b>Vendor List</b><br/>");
        if (hd3.deviceVendors()) {
            string rawreply = hd3.getRawReply();
            Response.Write("JSON object dump "+rawreply + "<br/>");
        } else {
            Response.Write(hd3.getError() + "<br/>");
            Response.Write(hd3.getLog());
        }
        hd3.cleanUp();

        Response.Write("<b>All Nokia Models</b><br/>");
        if (hd3.deviceModels("Nokia")) {
            string rawreply = hd3.getRawReply();
            Response.Write("JSON object dump " + rawreply + "<br/>");
        } else {
            Response.Write(hd3.getError() + "<br/>");
            Response.Write(hd3.getLog());
        }
        hd3.cleanUp();

        Response.Write("<b>Nokia N95 device properties</b><br/>");
        if (hd3.deviceView("Nokia","N95")) {
            string rawreply = hd3.getRawReply();
            Response.Write("JSON object dump " + rawreply + "<br/>");
            dynamic reply = hd3.getReply();
            Response.Write(hd3.getLog());
           /*
            Response.Write("Vendor " + reply["hd_specs"]["general_vendor"] + "<br/>");
            Response.Write("Model " + reply["hd_specs"]["general_model"] + "<br/>");
            Response.Write("Browser " + reply["hd_specs"]["general_browser"] + "<br/>");
            Response.Write("Platform " + reply["hd_specs"]["general_platform"] + "<br/>");
            * */
        } else {
            Response.Write(hd3.getError() + "<br/>");
            Response.Write(hd3.getLog());
        }
        hd3.cleanUp();

        //===========================================================
        Response.Write("<b>Handsets with Network CDMA</b><br/>");
        hd3.ReadTimeout = 600;    // Increse the read timeout on long running requests
        if (hd3.deviceWhatHas("network","cdma")) {
            string rawreply = hd3.getRawReply();
            Response.Write("JSON object dump " + rawreply + "<br/>");
            dynamic reply = hd3.getReply();
        } else {
            Response.Write(hd3.getError() + "<br/>");
            Response.Write(hd3.getLog());
        }
        hd3.cleanUp();
    }
}