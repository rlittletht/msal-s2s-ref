<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="auth.aspx.cs" Inherits="WebApi.auth" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <p>You have reached this page because you have successfully granted consent to WebApi 
        Reference implementation. Go back to the original WebApp and retry the WebApi call 
        and it should succeed
    </p>
    <p>If you want to get back to the original state when the WebApi is not granted consent 
        (for a consumer Microsoft account), visit 
        <a href="https://account.live.com/consent/Manage">https://account.live.com/consent/Manage</a>
    </p>
</body>
</html>
