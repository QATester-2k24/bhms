using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BHMS_Portal.Models
{
    public class EmailNotificationType
    {
        public int Id { get; set; }
        public string EmailType { get; set; }
        public string EmailTemplate { get; set; }
        
    }
}
