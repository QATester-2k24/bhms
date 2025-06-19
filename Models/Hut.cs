using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BHMS_Portal.Models
{
    public class Hut
    {
        public int HutId { get; set; }
        public string HutName { get; set; }
        public string HutType { get; set; } // Staff or Executive
        public decimal CostOfHut { get; set; }

      //  public decimal Cost { get; set; }

        public bool IsActive { get; set; }

    }
}
