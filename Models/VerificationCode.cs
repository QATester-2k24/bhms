using System;

namespace BHMS_Portal.Models
{
    public class VerificationCode
    {
        public int ID { get; set; }
        public string DecibelId { get; set; }
        public string Code { get; set; }
        public string VerificationType { get; set; }
        public bool? IsVerified { get; set; }
        public DateTime? VerifiedOn { get; set; }
        public int? VerifiedBy { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
