using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Xml;
using System.Xml.Linq;
using Thesis.Ipsum;
using Thesis.Models;

namespace Thesis.Controllers
{
    public class SensorController : Controller
    {
        private IpsumClient client = new IpsumClient("http://ipsum.groept.be", "/", "a31dd4f1-9169-4475-b316-764e1e737653");

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Module(int imageid, int floor)
        {
            ViewBag.image = imageid;

            switch (imageid)
            {
                case 1:
                    ViewBag.heading = "Ground floor";
                    break;
                case 2:
                    ViewBag.heading = "Module 3-6";
                    break;
                case 3:
                    ViewBag.heading = "Module 7-10";
                    break;
                case 4:
                    ViewBag.heading = "Module 11-14";
                    break;
                case 5:
                    ViewBag.heading = "Rooftop";
                    break;
                case 6:
                    ViewBag.heading = "Parking -1";
                    break;
                case 7:
                    ViewBag.heading = "Parking -2";
                    break;
                default:
                    break;
            }

            ViewBag.installation = floor;
            ViewBag.sensortypes = FetchAllSensorTypes();
            return View();
        }

        public ActionResult SensorDetails(int id, int sensorgroupid, int floorid)
        {
            ViewBag.image = id;
            ViewBag.installationid = floorid;
            ViewBag.sensorgroupid = sensorgroupid;
            ViewBag.sensorfields = FetchSensorNodeFields(floorid, sensorgroupid);
            return View();
        }

        public void IpsumLogin()
        {
            TimeSpan difference = DateTime.UtcNow - client.TokenExpire;
            if (difference.TotalMinutes >= 30)
                client.Authenticate("roel", "roel");
        }

        public JsonResult FetchAllSensors(int installationID)
        {
            IpsumLogin();

            Destination installationdest = new Destination(21, installationID, 0, 0);
            String xmlChildNodes = client.LoadChildren(installationdest);
            List<IpsumSensorNode> sensornodes = new List<IpsumSensorNode>();

            XmlDocument xmlreader = new XmlDocument();
            xmlreader.LoadXml(xmlChildNodes);
            XmlNodeList xmlsensornodes = xmlreader.DocumentElement.SelectNodes("//*[local-name()='SensorGroup']");

            foreach (XmlNode xmlsensornode in xmlsensornodes)
            {
                IpsumSensorNode sensornode = new IpsumSensorNode();

                XmlNode location = xmlsensornode.SelectSingleNode("./*[local-name()='location']");
                sensornode.location = location.InnerText;

                XmlNode id = xmlsensornode.SelectSingleNode("./*[local-name()='id']");
                sensornode.id = Convert.ToInt32(id.InnerText);

                XmlNode name = xmlsensornode.SelectSingleNode("./*[local-name()='name']");
                sensornode.name = name.InnerText;

                XmlNode description = xmlsensornode.SelectSingleNode("./*[local-name()='description']");
                sensornode.description = description.InnerText;

                XmlNode inuse = xmlsensornode.SelectSingleNode("./*[local-name()='inuse']");
                sensornode.inuse = Convert.ToBoolean(inuse.InnerText);

                Destination sensorgroupdest = new Destination(21, installationID, sensornode.id, 0);
                String xmlChildSensors = client.LoadChildren(sensorgroupdest);
                List<IpsumSensor> sensors = new List<IpsumSensor>();

                xmlreader.LoadXml(xmlChildSensors);
                XmlNodeList xmlsensors = xmlreader.DocumentElement.SelectNodes("//*[local-name()='Sensor']");

                foreach (XmlNode xmlsensor in xmlsensors)
                {
                    IpsumSensor sensor = new IpsumSensor();

                    XmlNode frequencysensor = xmlsensor.SelectSingleNode("./*[local-name()='frequency']");
                    sensor.frequency = Convert.ToInt32(frequencysensor.InnerText);

                    XmlNode idsensor = xmlsensor.SelectSingleNode("./*[local-name()='id']");
                    sensor.id = Convert.ToInt32(idsensor.InnerText);

                    XmlNode namesensor = xmlsensor.SelectSingleNode("./*[local-name()='name']");
                    sensor.name = namesensor.InnerText;

                    XmlNode descriptionsensor = xmlsensor.SelectSingleNode("./*[local-name()='description']");
                    sensor.description = descriptionsensor.InnerText;

                    XmlNode datanamesensor = xmlsensor.SelectSingleNode("./*[local-name()='dataname']");

                    String xmlsensortype = client.Custom("objects/{token}/{code}?filter=" + datanamesensor.InnerText);
                    xmlreader.LoadXml(xmlsensortype);
                    XmlNode sensortypenamenode = xmlreader.SelectSingleNode("//*[local-name()='name']");
                    sensor.type = sensortypenamenode.InnerText;

                    Destination sensordest = new Destination(21, installationID, sensornode.id, sensor.id);
                    String xmlsensorvalue = "";
                    try
                    {
                        xmlsensorvalue = client.Calculation(sensordest);
                    }
                    catch (WebException e)
                    {
                        sensor.lastvalue = "No data recorded yet";
                        sensor.timestamp = "";
                        sensors.Add(sensor);
                        break;
                    }

                    xmlreader.LoadXml(xmlsensorvalue);

                    XmlNode entry = xmlreader.DocumentElement.SelectSingleNode("//*[local-name()='entry']");
                    if (entry != null)
                    {
                        XmlNode value = entry.FirstChild;
                        sensor.lastvalue = value.InnerText;
                        XmlNode timestamp = entry.SelectSingleNode("//*[local-name()='timestamp']");
                        sensor.timestamp = timestamp.InnerText;
                    }
                    else
                    {
                        sensor.lastvalue = "No data recorded yet";
                        sensor.timestamp = "";
                    }

                    sensors.Add(sensor);
                }

                sensornode.sensors = sensors;
                sensornodes.Add(sensornode);
            }

            JsonResult json = Json(sensornodes, JsonRequestBehavior.AllowGet);
            return json;
        }

