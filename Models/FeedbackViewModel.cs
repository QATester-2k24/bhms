using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace BHMS_Portal.Models
{
    public class FeedbackViewModel
    {
        public int BookingId { get; set; }
        public string FeedbackKey { get; set; }

        [Range(1, 5, ErrorMessage = "Please rate between 1 and 5")]
        [Display(Name = "Hut General Condition")]
        public int HutConditionRating { get; set; }

        [Range(1, 5)]
        [Display(Name = "Hygiene / Cleanliness")]
        public int HygieneRating { get; set; }

        [Range(1, 5)]
        [Display(Name = "Staff Rating")]
        public int StaffRating { get; set; }

        [Range(1, 5)]
        [Display(Name = "Items of Use")]
        public int ItemsRating { get; set; }

        [MaxLength(500, ErrorMessage = "Maximum 500 characters allowed")]
        [Display(Name = "Additional Comments / Suggestions")]
        public string Comments { get; set; }
    }
}
