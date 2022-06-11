using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Services;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

using static System.Net.WebRequestMethods;

namespace SCyC_Web
{
    /// <summary>
    /// Descripción breve de FACTURA_ELECTRONICA
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // Para permitir que se llame a este servicio web desde un script, usando ASP.NET AJAX, quite la marca de comentario de la línea siguiente. 
    [System.Web.Script.Services.ScriptService]
    public class FACTURA_ELECTRONICA : System.Web.Services.WebService
    {

        [WebMethod]
        public string HelloWorld()
        {
            return "Hola a todos";
        }

        [WebMethod]
        public XmlDocument EmitirDTE(string nombre_emisor, string nit_emisor, string dir_emisor, string cod_auth, string num_serie, string moneda, string productos, string descuentos, string email)
        {
            //string DTE = "";
            XmlDocument DTE = CreateXMLFile(nombre_emisor, nit_emisor, dir_emisor, cod_auth, num_serie, moneda, productos, descuentos);

            //Se guarda en la base de datos
            guardarDatosBd(num_serie, nombre_emisor, cod_auth, Serializar(DTE));

            //se envía correo
            string _total = "";
            string totalLetras = "";
            string _totalsindesc = "";
            XmlNode MyNode = DTE.SelectSingleNode("Factura_DTE/Productos/Total_con_descuento");
            _total = MyNode.InnerText;

            XmlNode MyNode3 = DTE.SelectSingleNode("Factura_DTE/Productos/Total_sin_descuento");
            _totalsindesc = MyNode3.InnerText;

            XmlNode MyNode2 = DTE.SelectSingleNode("Factura_DTE/Productos/Total_en_Letras");
            totalLetras = MyNode2.InnerText;

            //genero el pdf de factura y la envío            
            List<ModeloReporte> _li = new List<ModeloReporte>();
            ModeloReporte rd = new ModeloReporte();

            _li = generarFacturaPDF(nombre_emisor, nit_emisor, dir_emisor, cod_auth, num_serie, descuentos, email, _total, _totalsindesc, totalLetras);
            enviarEmail(email, nombre_emisor, nit_emisor, num_serie, totalLetras, obtenerDireccionLista(_li));

            return DTE;        
        }

