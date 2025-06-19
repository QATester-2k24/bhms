using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BHMS_Portal.Models
{
    public class Booking
    {
        public int BookingId { get; set; }
        public int UserId { get; set; }
        public int HutId { get; set; }
        public DateTime BookingDate { get; set; }
        public string BookingStatus { get; set; }
        public string NomineeName { get; set; }
        public DateTime CreatedOn { get; set; }
        public string PermissionLetterPath { get; set; }

        public DateTime? CancellationDate { get; set; }
        public decimal? PenaltyAmount { get; set; }

        public decimal CostOfHut { get; set; }

        public bool IsApproved { get; set; }
        public string AdminComments { get; set; }
        public string AuthorizationLetterPath { get; set; }
        public bool CancelledByAdmin { get; set; }

        public bool? IsHODApproved { get; set; }
        public string HODComments { get; set; }
        public bool? CancelledByHOD { get; set; }
        public string HODDecibelId { get; set; }

        public decimal TotalMaintenanceCost { get; set; }
        public bool IsReturned { get; set; }

    }
}
