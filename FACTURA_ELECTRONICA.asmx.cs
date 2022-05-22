﻿using System;
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
            XmlNode MyNode = DTE.SelectSingleNode("Factura_DTE/Productos/Total");
            _total = MyNode.InnerText;

            XmlNode MyNode2 = DTE.SelectSingleNode("Factura_DTE/Productos/Total_en_Letras");
            totalLetras = MyNode2.InnerText;

            //genero el pdf de factura y la envío            
            List<ModeloReporte> _li = new List<ModeloReporte>();
            ModeloReporte rd = new ModeloReporte();

            _li = generarFacturaPDF(nombre_emisor, nit_emisor, cod_auth, num_serie, descuentos, email, _total);            
            enviarEmail(email, nombre_emisor, nit_emisor,num_serie, totalLetras, obtenerDireccionLista(_li));

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
            totalDineroProd = totalDineroProd - Convert.ToDouble(descuentos);

            //agrego total con letras
            XmlElement totalCompra = doc.CreateElement("Total");
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
    }

}
