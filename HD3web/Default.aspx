<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>HD3 samples</title>
    <style type="text/css">
        body
        {
            font-family: Trebuchet MS;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <h2>HD3 Test Suite</h2>
        <a href="Sites.aspx">Sites</a><br />
	<a href="Devices.aspx">Devices</a><br />
        <a href="Tests.aspx">Tests</a><br />
	<h3>By Default these should not be viewable (requires licence)</h3>
	<a href="hd3specs.json">hd3specs</a><br />
        <a href="hd3trees.json">hd3trees</a><br />
    </div>
    </form>
</body>
</html>