        //registrar facturas
        [WebMethod]
        public string guardarDatosBd(string numSerie, string nombreEmisor, string codAuth, string xmlDocument64)
        {
            MySqlConnection conexionBD = new MySqlConnection("server = servidor ; database = bd_ingsoft ; Uid = usuario_bd ; pwd = constraseña_de_bd ");
            string estado = "";

            //inserto datos a la base de datos
            conexionBD.Open();
            MySqlCommand comando = new MySqlCommand("insert into  `bd_ingsoft`.`factura_data` (numSerie, nombreEmisor, codAutorizacion, xmlFactura) values (@numSerie, @nombreEmisor, @codAutorizacion, @xmlFactura)", conexionBD);
            comando.Parameters.AddWithValue("@numSerie", numSerie);
            comando.Parameters.AddWithValue("@nombreEmisor", nombreEmisor);
            comando.Parameters.AddWithValue("@codAutorizacion", codAuth);
            comando.Parameters.AddWithValue("@xmlFactura", xmlDocument64);
            
            comando.ExecuteNonQuery();

            conexionBD.Close();
            return estado;
        }
        //creacion de PDF's
        [WebMethod]
        public List<ModeloReporte> generarFacturaPDF(string nombre_emisor, string nit_emisor, string dir_emisor, string cod_auth, string num_serie, string descuentos, string email, string _total, string _totalsindesc, string totalLetras)
        {
            List<ModeloReporte> _li = new List<ModeloReporte>();
            ModeloReporte rd = new ModeloReporte();

            try
            {
                string html = "";
                //ES EL HTML DE LA FACTURA
                html = @"<!DOCTYPE html><html lang='es'><head> <meta charset='UTF-8'> <meta http-equiv='X-UA-Compatible' content='IE=edge'> <meta name='viewport' content='width=device-width, initial-scale=1.0'> <style>html{box-sizing: border-box; font-family: sans-serif; font-size: 16px;}*,*::after,*::before{box-sizing: inherit;}.factura{margin-left: auto; margin-right: auto; /* border: medium solid #000; */ width: 700px; height: 900px;}.cabecera-factura{background-color: #65ccd3; border-radius: 0.25rem; text-align: center; height: 45px; /* padding-top: 0.02rem; */}.datos-emisor{background-color: #eee; border: thin solid #000; border-radius: 0.5rem; margin: 0.5rem; padding: 1rem; font-weight: bolder;}.datos-emisor input{border: none; border-bottom: medium solid #000;}.datos-emisor input:nth-of-type(odd){width: 70%;}.factura h3{padding-left: 2rem;}.article-col-3{background-color: #eee; font-size: 0.85rem; border: 1px solid #000; border-radius: 0.5rem; width: 699px; height: 70px;}.article-col-3 > *{padding: 0.5rem; height: 100%; box-sizing: border-box; width: 232px; float: left;}.article-col-3 input{border: none; border-bottom: thin solid #000;}.item input{margin-top: 0.35rem;}.datos-venta{height: 450px; width: 696px; border: thin solid 000;}.encabezado-venta{/* border: thin solid cyan; */ width: 100%; height: 40px;}.item-1{border: thin solid #000;}.item-cantidad,.item-total{width: 90px; height: 100%;}.encabezado-venta > *{padding: 0.5rem; height: 100%; box-sizing: border-box; float: left; color: #fff; background-color: #1c3461; font-style: bold;}.item-descripcion{width: 513px; padding-left: 3rem;}.descripcion-venta{/* border: thin solid green; */ width: 100%; height: 300px;}.txt-area{display: block; /* border: none;*/ border: thin solid #000; resize: none; height: 100%;}.area-cantidad,.area-total{width: 90px;}.area-descripcion{width: 513px;}.descripcion-venta > *{padding: 0.5rem; border-radius: 0; height: 100%; box-sizing: border-box; float: left;}.descuento-venta{/* border: thin solid palevioletred; */ width: 50%; height: 99px; float: right;}.container{border: thin solid #000; height: 100%;}.textos{width: 256px;}.cantidades{width: 90px;}.descuento-venta > *{box-sizing: border-box; float: left;}.textos label{/* border-top: thin solid #000; */ text-align: center; padding: 0.4rem; display: block; height: 33px;}.cantidades label{/* border-top: thin solid #000; */ text-align: center; padding: 0.4rem; display: block; height: 33px;}.textos label:last-child{border-top: thin solid #000;}.cantidades > *{border: none; height: 33px; width: 100%;}.cantidades input:last-child{border-top: thin solid #000; border-bottom: thin solid #000;}.box-correo input{width: 50%; border: none; border-bottom: thin solid #000;}.box-correo{margin-top: 1rem;}</style></head><body> <div class='factura'> <form action=''> <header class='cabecera-factura'> <h1>FACTURA ELECTRONICA ©</h1> </header> <br><article class='article-col-3'> <section class='item'> <label for='txt-codigo'>Código de Autorización </label><br><input type='text' id='txt-codigo' value='" + cod_auth + "'> </section> <section class='item'> <label for='txt-num-serie'>Número de Serie: </label><br><input type='text' id='txt-num-serie' value='" + num_serie + "'> </section> <section class='item'> <label for='txt-moneda'>Moneda: </label><br><input type='text' id='txt-moneda' value='Quetzales'> </section> </article> <h3>Datos del Emisor</h3> <section class='datos-emisor'> <label for='txt-nombre'>Nombre: </label> <input type='text' id='txt-nombre' value='" + nombre_emisor + "'> <br><br><label for='txt-nit'>Nit: </label> <input type='text' id='txt-nit' value='" + nit_emisor + "'> <br><br><label for='txt-direccion'>Dirección: </label> <input type='text' id='txt-direccion' value='" + dir_emisor + "'> </section> <h3>Datos de Venta</h3> <section class='datos-venta'> <article class='encabezado-venta'> <section class='item-1 item-cantidad'>Cantidad</section> <section class='item-1 item-descripcion'>Descripción</section> <section class='item-1 item-total'>Total</section> </article> <section class='descripcion-venta'> <textarea class='txt-area area-cantidad' name='' id='' cols='30' rows='10'>1</textarea> <textarea class='txt-area area-descripcion' name='' id='' cols='30' rows='10'>Compra Realizada en linea, no genera derecho a credito fiscal</textarea> <textarea class='txt-area area-total' name='' id='' cols='30' rows='10'>" + _totalsindesc + "</textarea> </section> <section class='descuento-venta'> <div class='container textos'> <label for='txt-subtotal'>Sub-total</label> <label for='txt-descuento'>Descuento</label> <label for='txt-total'><b>TOTAL</b></label> </div><div class='container cantidades'> <label id='txt-subtotal'>" + _totalsindesc + "</label><label id='txt-descuento'>" + descuentos + "</label><label id='txt-total'>" + _total + "</label><input type='text' name='' id='txt-subtotal' value='" + _totalsindesc + "'> <input type='text' name='' id='txt-descuento' value='" + descuentos + "'> <input type='text' name='' id='txt-total' value='" + _total + "'> </div></section> </section><div class='box-correo'> <label for='txt-correo'><b>Total en letras</b></label> <input type='text' name='' id='txt-totalLetras' value='" + totalLetras + "'> </div><div class='box-correo'> <label for='txt-correo'><b>E-mail:  </b></label>" + email + " <input type='text' name='' id='txt-correo' value='" + email + "'> </div></form> </div></body></html>";
                
                string fpath = Server.MapPath("\\RUTA_DONDE_SE_GUARDA_ARCHIVO\\");
                string _filename = System.Guid.NewGuid().ToString();
                string file = fpath + _filename + ".pdf";

                int pdf = generarPDF("http://localhost:NUMERO_DE_PUERTO/", html, file);

                if (pdf > 0)
                {
                    rd.pdfReportPath = "\\RUTA_DONDE_SE_GUARDA_ARCHIVO\\" + _filename + ".pdf";                    

                }
                else
                {
                    rd.pdfReportPath = "";
                }
                
                _li.Add(rd);
                return _li;
            }
            catch
            {
                
                rd.pdfReportPath = "";
                _li.Add(rd);
                return _li;
                //return null;
            }
        }        
        
