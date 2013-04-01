using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Security.Cryptography;
using System.IO;
using System.Xml;
using System.Web;

namespace Thesis.Ipsum
{
    /// <summary>
    /// Class to simplify the connection towards the Ipsum Cloud
    /// </summary>
    public class IpsumClient
    {
        string _privateKey;
        string _host;
        string _target;

        /// <summary>
        /// Create a new helper class.
        /// </summary>
        /// <param name="host">Ipsum Host URL e.g. ipsum.groept.be</param>
        /// <param name="target">The target path on the host. default: "/" </param>
        /// <param name="PrivateKey">The private key that was provided for a specific user account</param>
        public IpsumClient(string host, string target, string PrivateKey)
        {
            if (System.Threading.Thread.CurrentThread.CurrentCulture != System.Globalization.CultureInfo.InvariantCulture)
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            }
            _privateKey = PrivateKey.ToLower();
            _host = host;
            _target = target;
        }

        /// <summary>
        /// Session Token
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Session expire date
        /// </summary>
        public DateTime TokenExpire { get; set; }

        /// <summary>
        /// Authenticate a user account. (account credentials for specified private key)
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public void Authenticate(string username, string password)
        {
            // Create the web request  
            HttpWebRequest request = WebRequest.Create(CreateUrl("auth/{code}")) as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "application/xml";

            string data = "<UserLogin><username>{username}</username><password>{password}</password></UserLogin>";
            data = data.Replace("{username}", username).Replace("{password}", password);
            byte[] byteData = UTF8Encoding.UTF8.GetBytes(data.ToString());
            using (Stream postStream = request.GetRequestStream())
            {
                postStream.Write(byteData, 0, byteData.Length);
            }
            // Get response  
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                // Get the response stream  
                StreamReader reader = new StreamReader(response.GetResponseStream());
                XmlReader xml = XmlReader.Create(reader);
                xml.ReadToFollowing("token");
                if (xml.EOF) { throw new InvalidDataException(); }
                Token = xml.ReadElementContentAsString();
                if (xml.Name != "error") xml.ReadToFollowing("error");
                if (Convert.ToBoolean(xml.ReadElementContentAsString())) { throw new UnauthorizedAccessException(); }
                if (xml.Name != "expire") xml.ReadToFollowing("expire");
                TokenExpire = xml.ReadElementContentAsDateTime();
            }

        }

        /// <summary>
        /// Uploaden van nieuwe data voor een bepaalde sensor.
        /// </summary>
        /// <param name="destination">Sensor wordt geaddresseerd door een juiste destination op te geven</param>
        /// <param name="xml">XML String to upload the data</param>
        /// <returns>Response</returns>
        public string Upload(Destination destination, string xml)
        {
            // Create the web request  
            HttpWebRequest request = WebRequest.Create(CreateUrl("upload/" + destination + "/{code}")) as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "application/xml";

            string data = xml;
            byte[] byteData = UTF8Encoding.UTF8.GetBytes(data.ToString());
            using (Stream postStream = request.GetRequestStream())
            {
                postStream.Write(byteData, 0, byteData.Length);
            }
            // Get response  
            string resp = "";
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                // Get the response stream  
                StreamReader reader = new StreamReader(response.GetResponseStream());
                resp = reader.ReadToEnd();
                reader.Dispose();
            }
            return resp;
        }

        /// <summary>
        /// Perform advanced calculations
        /// </summary>
        /// <param name="destination">Sensor to perform calculations on</param>
        /// <param name="xml">XML that defines the calculations to be performed</param>
        /// <returns>Results</returns>
        public string Calculation(Destination destination, string xml = null)
        {
            // Create the web request  
            HttpWebRequest request = WebRequest.Create(CreateUrl("select/{token}/" + destination + "/{code}")) as HttpWebRequest;
            if (xml == null)
                request.Method = "GET";
            else
            {
                request.Method = "POST";
                request.ContentType = "application/xml";

                string data = xml;
                byte[] byteData = UTF8Encoding.UTF8.GetBytes(data.ToString());
                using (Stream postStream = request.GetRequestStream())
                {
                    postStream.Write(byteData, 0, byteData.Length);
                }
            }
            // Get response  
            string resp = "";
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                // Get the response stream  
                StreamReader reader = new StreamReader(response.GetResponseStream());
                resp = reader.ReadToEnd();
                reader.Dispose();
            }
            return resp;
        }

        /// <summary>
        /// Download all data from a specific sensor.
        /// Performing a Calculation query is much faster.
        /// </summary>
        /// <param name="destination">Destination of the sensor to download the data from</param>
        /// <returns>Results</returns>
        public string Download(Destination destination)
        {
            // Create the web request  
            HttpWebRequest request = WebRequest.Create(CreateUrl("objects/{token}/" + destination + "/{code}")) as HttpWebRequest;
            request.Method = "GET";

            // Get response  
            string resp = "";
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                // Get the response stream  
                StreamReader reader = new StreamReader(response.GetResponseStream());
                resp = reader.ReadToEnd();
                reader.Dispose();
            }
            return resp;
        }

        /// <summary>
        /// Load a specified destination, user, installation, sensor_group, sensor
        /// </summary>
        /// <param name="destination">Destination to load</param>
        /// <returns>Return destination information</returns>
        public string LoadDestination(Destination destination)
        {
            // Create the web request  
            HttpWebRequest request = WebRequest.Create(CreateUrl("entity/{token}/" + destination + "/{code}")) as HttpWebRequest;
            request.Method = "GET";

            // Get response  
            string resp = "";
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                // Get the response stream  
                StreamReader reader = new StreamReader(response.GetResponseStream());
                resp = reader.ReadToEnd();
                reader.Dispose();
            }
            return resp;
        }

        /// <summary>
        /// Load the children of a destination object.
        /// e.g. the sensors of a sensor_group or instalations of a user...
        /// </summary>
        /// <param name="destination">Object to download the children from</param>
        /// <returns>List of children</returns>
        public string LoadChildren(Destination destination)
        {
            // Create the web request  
            HttpWebRequest request = WebRequest.Create(CreateUrl("children/{token}/" + destination + "/{code}")) as HttpWebRequest;
            request.Method = "GET";

            // Get response  
            string resp = "";
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                // Get the response stream  
                StreamReader reader = new StreamReader(response.GetResponseStream());
                resp = reader.ReadToEnd();
                reader.Dispose();
            }
            return resp;
        }

        /// <summary>
        /// Address a custom URL
        /// </summary>
        /// <param name="url">URL that needs to be addressed. (Without host address) All tags should be replaced exept the {code} and {token} tags.</param>
        /// <param name="xml">null if you want to perform a GET request, or an xml string to perform a POST request</param>
        /// <returns>The response</returns>
        public string Custom(string url, string xml = null)
        {
            // Create the web request  
            HttpWebRequest request = WebRequest.Create(CreateUrl(url)) as HttpWebRequest;
            if (xml == null)
                request.Method = "GET";
            else
            {
                request.Method = "POST";
                request.ContentType = "application/xml";
                string data = xml;
                byte[] byteData = UTF8Encoding.UTF8.GetBytes(data.ToString());
                using (Stream postStream = request.GetRequestStream())
                {
                    postStream.Write(byteData, 0, byteData.Length);
                }
            }
            // Get response  
            string resp = "";
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                // Get the response stream  
                StreamReader reader = new StreamReader(response.GetResponseStream());
                resp = reader.ReadToEnd();
                reader.Dispose();
            }
            return resp;
        }

        /// <summary>
        /// Parse the url
        /// </summary>
        /// <param name="uriTemplate">Url to parse</param>
        /// <returns></returns>
        private string CreateUrl(string uriTemplate)
        {
            uriTemplate = _target + uriTemplate.Replace("{token}", Token);

            string temp = uriTemplate.Replace("{code}", _privateKey);

            SHA1 sha = SHA1.Create();

            string encrypted = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(temp))).Replace("-", "").ToLower();

            uriTemplate = uriTemplate.Replace("{code}", encrypted);

            return _host + uriTemplate;
        }
    }
}