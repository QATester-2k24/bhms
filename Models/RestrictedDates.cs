using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BHMS_Portal.Models
{
    public class RestrictedDate
    {
        public int Id { get; set; }
        public int HutId { get; set; }
        public DateTime RestrictDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
    }

}