        public int generarPDF(string baseuri, string html, string destino)
        {
            try
            {
                ConverterProperties prop = new ConverterProperties();
                prop.SetBaseUri(baseuri);
                HtmlConverter.ConvertToPdf(html, new FileStream(destino, FileMode.Create), prop);
                return 1;
            }
            catch
            {
                return 0;
            }
        }        

        public string enviarEmail(string email, string nombre, string nitemisot, string num_serie, string totalLetras,string filePath)
        {
            string estado = "";

            //envio de email
            System.Net.Mail.MailMessage mmsg = new System.Net.Mail.MailMessage();

            mmsg.To.Add(email);
            mmsg.Subject = "Confirmacion de Pago";
            mmsg.SubjectEncoding = System.Text.Encoding.UTF8;

            //que agregue copia a mi correo
            mmsg.Bcc.Add("correoparadjuntar@prueba.com");

            /*mmsg.Body = "Buenas dia estimado cliente.   " +
                "Le informamos que se ha realizado exitosamente la transaccion a nombre de: " + nombre + " ,\n" +
                "con numero de nit: " + nitemisot +
                " factura serie No. \n" + num_serie + 
                "  Gracias por usar nuestro servicio de factura electronica";*/

            mmsg.Body = "<font> Estimado cliente:</font>" +
                "<br>" +
                "<font><strong>Le informamos que se ha realizado exitosamente la transaccion a nombre de: "+ nombre + ",con numero de nit: "+ nitemisot+" y factura serie no. "+ num_serie +" por un total de "+ totalLetras +"</strong></font>" +
                "<br>" +
                "<font> Gracias por usar nuestro servicio de factura electronica</font>";
            mmsg.BodyEncoding = System.Text.Encoding.UTF8;
            mmsg.IsBodyHtml = true;
            mmsg.Attachments.Add(new Attachment(@"C:\rutaDelPdf" + filePath));
            //mmsg.Attachments.Add(new Attachment(filePath));
            mmsg.From = new System.Net.Mail.MailAddress("correoemisor@prueba.com");


            //Cliente correo
            System.Net.Mail.SmtpClient cliente = new System.Net.Mail.SmtpClient();

            //credenciales
            cliente.Credentials = new System.Net.NetworkCredential("correoemisor@prueba.com", "contrase?a");

            cliente.Port = 587;
            cliente.EnableSsl = true;
            cliente.Host = "smtp.gmail.com";


            //logica de ejecución
            try
            {
                cliente.Send(mmsg);
            }
            catch (Exception)
            {

            }

            return estado;
        }

        public string obtenerDireccionLista(List<ModeloReporte> lista)
        {            
            ModeloReporte rd = new ModeloReporte();            

            rd = lista.ElementAt(0);
            string link = rd.pdfReportPath;

            return link; 
        }

