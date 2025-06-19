using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BHMS_Portal.Models
{
    public class UserDetailsModel
    {
        public int UserDetailsId { get; set; }
        public int UserId { get; set; }
        public string DecibelId { get; set; }
        public string FullName { get; set; }
        public string PrimaryEmail { get; set; }
        public string DepartmentName { get; set; }
        public string Grade { get; set; }
        public string Designation { get; set; }
        public string MobileNo { get; set; }
        public string CNIC { get; set; }
        public int NoOfPersons { get; set; }
        public DateTime CreatedOn { get; set; }
        public decimal CostOfHut { get; set; }
        public bool HasAdditionalRequirements { get; set; }
        public string AdditionalRequirements { get; set; }
    }

}
