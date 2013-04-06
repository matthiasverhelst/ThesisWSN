using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;
using Thesis.Ipsum;
using Thesis.Models;

namespace Thesis.Controllers
{
    public class SensorController : Controller
    {
        private SensorDBContext db = new SensorDBContext();

        //
        // GET: /Sensor/

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

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(Sensor sensor)
        {
            if (ModelState.IsValid)
            {
                db.Sensors.Add(sensor);
                db.SaveChanges();
                return RedirectToAction("ModuleDetails");
            }

            return View(sensor);
        }

        public ActionResult Edit(int id = 0)
        {
            Sensor sensor = db.Sensors.Find(id);
            if (sensor == null)
            {
                return HttpNotFound();
            }
            return View(sensor);
        }

        [HttpPost]
        public ActionResult Edit(Sensor sensor)
        {
            if (ModelState.IsValid)
            {
                db.Entry(sensor).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("ModuleDetails");
            }
            return View(sensor);
        }

        public ActionResult Delete(int id = 0)
        {
            Sensor sensor = db.Sensors.Find(id);
            if (sensor == null)
            {
                return HttpNotFound();
            }
            return View(sensor);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            Sensor sensor = db.Sensors.Find(id);
            db.Sensors.Remove(sensor);
            db.SaveChanges();
            return RedirectToAction("ModuleDetails");
        }

        public JsonResult FetchAllSensors(int installationID)
        {
            IpsumClient client = new IpsumClient("http://ipsum.groept.be", "/", "ad37d673-8803-4497-99dc-97f6baf91d5e");
            client.Authenticate("mverhelst", "mverhelst");

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

                Destination sensorgroupdest = new Destination(21, installationID, sensornode.id, 0);
                String xmlChildSensors = client.LoadChildren(sensorgroupdest);
                List<IpsumSensor> sensors = new List<IpsumSensor>();

                XmlDocument xmlreader2 = new XmlDocument();
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

                    sensor.field = new Dictionary<String, String>();

                    Destination sensordest = new Destination(21, installationID, sensornode.id, sensor.id);
                    String xmlsensorvalue = client.Calculation(sensordest);

                    XmlDocument xmlreader3 = new XmlDocument();
                    xmlreader3.LoadXml(xmlsensorvalue);

                    XmlNode entries = xmlreader3.DocumentElement.SelectSingleNode("//*[local-name()='entry']");
                    if (entries != null)
                    {
                        XmlNodeList values = entries.ChildNodes;

                        foreach (XmlNode value in values)
                        {
                            if (value != null)
                                sensor.field.Add(value.Name.First().ToString().ToUpper() + String.Join("", value.Name.Skip(1)), value.InnerText);
                        }

                        sensors.Add(sensor);
                    }
                }

                sensornode.sensors = sensors;
                sensornodes.Add(sensornode);
            }

            JsonResult json = Json(sensornodes, JsonRequestBehavior.AllowGet);
            return json;
        }

        public List<SelectListItem> FetchAllSensorTypes()
        {
            IpsumClient client = new IpsumClient("http://ipsum.groept.be", "/", "ad37d673-8803-4497-99dc-97f6baf91d5e");
            client.Authenticate("mverhelst", "mverhelst");

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
                XmlNodeList fields = type.ChildNodes;
                String fieldstring = "";
                foreach (XmlNode node in fields)
                {
                    if(node.Name != "timestamp" && node.Name != "utimestamp")
                        fieldstring += node.Name + ", ";
                }
                fieldstring = fieldstring.Remove(fieldstring.Length - 2);
                SelectListItem element = new SelectListItem();
                element.Text = type.Name + " (" + fieldstring + ")";
                element.Value = name.InnerText;
                elements.Add(element);
            }

            return elements;
        }

        public List<SelectListItem> FetchSensorNodeFields(int installationid, int sensorgroupid)
        {
            Destination sensornodedest = new Destination(21, installationid, sensorgroupid, 0);
            IpsumClient client = new IpsumClient("http://ipsum.groept.be", "/", "ad37d673-8803-4497-99dc-97f6baf91d5e");
            client.Authenticate("mverhelst", "mverhelst");
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
                XmlNodeList fieldnodes = doc.DocumentElement.ChildNodes;

                foreach (XmlNode fieldnode in fieldnodes)
                {
                    if (fieldnode.Name != "timestamp" && fieldnode.Name != "utimestamp")
                    {
                        SelectListItem field = new SelectListItem();
                        field.Text = fieldnode.Name;
                        field.Value = id + "," + fieldnode.Name;
                        fields.Add(field);
                    }
                }
            }

            return fields;
        }

        [HttpPost]
        public void CreateSensorNode(int installationID, String name, String location, String description)
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
                    new XElement("inuse", "True"),
                    new XElement("infoname", ""),
                    new XElement("location", location)
                )
            );

            IpsumClient client = new IpsumClient("http://ipsum.groept.be", "/", "ad37d673-8803-4497-99dc-97f6baf91d5e");
            client.Authenticate("mverhelst", "mverhelst");

            client.Custom("addGroup/{token}/{code}", postdata.Declaration.ToString() + postdata.ToString());
        }

        [HttpPost]
        public void CreateSensor(int sensorgroupID, String name, int frequency, String description, String dataname)
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
                    new XElement("inuse", "True"),
                    new XElement("infoname", ""),
                    new XElement("location", "")
                )
            );

            IpsumClient client = new IpsumClient("http://ipsum.groept.be", "/", "ad37d673-8803-4497-99dc-97f6baf91d5e");
            client.Authenticate("mverhelst", "mverhelst");

            client.Custom("addSensor/{token}/{code}", postdata.ToString());
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
            IpsumClient client = new IpsumClient("http://ipsum.groept.be", "/", "ad37d673-8803-4497-99dc-97f6baf91d5e");
            client.Authenticate("mverhelst", "mverhelst");

            String xmldata = client.Calculation(destination, postdata.ToString());

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmldata);
            XmlNodeList entries = doc.DocumentElement.ChildNodes;

            Dictionary<String, String> datalist = new Dictionary<String, String>();

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

            JsonResult json = Json(datalist, JsonRequestBehavior.AllowGet);
            return json;
        }

        public class IpsumSensorNode
        {
            public int id { get; set; }
            public String name { get; set; }
            public String location { get; set; }
            public String description { get; set; }
            public List<IpsumSensor> sensors {get; set; }
        }

        public class IpsumSensor
        {
            public int id { get; set; }
            public String name { get; set; }
            public String description { get; set; }
            public int frequency { get; set; }
            public Dictionary<String, String> field {get; set; }
        }
    }
}