        public XmlDocument CreateXMLFile(string nombre_emisor, string nit_emisor, string dir_emisor, string cod_auth, string num_serie, string moneda, string productos, string descuentos)
        {
            XmlDocument doc = new XmlDocument();

            XmlElement root = doc.CreateElement("Factura_DTE");
            doc.AppendChild(root);


            XmlNode raiz = doc.SelectSingleNode("Factura_DTE");

            //agrega Info emisor  a Factura_DTE
            XmlElement Info_emisor = doc.CreateElement("Info_emisor");
            raiz.AppendChild(Info_emisor);

            //agrega información de la factura
            XmlElement Info_Factura = doc.CreateElement("Info_Factura");
            raiz.AppendChild(Info_Factura);

            //agrega producto a Factura_DTE            
            XmlElement producto = doc.CreateElement("Productos");
            raiz.AppendChild(producto);

            //agrega autorización a Factura_DTE            
            XmlElement cert = doc.CreateElement("Certificacion");
            raiz.AppendChild(cert);

            //------------------------------------------------------------------

            //Agrega datos del emisor
            XmlElement emisor_nom = doc.CreateElement("Nombre_emisor");
            emisor_nom.InnerText = nombre_emisor;
            Info_emisor.AppendChild(emisor_nom);

            XmlElement emisor_nit = doc.CreateElement("Nit_emisor");
            emisor_nit.InnerText = nit_emisor;
            Info_emisor.AppendChild(emisor_nit);

            XmlElement emisor_dir = doc.CreateElement("Direccion_emisor");
            emisor_dir.InnerText = dir_emisor;
            Info_emisor.AppendChild(emisor_dir);

            //------------------------------------------------------------------

            //Agrega información de la factura
            XmlElement factura_codauth = doc.CreateElement("CodigoAuth_Factura");
            factura_codauth.InnerText = cod_auth;
            Info_Factura.AppendChild(factura_codauth);

            XmlElement factura_numserie = doc.CreateElement("NumSerie_Factura");
            factura_numserie.InnerText = num_serie;
            Info_Factura.AppendChild(factura_numserie);

            XmlElement factura_moneda = doc.CreateElement("Moneda_Factura");
            factura_moneda.InnerText = moneda;
            Info_Factura.AppendChild(factura_moneda);
            //------------------------------------------------------------------

            //agrega Productos
            List<string> prods = separadorProductos(productos);

            //contador para total
            string total = "";
            double totalDineroProd = 0;
            double totalDineroProd = 0;

            for (int i = 0; i < prods.Count; i++)
            {
                string[] descripcion_prod = prods[i].Split(',');

                XmlElement prod_ind = doc.CreateElement("Producto");
                producto.AppendChild(prod_ind);

                XmlAttribute id = doc.CreateAttribute("id");
                id.Value = doc.SelectNodes("Factura_DTE/Productos/Producto").Count.ToString();
                prod_ind.Attributes.Append(id);

                XmlElement cantidad = doc.CreateElement("Cantidad");
                cantidad.InnerText = descripcion_prod[0];
                prod_ind.AppendChild(cantidad);
                int cantidad_prod = Int32.Parse(descripcion_prod[0]);

                XmlElement nombre = doc.CreateElement("Nombre");
                nombre.InnerText = descripcion_prod[1];
                prod_ind.AppendChild(nombre);

                XmlElement precio = doc.CreateElement("Precio");
                precio.InnerText = descripcion_prod[2];
                prod_ind.AppendChild(precio);
                double precio_prod = Convert.ToDouble(descripcion_prod[2]);

                Array.Clear(descripcion_prod, 0, descripcion_prod.Length);
                totalDineroProd = totalDineroProd + (cantidad_prod * precio_prod);
            }
            totalsindesc = totalDineroProd
            totalDineroProd = totalDineroProd - Convert.ToDouble(descuentos);

            //agrego total con letras
            XmlElement totalComprasin = doc.CreateElement("Total_sin_descuento");
            totalComprasin.InnerText = totalsindesc.ToString();
            producto.AppendChild(totalComprasin);

            XmlElement totalCompra = doc.CreateElement("Total_con_descuento");
            totalCompra.InnerText = totalDineroProd.ToString();
            producto.AppendChild(totalCompra);

            
            XmlElement totalCompra_letras = doc.CreateElement("Total_en_Letras");
            totalCompra_letras.InnerText = Convertir_NumALetras(totalDineroProd);
            producto.AppendChild(totalCompra_letras);

            //agrega codigo de certificacion en XML
            XmlElement certificacion = doc.CreateElement("Cod_certificacion");
            certificacion.InnerText = GenerarAlfanumerico();
            cert.AppendChild(certificacion);

            //agrega hora de certificacion
            XmlElement hora_cert = doc.CreateElement("FechaHora_certificacion");
            hora_cert.InnerText = DateTime.Now.ToString();
            cert.AppendChild(hora_cert);

            /*
            //agrega id
            XmlAttribute id = doc.CreateAttribute("id");
            id.Value = doc.SelectNodes("Factura_DTE/Producto").Count.ToString();
            producto.Attributes.Append(id);

                //agrega nombre
                XmlElement nombre = doc.CreateElement("Nombre");
                nombre.InnerText = "PRUEBA 1";
                producto.AppendChild(nombre);

                //agrega codigo
                XmlElement codigo = doc.CreateElement("Codigo");
                codigo.InnerText = "000001256";
                producto.AppendChild(codigo);
            */

            return doc;
        }

