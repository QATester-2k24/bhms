
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using BHMS_Portal.DecibelAPI;
using System.Data.SqlClient;
using System.Configuration;



namespace BHMS_Portal.Utility
{
    public static class Utility
    {
        public static async Task<DataSet> DecibelAuthentication(string decibelId)
        {
            DecibelAPI.getRequestList[] com = new DecibelAPI.getRequestList[3];
            com[0] = new DecibelAPI.getRequestList() { name = "P_EMP_ID", value = decibelId, direction = DecibelAPI.ParameterDirection.Input, p_type = DecibelAPI.OracleDbType.Varchar2 };
            com[1] = new DecibelAPI.getRequestList() { name = "P_OPT", value = "1", direction = DecibelAPI.ParameterDirection.Input, p_type = DecibelAPI.OracleDbType.Varchar2 };
            com[2] = new DecibelAPI.getRequestList() { name = "P_EMP_RECORDSET", direction = DecibelAPI.ParameterDirection.Output, p_type = DecibelAPI.OracleDbType.RefCursor };
            DecibelAPI.getDataSoap SD = new DecibelAPI.getDataSoap();
            DataSet ds = SD.callProcedure("ISF_Decibel", "TMS_DEC_EMP_DETAIL", com);
            return ds;
        }

        // Read from config whether to enable email sending
        public static bool EnableEmailSending
        {
            get
            {
                bool enabled = true;
                var configValue = ConfigurationManager.AppSettings["EnableEmailSending"];
                if (!string.IsNullOrEmpty(configValue))
                {
                    bool.TryParse(configValue, out enabled);
                }
                return enabled;
            }
        }

        public async static Task<bool> SendEmail(string ToEmail, string Subject, string EmailTemplate)
        {

            if (!EnableEmailSending)
            {
                string logMsg = $"{DateTime.Now}: Email sending skipped to {ToEmail}{Environment.NewLine}";
                System.IO.File.AppendAllText(@"C:\Logs\EmailSkipped.log", logMsg);
                System.Diagnostics.Debug.WriteLine(logMsg); // Also output to debug window
                return true;
            }


            var requestBody = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
            {
                Message = new Message
                {
                    Subject = Subject,
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Html,
                        Content = EmailTemplate
                    },
                    ToRecipients = new List<Recipient>{
                        new Recipient{
                            EmailAddress = new Microsoft.Graph.Models.EmailAddress{
                                Address = ToEmail
                            }
                        }
                    },
                    BccRecipients = new List<Recipient>{
                        //new Recipient {
                            //EmailAddress = new Microsoft.Graph.Models.EmailAddress{
                                //Address = "@jubileelife.com"
                            //}
                        //},
                        new Recipient {
                            EmailAddress = new Microsoft.Graph.Models.EmailAddress{
                                Address = "bismah.aamir@jubileelife.com"
                            }
                        }
                    }
                }
            };

            var scopes = new[] { "Users.SendMail" };

            string tenantId = "39c4038a-4151-4e79-bb31-252f3e24ccf8";
            string clientId = "0a637ea9-0a93-4d43-91e6-4123e258089c";
            string secretId = "r_~8Q~x-mT7c-hvGjmQlahSB0TWlxApc5_5qLb4k";

            ClientSecretCredential clientCredential = new ClientSecretCredential(tenantId, clientId, secretId);

            var graphClient = new GraphServiceClient(clientCredential);

            try
            {
                //Console.WriteLine("Sending");
                await graphClient.Users["noreply@jubileelife.com"].SendMail.PostAsync(requestBody);
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception message to a file or monitoring system
                System.IO.File.AppendAllText(@"C:\Logs\EmailErrors.log", $"{DateTime.Now}: {ex.Message}{Environment.NewLine}");
                return false;
            }

            return true;

        }

        // NEW: Get email template from database
        public static string GetEmailTemplate(string emailType)
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT EmailTemplate FROM EmailNotificationTypes WHERE EmailType = @type", conn);
                cmd.Parameters.AddWithValue("@type", emailType);
                return cmd.ExecuteScalar()?.ToString();
            }
        }

        // NEW: Replace placeholders in template
        public static string FillEmailTemplate(string template, Dictionary<string, string> values)
        {
            foreach (var kvp in values)
            {
                template = template.Replace("{" + kvp.Key + "}", kvp.Value ?? "");
            }
            return template;
        }



    }
}