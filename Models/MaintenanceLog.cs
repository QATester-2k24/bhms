using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BHMS_Portal.Models
{
    public class MaintenanceLog
    {
        public int MaintenanceLogId { get; set; }
        public int BookingId { get; set; }
        public string ItemDescription { get; set; }
        public decimal Cost { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
