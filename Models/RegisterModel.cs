using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BHMS_Portal.Models
{
    public class RegisterModel
    {
        public string DecibelId { get; set; }
        public string Password { get; set; }
        public int UserTypeId { get; set; }
        public string UserType { get; set; }
        public string VerificationType { get; set; }
    }
}