        public string GenerarAlfanumerico()
        {
            string resp = "";
            var characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var Charsarr = new char[128];
            var random = new Random();

            for (int i = 0; i < Charsarr.Length; i++)
            {
                Charsarr[i] = characters[random.Next(characters.Length)];
            }

            var resultString = new String(Charsarr);

            return resultString;

        }

        public static string Convertir_NumALetras(double numberAsString)
        {
            string dec;

            var entero = Convert.ToInt64(Math.Truncate(numberAsString));
            var decimales = Convert.ToInt32(Math.Round((numberAsString - entero) * 100, 2));
            if (decimales > 0)
            {
                //dec = " QUETZALES CON " + decimales.ToString() + "/100";
                dec = $" QUETZALES {decimales:0,0} /100";
            }
            //Código agregado por mí
            else
            {
                //dec = " QUETZALES CON " + decimales.ToString() + "/100";
                dec = $" QUETZALES {decimales:0,0} /100";
            }
            var res = NumeroALetras(Convert.ToDouble(entero)) + dec;
            return res;
        }

        private static string NumeroALetras(double value)
        {
            string num2Text; value = Math.Truncate(value);
            if (value == 0) num2Text = "CERO";
            else if (value == 1) num2Text = "UNO";
            else if (value == 2) num2Text = "DOS";
            else if (value == 3) num2Text = "TRES";
            else if (value == 4) num2Text = "CUATRO";
            else if (value == 5) num2Text = "CINCO";
            else if (value == 6) num2Text = "SEIS";
            else if (value == 7) num2Text = "SIETE";
            else if (value == 8) num2Text = "OCHO";
            else if (value == 9) num2Text = "NUEVE";
            else if (value == 10) num2Text = "DIEZ";
            else if (value == 11) num2Text = "ONCE";
            else if (value == 12) num2Text = "DOCE";
            else if (value == 13) num2Text = "TRECE";
            else if (value == 14) num2Text = "CATORCE";
            else if (value == 15) num2Text = "QUINCE";
            else if (value < 20) num2Text = "DIECI" + NumeroALetras(value - 10);
            else if (value == 20) num2Text = "VEINTE";
            else if (value < 30) num2Text = "VEINTI" + NumeroALetras(value - 20);
            else if (value == 30) num2Text = "TREINTA";
            else if (value == 40) num2Text = "CUARENTA";
            else if (value == 50) num2Text = "CINCUENTA";
            else if (value == 60) num2Text = "SESENTA";
            else if (value == 70) num2Text = "SETENTA";
            else if (value == 80) num2Text = "OCHENTA";
            else if (value == 90) num2Text = "NOVENTA";
            else if (value < 100) num2Text = NumeroALetras(Math.Truncate(value / 10) * 10) + " Y " + NumeroALetras(value % 10);
            else if (value == 100) num2Text = "CIEN";
            else if (value < 200) num2Text = "CIENTO " + NumeroALetras(value - 100);
            else if ((value == 200) || (value == 300) || (value == 400) || (value == 600) || (value == 800)) num2Text = NumeroALetras(Math.Truncate(value / 100)) + "CIENTOS";
            else if (value == 500) num2Text = "QUINIENTOS";
            else if (value == 700) num2Text = "SETECIENTOS";
            else if (value == 900) num2Text = "NOVECIENTOS";
            else if (value < 1000) num2Text = NumeroALetras(Math.Truncate(value / 100) * 100) + " " + NumeroALetras(value % 100);
            else if (value == 1000) num2Text = "MIL";
            else if (value < 2000) num2Text = "MIL " + NumeroALetras(value % 1000);
            else if (value < 1000000)
            {
                num2Text = NumeroALetras(Math.Truncate(value / 1000)) + " MIL";
                if ((value % 1000) > 0)
                {
                    num2Text = num2Text + " " + NumeroALetras(value % 1000);
                }
            }
            else if (value == 1000000)
            {
                num2Text = "UN MILLON";
            }
            else if (value < 2000000)
            {
                num2Text = "UN MILLON " + NumeroALetras(value % 1000000);
            }
            else if (value < 1000000000000)
            {
                num2Text = NumeroALetras(Math.Truncate(value / 1000000)) + " MILLONES ";
                if ((value - Math.Truncate(value / 1000000) * 1000000) > 0)
                {
                    num2Text = num2Text + " " + NumeroALetras(value - Math.Truncate(value / 1000000) * 1000000);
                }
            }
            else if (value == 1000000000000) num2Text = "UN BILLON";
            else if (value < 2000000000000) num2Text = "UN BILLON " + NumeroALetras(value - Math.Truncate(value / 1000000000000) * 1000000000000);
            else
            {
                num2Text = NumeroALetras(Math.Truncate(value / 1000000000000)) + " BILLONES";
                if ((value - Math.Truncate(value / 1000000000000) * 1000000000000) > 0)
                {
                    num2Text = num2Text + " " + NumeroALetras(value - Math.Truncate(value / 1000000000000) * 1000000000000);
                }
            }
            return num2Text;
        }
    