        public List<SelectListItem> FetchAllSensorTypes()
        {
            IpsumLogin();

            String xmlSensorTypes = client.Custom("objects/{token}/{code}");

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlSensorTypes);
            XmlNodeList names = doc.DocumentElement.SelectNodes("//*[local-name()='dataname']");

            List<SelectListItem> elements = new List<SelectListItem>();

            foreach (XmlNode name in names)
            {
                String xmlSingleType = client.Custom("structure/" + name.InnerText);

                XmlDocument doc2 = new XmlDocument();
                doc2.LoadXml(xmlSingleType);
                XmlNode type = doc2.DocumentElement;

                if (type.Name.Contains("zigbee"))
                {
                    SelectListItem element = new SelectListItem();
                    element.Text = type.Name;
                    element.Value = name.InnerText;
                    elements.Add(element);
                }
            }

            return elements;
        }

        public List<SelectListItem> FetchSensorNodeFields(int installationid, int sensorgroupid)
        {
            Destination sensornodedest = new Destination(21, installationid, sensorgroupid, 0);
            IpsumLogin();
            String xmlsensors = client.LoadChildren(sensornodedest);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlsensors);
            XmlNodeList sensors = doc.DocumentElement.SelectNodes("//*[local-name()='Sensor']");
            List<SelectListItem> fields = new List<SelectListItem>();

            foreach (XmlNode sensor in sensors)
            {
                XmlNode datanamenode = sensor.SelectSingleNode("./*[local-name()='dataname']");
                String dataname = datanamenode.InnerText;

                XmlNode idnode = sensor.SelectSingleNode("./*[local-name()='id']");
                String id = idnode.InnerText;

                String xmlfields = client.Custom("structure/" + dataname);
                doc.LoadXml(xmlfields);
                XmlNode sensortypenode = doc.DocumentElement;

                SelectListItem field = new SelectListItem();
                field.Text = sensortypenode.Name;
                field.Value = id;
                fields.Add(field);
            }

