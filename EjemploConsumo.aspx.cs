using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SCyC_Web.ING_SOFTWARE
{
    public partial class EjemploConsumo : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        //como consumir servicio de impriir PDF
        public void consumir(object sender, EventArgs e)
        {
            ServiceReference1.WS_Pago_ColegiaturaSoapClient controlador = new ServiceReference1.WS_Pago_ColegiaturaSoapClient();
            ServiceReference2.FACTURA_ELECTRONICASoapClient WS = new ServiceReference2.FACTURA_ELECTRONICASoapClient();
            WS.generarFacturaPDF("prueba", "prueba", "prueba", "prueba", "prueba", "prueba", "prueba");

            /*
            XElement doc = WS.consultarUsuario("1231231");

            inputName.Value = doc.Element("DPI").Value;
            inputApe.Value = doc.Element("NOMBRE").Value;
            inputCui.Value = doc.Element("ESTADO").Value;
            */
        }
    }
}