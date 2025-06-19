using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BHMS_Portal.Models
{
    public class User
    {
        public int Id { get; set; }
        public string DecibelId { get; set; }
        public string FullName { get; set; }
        public string PrimaryEmail { get; set; }
        public string SecondaryEmail { get; set; }
        public string Password { get; set; }
        public string DepartmentName { get; set; }

         public string Grade { get; set; }

        public string Manager_Code { get; set; }

        public string Manager_Name { get; set; }

        public string Manager_Email { get; set; }

        public string Designation { get; set; }
        public string MobileNo { get; set; }
        public string UserType { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool IsAgreedTerms { get; set; }
        public bool? IsAdmin { get; set; }

         public int UserTypeId { get; set; }


    }
    
   }