            return fields;
        }

        [HttpPost]
        public void CreateSensorNode(int installationID, String name, String location, String zigbeeAddress, String description)
        {
            XDocument postdata = new XDocument(
                new XDeclaration("1.0", "utf-16", null),
                new XElement("SensorGroup",
                    new XElement("start", "2013-01-01T00:00:00"),
                    new XElement("end", "9999-12-31T23:59:59"),
                    new XElement("installationid", installationID),
                    new XElement("id", "0"),
                    new XElement("name", name),
                    new XElement("description", description),
                    new XElement("inuse", "False"),
                    new XElement("infoname", ""),
                    new XElement("location", location)
                )
            );

            IpsumLogin();

            String response = client.Custom("addGroup/{token}/{code}", postdata.Declaration.ToString() + postdata.ToString());

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(response);
            XmlNode idNode = doc.DocumentElement.SelectSingleNode("//*[local-name()='id']");
            String sensorGroupID = idNode.InnerText;

            postdata = new XDocument(
                new XElement("addNode",
                    new XElement("installationID", installationID),
                    new XElement("sensorGroupID", sensorGroupID),
                    new XElement("zigbeeAddress", zigbeeAddress)
                )
            );

            /*response = ContactWebService("/addNode/", postdata.ToString());*/
        }

        [HttpPost]
        public void CreateSensor(int installationID, int sensorgroupID, String name, int frequency, String description, String dataname, String sensorType)
        {
            XDocument postdata = new XDocument(
                new XDeclaration("1.0", "utf-16", "yes"),
                new XElement("Sensor",
                    new XElement("sensorgroupid", sensorgroupID),
                    new XElement("frequency", frequency),
                    new XElement("dataname", dataname),
                    new XElement("id", "0"),
                    new XElement("name", name),
                    new XElement("description", description),
                    new XElement("inuse", "False"),
                    new XElement("infoname", ""),
                    new XElement("location", "")
                )
            );

            IpsumLogin();

            String response = client.Custom("addSensor/{token}/{code}", postdata.ToString());

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(response);
            XmlNode idNode = doc.DocumentElement.SelectSingleNode("//*[local-name()='id']");
            String sensorID = idNode.InnerText;

            Destination sensorDest = new Destination(21, installationID, sensorgroupID, Convert.ToInt32(sensorID));
            response = client.LoadDestination(sensorDest);

            client.Custom("setUser/21:255/{token}/{code}", response);

            postdata = new XDocument(
                new XElement("addSensor",
                    new XElement("sensorGroupID", sensorgroupID),
                    new XElement("sensor",
                        new XElement("sensorID", sensorID),
                        new XElement("sensorType", sensorType)
                    )
                )
            );

            /*response = ContactWebService("/addSensor/", postdata.ToString());*/
        }

        [HttpPost]
        public void CreateSensorType(String name, String field)
        {
            XDocument postdata = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("usertype",
                    new XAttribute("name", name),
                    new XElement("field",
                        new XElement("sql",
                            new XElement("type", name),
                            new XElement("l1", name),
                            new XElement("l2", name)
                        ),
                        new XElement("name", name),
                        new XElement("type", name)
                    )
                )
            );
        }

        public JsonResult FetchSpecificData(int installationID, int sensorGroupID, int sensorID, String field, String start, String end)
        {
            String startdatetime = start.Replace(" ", "T");
            String enddatetime = end.Replace(" ", "T");

            XDocument postdata = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("get",
                    new XElement("start", start),
                    new XElement("end", end),
                    new XElement("select",
                        new XElement("field",
                            new XElement("function", ""),
                            new XElement("name", field)
                        ),
                        new XElement("field",
                            new XElement("function", ""),
                            new XElement("name", "timestamp")
                        )
                    )
                )
            );

            Destination destination = new Destination(21, installationID, sensorGroupID, sensorID);
            IpsumLogin();

            String xmldata = "";
            Dictionary<String, String> datalist = new Dictionary<String, String>();
            try
            {
                xmldata = client.Calculation(destination, postdata.ToString());
            }
            catch (WebException e)
            {
                JsonResult json = Json(datalist, JsonRequestBehavior.AllowGet);
                return json;
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmldata);
            XmlNodeList entries = doc.DocumentElement.ChildNodes;

            foreach (XmlNode entry in entries)
            {
                XmlNode fieldvaluenode = entry.SelectSingleNode("./*[local-name()='" + field + "']");
                XmlNode timestampnode = entry.SelectSingleNode("./*[local-name()='timestamp']");
                if(fieldvaluenode != null)
                {
                    try
                    {
                        datalist.Add(timestampnode.InnerText, fieldvaluenode.InnerText);
                    }
                    catch (ArgumentException) { /* timestamp already exists, don't add a new one with the same timestamp */};
                }
            }

            JsonResult result = Json(datalist, JsonRequestBehavior.AllowGet);
            return result;
        }

        public void FetchDirectSensorData(int sensorGroupID, int sensorID)
        {
            XDocument postdata = new XDocument(
                new XElement("requestData",
                    new XElement("sensorGroupID", sensorGroupID),
                    new XElement("sensor", sensorID)
                )
            );

            /*String response = ContactWebService("/requestData/", postdata.ToString());*/
        }

        public void ChangeFrequency(int sensorGroupID, int sensorID, int newFrequency)
        {
            XDocument postdata = new XDocument(
                new XElement("changeFrequency",
                    new XElement("sensorGroupID", sensorGroupID),
                    new XElement("sensor",
                        new XElement("sensorID", sensorID),
                        new XElement("frequency", newFrequency)
                    )
                )
            );

            /*String response = ContactWebService("/changeFrequency/", postdata.ToString());*/
        }

        public String ContactWebService(String url, String xml = null)
        {
            // Create the web request  
            HttpWebRequest request = WebRequest.Create("http://10.65.101.200:8080" + url) as HttpWebRequest;
            request.Timeout = 20000;
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
            String resp = "";
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                // Get the response stream  
                StreamReader reader = new StreamReader(response.GetResponseStream());
                resp = reader.ReadToEnd();
                reader.Dispose();
            }
            return resp;
        }

        public class IpsumSensorNode
        {
            public int id { get; set; }
            public String name { get; set; }
            public String location { get; set; }
            public String description { get; set; }
            public Boolean inuse { get; set; }
            public List<IpsumSensor> sensors { get; set; }
        }

        public class IpsumSensor
        {
            public int id { get; set; }
            public String name { get; set; }
            public String description { get; set; }
            public int frequency { get; set; }
            public String type { get; set; }
            public String lastvalue { get; set; }
            public String timestamp { get; set; }
        }
    }
}