        public string ConvertirABase64(string codigo)
        {
            string resp = "";

            return resp;
                
        }

        [WebMethod]
        public static void Serializar()
        {

        }

        public string SplitProd()
        {
            string prod = "";

            return prod;
        }


        public static List<string> separadorProductos(string productos)
        {
            List<string> prods = new List<string>();

            //creamos array con productos individuales, es decir los separamos
            string[] prod_individuales = productos.Split(';');

            for (int i = 0; i < prod_individuales.Length; i++)
            {
                prods.Add(prod_individuales[i]);
            }

            return prods;
        }
        
        //Administrar usuarios
        [WebMethod]
        public string registrarUsuario(string dpi, string nombres, string apellidos, string nit, string direccion, string email)
        {
            string estado = "";

            MySqlConnection conexionBD = new MySqlConnection("server = servidor ; database = base_de_datos ; Uid = root ; pwd = Contrase?a");

            //inserto datos a la base de datos
            conexionBD.Open();
            MySqlCommand comando = new MySqlCommand("insert into  `base_de_datos`.`usuarios` (dpi, nombres, apellidos, nit, direccion, email) values (@dpi, @nombres, @apellidos, @nit, @direccion, @email)", conexionBD);
            comando.Parameters.AddWithValue("@dpi", dpi);
            comando.Parameters.AddWithValue("@nombres", nombres);
            comando.Parameters.AddWithValue("@apellidos", apellidos);
            comando.Parameters.AddWithValue("@nit", nit);
            comando.Parameters.AddWithValue("@direccion", direccion);
            comando.Parameters.AddWithValue("@email", email);

            try
            {
                comando.ExecuteNonQuery();
                estado = "exito";
            }
            catch (Exception)
            {
                estado = "error";
            }
            conexionBD.Close();

            return estado;
        }

        [WebMethod]
        public string eliminarrUsuario(string dpi)
        {
            string estado = "";

            try
            {
                MySqlConnection conexionBD = new MySqlConnection("server = servidor ; database = base_de_datos ; Uid = root ; pwd = Contrase?a");

                //elimino datos a la base de datos
                conexionBD.Open();
                MySqlCommand comando = new MySqlCommand("delete from  `base_de_datos`.`usuarios` where dpi = " + dpi, conexionBD);

                comando.ExecuteNonQuery();
                estado = "exito";
                conexionBD.Close();
            }
            catch (Exception)
            {
                estado = "error";
            }

            return estado;
        }

        [WebMethod]
        public string modificarUsuario(string dpi, string nombres, string apellidos, string nit, string direccion, string email)
        {
            string estado = "";

            MySqlConnection conexionBD = new MySqlConnection("server = servidor ; database = base_de_datos ; Uid = root ; pwd = Contrase?a");

            //inserto datos a la base de datos
            conexionBD.Open();
            MySqlCommand comando = new MySqlCommand("update `base_de_datos`.`usuarios` set nombres = @nombres, apellidos = @apellidos, nit = @nit, direccion = @direccion, email = @email where dpi = @dpi", conexionBD);
            comando.Parameters.AddWithValue("@dpi", dpi);
            comando.Parameters.AddWithValue("@nombres", nombres);
            comando.Parameters.AddWithValue("@apellidos", apellidos);
            comando.Parameters.AddWithValue("@nit", nit);
            comando.Parameters.AddWithValue("@direccion", direccion);
            comando.Parameters.AddWithValue("@email", email);

            try
            {
                comando.ExecuteNonQuery();
                estado = "exito";
            }
            catch (Exception)
            {
                estado = "error";
            }
            conexionBD.Close();

            return estado;
        }

