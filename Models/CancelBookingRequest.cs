using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BHMS_Portal.Models
{
    public class CancelBookingRequest
    {
        public int BookingId { get; set; }
        public int UserId { get; set; }
    }
}
