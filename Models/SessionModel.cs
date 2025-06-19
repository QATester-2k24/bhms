using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BHMS_Portal.Models
{
    public class SessionModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public int UserTypeId { get; set; }

        public String UserType { get; set; }

        public string Email { get; set; }
    
        public bool IsAdmin { get; set; }
        public bool IsAgreedTerms { get; set; }
        public string SessionId { get; set; }

        public string DecibelId { get; set; }
    }

}