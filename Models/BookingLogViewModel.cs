using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BHMS_Portal.Models
{
    public class BookingLogViewModel
    {
        public int BookingId { get; set; }
        public int UserId { get; set; }
        public int HutId { get; set; }
        public string HutName { get; set; }

        public DateTime BookingDate { get; set; }
        public string BookingStatus { get; set; }
        public string UserFullName { get; set; }
        public string UserDecibelId { get; set; }
        public string UserEmail { get; set; }
        public string UserDepartment { get; set; }
        public string UserGrade { get; set; }
        public string UserDesignation { get; set; }
        public string UserMobileNo { get; set; }
        public string UserCNIC { get; set; }
        public string NomineeName { get; set; }
        public bool? IsAdminApproved { get; set; }
        public string AdminComments { get; set; }
        public bool? IsHODApproved { get; set; }
        public string HODComments { get; set; }
        public int? NomineeDetailsId { get; set; }
        public string NomineeFullName { get; set; }
        public string NomineeEmail { get; set; }
        public string NomineeDepartment { get; set; }
        public string NomineeGrade { get; set; }
        public string NomineeDesignation { get; set; }
        public string NomineeMobileNo { get; set; }
        public string NomineeCNIC { get; set; }
        public string HeadOfDepartment { get; set; }
        public string HeadOfDepartmentId { get; set; }
        public string HeadOfDepartmentEmail { get; set; }
        public int? TotalPersonsForBooking { get; set; }
        public decimal? BookingCostOfHut { get; set; }
        public bool? BookingHasAdditionalRequirements { get; set; }
        public string BookingAdditionalRequirements { get; set; }
        public DateTime? BookingCancellationDate { get; set; }
        public bool? CancelledByAdmin { get; set; }
        public bool? CancelledByHOD { get; set; }
        public decimal? PenaltyAmount { get; set; }
        public decimal? TotalMaintenanceCost { get; set; }
        public bool? IsReturned { get; set; }
    }

}