        [WebMethod]
        public XmlDocument consultarUsuario(string dpi)
        {
            string estado = "";
            XmlDocument doc = new XmlDocument();
            XmlDeclaration declaration = doc.CreateXmlDeclaration("1.0", "utf-8", "no");
            try
            {
                MySqlConnection conexionBD = new MySqlConnection("server = servidor ; database = base_de_datos ; Uid = root ; pwd = Contrase?a");

                //selecciono datos a la base de datos
                conexionBD.Open();
                MySqlCommand comando = new MySqlCommand("select * from `base_de_datos`.`usuarios` where dpi = @dpi ", conexionBD);
                comando.Parameters.AddWithValue("@dpi", dpi);

                MySqlDataReader lector = comando.ExecuteReader();
                lector.Read();

                string dpiXml = lector.GetString(0);
                string nombresXml = lector.GetString(1);
                string apellidosXml = lector.GetString(2);
                string nitXml = lector.GetString(3);
                string direccionXml = lector.GetString(4);
                string emailXml = lector.GetString(5);

                conexionBD.Close();


                //creacion de xml                            
                XmlElement root = doc.CreateElement("Usuario_XML");
                doc.AppendChild(root);
                XmlNode raiz = doc.SelectSingleNode("Usuario_XML");

                XmlElement dpi_Xml = doc.CreateElement("DPI");
                dpi_Xml.InnerText = dpiXml;
                raiz.AppendChild(dpi_Xml);

                XmlElement nombre_Xml = doc.CreateElement("NOMBRE");
                nombre_Xml.InnerText = nombresXml;
                raiz.AppendChild(nombre_Xml);

                XmlElement apellidos_Xml = doc.CreateElement("APELLIDOS");
                apellidos_Xml.InnerText = apellidosXml;
                raiz.AppendChild(apellidos_Xml);

                XmlElement nit_Xml = doc.CreateElement("NIT");
                nit_Xml.InnerText = nitXml;
                raiz.AppendChild(nit_Xml);

                XmlElement direccion_Xml = doc.CreateElement("DIRECCION");
                direccion_Xml.InnerText = direccionXml;
                raiz.AppendChild(direccion_Xml);

                XmlElement email_Xml = doc.CreateElement("DIRECCION");
                email_Xml.InnerText = emailXml;
                raiz.AppendChild(email_Xml);

                XmlElement info_Xml = doc.CreateElement("ESTADO");
                info_Xml.InnerText = "Exito";
                raiz.AppendChild(info_Xml);


            }
            catch (Exception)
            {
                XmlElement root = doc.CreateElement("Usuario_XML");
                doc.AppendChild(root);
                XmlNode raiz = doc.SelectSingleNode("Usuario_XML");

                XmlElement info_Xml = doc.CreateElement("ESTADO");
                info_Xml.InnerText = "Fallo";
                raiz.AppendChild(info_Xml);
            }

            return doc;
        }
        
