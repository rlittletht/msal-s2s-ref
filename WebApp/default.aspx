<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="default.aspx.cs" Inherits="WebApp._default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <asp:Button ID="btnCallService" runat="server" Text="Call Service" OnClick="DoCallService" />
            <div id="divOutput" runat="server">

            </div>
        </div>
    </form>
</body>
</html>
