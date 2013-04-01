using System;
using System.Data.Entity;

namespace Thesis.Models
{
    public class Sensor
    {
        public int ID { get; set; }
        public string Type { get; set; }
        public decimal Value { get; set; }
        public decimal BatteryLevel { get; set; }
        public int XPos { get; set; }
        public int YPos { get; set; }
    }

    public class SensorDBContext : DbContext
    {
        public DbSet<Sensor> Sensors { get; set; }
    }
}