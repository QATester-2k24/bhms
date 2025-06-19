using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BHMS_Portal.Models
{
    public class BookingRequestModel
    {
        public DateTime Date { get; set; }
        public int HutId { get; set; }
        public string BookingFor { get; set; } // "Myself" or "Nominee"
        public UserDetailsModel UserDetails { get; set; }
        public NomineeDetailsModel NomineeDetails { get; set; }
    }

}
