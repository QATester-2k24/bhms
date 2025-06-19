using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace BHMS_Portal.Models
{
    public class ResponseModel<T>
    {
        public T data { get; set; }
        public string statusMessage { get; set; }
        public string message { get; set; }
        public HttpStatusCode statusCode { get; set; }
    }

}