using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Web;

namespace Thesis.Ipsum
{
    /// <summary>
    /// Object to address a specific destination
    /// </summary>
    public class Destination
    {
        private uint _user, _installation, _sensorGroup, _sensor;
        private long _timestamp;
        private string _checksum;

        /// <summary>
        /// Convert a destination string to a new object.
        /// Can throw an InvalidCastException if the string is not well formed
        /// Can throw an UnauthorizedAccessException if the checksum is invalid.
        /// </summary>
        /// <param name="destination">String to parse</param>
        public Destination(string destination)
        {

            destination = ASCIIEncoding.ASCII.GetString(Convert.FromBase64String(destination));
            string[] split = destination.Split(':');

            try
            {
                _user = UInt32.Parse(split[0]);
                if (split.Length - 2 >= 2)
                    _installation = UInt32.Parse(split[1]);
                if (split.Length - 2 >= 3)
                    _sensorGroup = UInt32.Parse(split[2]);
                if (split.Length - 2 >= 4)
                    _sensor = UInt32.Parse(split[3]);
            }
            catch
            {
                throw new InvalidCastException();
            }

            _timestamp = UInt32.Parse(split[split.Length - 2]);

            check();

            if (split[split.Length - 1] != _checksum)
                throw new UnauthorizedAccessException("Invalid checksum");
        }

        /// <summary>
        /// Generate a new destination object
        /// </summary>
        /// <param name="user">User id</param>
        /// <param name="installation">Installation id</param>
        /// <param name="sensorGroup">SensorGroup id</param>
        /// <param name="sensor">Sensor id</param>
        public Destination(int user, int installation, int sensorGroup, int sensor)
        {
            _user = (uint)user;
            _installation = (uint)installation;
            _sensorGroup = (uint)sensorGroup;
            _sensor = (uint)sensor;

            _timestamp = DTToTimestamp(DateTime.UtcNow);

            check();
        }

        /// <summary>
        /// Generate checksum
        /// </summary>
        private void check()
        {
            long l = User + Installation + SensorGroup + Sensor + _timestamp;
            string s = l.ToString("000000");
            s = s.Substring(s.Length - 6);
            _checksum = s;
        }

        /// <summary>
        /// Convert to String
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string ret = User.ToString() + ":";
            if (Installation != 0)
            {
                ret += Installation.ToString() + ":";
                if (SensorGroup != 0)
                {
                    ret += SensorGroup.ToString() + ":";
                    if (Sensor != 0)
                        ret += Sensor.ToString();
                }
            }
            return Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(ret.TrimEnd(':') + ":" + _timestamp + ":" + Checksum));
        }

        public virtual int Sensor { get { return (int)_sensor; } }
        public virtual int SensorGroup { get { return (int)_sensorGroup; } }
        public virtual int Installation { get { return (int)_installation; } }
        public virtual int User { get { return (int)_user; } }
        public virtual string Checksum { get { return _checksum; } }
        public virtual DateTime TimeStamp
        {
            get
            {
                return DTFromTimestamp(_timestamp);

            }
        }

        public static DateTime DTFromTimestamp(long _timestamp)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(_timestamp).ToLocalTime();
            return dtDateTime;
        }

        public static long DTToTimestamp(DateTime dt)
        {
            return (dt.ToUniversalTime().Ticks - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).Ticks) / TimeSpan.TicksPerSecond;
        }

    }
}
