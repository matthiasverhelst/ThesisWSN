using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Web;

namespace Thesis.Models
{
    public class NetworkContext : DbContext
    {
        public NetworkContext()
            : base("DefaultConnection")
        {
        }

        public DbSet<Network> networks { get; set; }
    }

    public class InstallationContext : DbContext
    {
        public InstallationContext()
            : base("DefaultConnection")
        {
        }

        public DbSet<Installation> installations { get; set; }
    }

    [Table("Network")]
    public class Network
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public string name { get; set; }
    }

    [Table("Installation")]
    public class Installation
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public string name { get; set; }
        public int networkID { get; set; }
        public string groundplanURL { get; set; }
    }
}