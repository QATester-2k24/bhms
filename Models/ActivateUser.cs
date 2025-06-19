using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BHMS_Portal.Models
{ 
    public class ActivateUser
    {
        public int UserId { get; set; }
        public bool IsDeleted { get; set; }
        public string UserTypeId { get; set; }
    }
}