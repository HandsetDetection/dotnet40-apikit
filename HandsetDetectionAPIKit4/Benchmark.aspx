<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Benchmark.aspx.cs" Inherits="Web.Benchmark" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Test headers</title>
    <style type="text/css">
        body { background-color: lemonchiffon; }

        table { background-color: #fff; }

        tr:nth-child(even) { background: #f1f1f1; }

        tr:nth-child(odd) { background: #FFF; }
    </style>
</head>
<body>

<form id="form1" runat="server">
    <div>
        <asp:GridView ID="grdDeviceModel" runat="server"></asp:GridView>
        <h3>
            Elapsed Time : <asp:Label ID="lblTimeElapsed" runat="server" Text=""></asp:Label>
            Total Detections : <asp:Label ID="lblTotDetect" runat="server" Text=""></asp:Label>
            Detection per second : <asp:Label ID="lblDetectPerSec" runat="server" Text=""></asp:Label>
        </h3>
    </div>
</form>
</body>
</html>