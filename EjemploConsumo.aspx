<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="EjemploConsumo.aspx.cs" Inherits="SCyC_Web.ING_SOFTWARE.EjemploConsumo" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <script src="https://code.jquery.com/jquery-3.6.0.min.js" integrity="sha256-/xUj+3OJU5yExlq6GSYGSHk7tPXikynS7ogEvDej/m4=" crossorigin="anonymous"></script>
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <title>Prueba consumo Servicio PDF</title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <button type="button" class="reporte-pdf">Reporte PDF</button>
        </div>
    </form>
</body>

<script type ="text/javascript">
    var soapMessage = '<?xml version="1.0" encoding="utf-8"?><soap12:Envelope xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:soap12="http://www.w3.org/2003/05/soap-envelope"> <soap12:Body> <EmitirDTE xmlns="http://tempuri.org/"> <nombre_emisor>string</nombre_emisor> <nit_emisor>string</nit_emisor> <dir_emisor>string</dir_emisor> <cod_auth>string</cod_auth> <num_serie>string</num_serie> <moneda>string</moneda> <productos>string</productos> <descuentos>string</descuentos> <email>string</email> </EmitirDTE> </soap12:Body></soap12:Envelope>';

    $(document).on("click", ".reporte-pdf", function () {
        $.ajax({
            url: "FACTURA_ELECTRONICA.asmx/generarFacturaPDF",
            data: '',
            contentType: "application/xml; charset=utf-8",
            dataType: "xml",
            type: "POST",            
            success: function (response) {
                if (response.d.length > 0) {
                    window.open(response.d[0].pdfReportPath);
                }
            },
            error: function (response) {
                alert(response.responseText);
            }
        });
    });
</script>
</html>