        //Administrar Facturas una factura, solo se crea, se elimina y se consulta, no se puede modificar
        //consultar facturas
        [WebMethod]
        public XmlDocument consultarFactura(string numSerie)
        {
            string factura = "";

            XmlDocument doc = new XmlDocument();
            XmlDeclaration declaration = doc.CreateXmlDeclaration("1.0", "utf-8", "no");

            try
            {
                MySqlConnection conexionBD = new MySqlConnection("server = servidor ; database = base_de_datos ; Uid = root ; pwd = Contrase?a");

                //selecciono datos a la base de datos
                conexionBD.Open();
                MySqlCommand comando = new MySqlCommand("select * from `base_de_datos`.`factura` where numSerie = @numSerie ", conexionBD);
                comando.Parameters.AddWithValue("@numSerie", numSerie);

                MySqlDataReader lector = comando.ExecuteReader();
                lector.Read();

                string numSerieXml = lector.GetString(0);
                string nombreEmisorXml = lector.GetString(1);
                string codAutorizacionXml = lector.GetString(2);
                string xmlFacturaXml = lector.GetString(3);

                conexionBD.Close();

                //creacion de xml                            
                XmlElement root = doc.CreateElement("Factura_XML");
                doc.AppendChild(root);
                XmlNode raiz = doc.SelectSingleNode("Factura_XML");

                XmlElement numSerie_Xml = doc.CreateElement("numSerie");
                numSerie_Xml.InnerText = numSerieXml;
                raiz.AppendChild(numSerie_Xml);

                XmlElement nombreEmisor_Xml = doc.CreateElement("nombreEmisor");
                nombreEmisor_Xml.InnerText = nombreEmisorXml;
                raiz.AppendChild(nombreEmisor_Xml);

                XmlElement codAutorizacion_Xml = doc.CreateElement("codAutorizacion");
                codAutorizacion_Xml.InnerText = codAutorizacionXml;
                raiz.AppendChild(codAutorizacion_Xml);

                XmlElement xmlFacturaBase64_Xml = doc.CreateElement("xmlFacturaBase64");
                xmlFacturaBase64_Xml.InnerText = xmlFacturaXml;
                raiz.AppendChild(xmlFacturaBase64_Xml);

                XmlElement info_Xml = doc.CreateElement("ESTADO");
                info_Xml.InnerText = "Exito";
                raiz.AppendChild(info_Xml);
            }
            catch
            {
                XmlElement root = doc.CreateElement("Factura_XML");
                doc.AppendChild(root);
                XmlNode raiz = doc.SelectSingleNode("Factura_XML");

                XmlElement info_Xml = doc.CreateElement("ESTADO");
                info_Xml.InnerText = "Fallo";
                raiz.AppendChild(info_Xml);
            }
            /*
            XmlDocument doc = new XmlDocument();
            XmlDeclaration declaration = doc.CreateXmlDeclaration("1.0", "utf-8", "no");
            try
            {
                MySqlConnection conexionBD = new MySqlConnection("server = servidor ; database = base_de_datos ; Uid = root ; pwd = Contrase?a");

                //selecciono datos a la base de datos
                conexionBD.Open();
                MySqlCommand comando = new MySqlCommand("select * from `base_de_datos`.`usuarios` where dpi = @dpi ", conexionBD);
                comando.Parameters.AddWithValue("@dpi", dpi);

                MySqlDataReader lector = comando.ExecuteReader();
                lector.Read();

                string dpiXml = lector.GetString(0);
                string nombresXml = lector.GetString(1);
                string apellidosXml = lector.GetString(2);
                string nitXml = lector.GetString(3);
                string direccionXml = lector.GetString(4);
                string emailXml = lector.GetString(5);

                conexionBD.Close();


                //creacion de xml                            
                XmlElement root = doc.CreateElement("Usuario_XML");
                doc.AppendChild(root);
                XmlNode raiz = doc.SelectSingleNode("Usuario_XML");

                XmlElement dpi_Xml = doc.CreateElement("DPI");
                dpi_Xml.InnerText = dpiXml;
                raiz.AppendChild(dpi_Xml);

                XmlElement nombre_Xml = doc.CreateElement("NOMBRE");
                nombre_Xml.InnerText = nombresXml;
                raiz.AppendChild(nombre_Xml);

                XmlElement apellidos_Xml = doc.CreateElement("APELLIDOS");
                apellidos_Xml.InnerText = apellidosXml;
                raiz.AppendChild(apellidos_Xml);

                XmlElement nit_Xml = doc.CreateElement("NIT");
                nit_Xml.InnerText = nitXml;
                raiz.AppendChild(nit_Xml);

                XmlElement direccion_Xml = doc.CreateElement("DIRECCION");
                direccion_Xml.InnerText = direccionXml;
                raiz.AppendChild(direccion_Xml);

                XmlElement email_Xml = doc.CreateElement("DIRECCION");
                email_Xml.InnerText = emailXml;
                raiz.AppendChild(email_Xml);

                XmlElement info_Xml = doc.CreateElement("ESTADO");
                info_Xml.InnerText = "Exito";
                raiz.AppendChild(info_Xml);


            }
            catch (Exception)
            {
                XmlElement root = doc.CreateElement("Usuario_XML");
                doc.AppendChild(root);
                XmlNode raiz = doc.SelectSingleNode("Usuario_XML");

                XmlElement info_Xml = doc.CreateElement("ESTADO");
                info_Xml.InnerText = "Fallo";
                raiz.AppendChild(info_Xml);
            }*/

            return doc;
        }

        [WebMethod]
        public string eliminarFactura(string numSerie, string nombreEmisor)
        {
            string estado = "";

            try
            {
                MySqlConnection conexionBD = new MySqlConnection("server = servidor ; database = base_de_datos ; Uid = root ; pwd = Contrase?a");

                //elimino datos a la base de datos
                conexionBD.Open();
                MySqlCommand comando = new MySqlCommand("delete from  `base_de_datos`.`factura` where numSerie = @numSerie and nombreEmisor= @nombreEmisor", conexionBD);
                comando.Parameters.AddWithValue("@numSerie", numSerie);
                comando.Parameters.AddWithValue("@nombreEmisor", nombreEmisor);

                comando.ExecuteNonQuery();
                estado = "exito";
                conexionBD.Close();
            }
            catch (Exception)
            {
                estado = "error";
            }

            return estado;
        }       
        
        
    }

}
