using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public class EmailDataModel
{
    public string EmailToSend { get; set; }
    public string DisplayName { get; set; }
    public string SenderName { get; set; }
    public string VerificationCode { get; set; }
    public string VerifyLink { get; set; }
}