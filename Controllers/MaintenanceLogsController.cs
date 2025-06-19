using BHMS_Portal.DataAccess;
using BHMS_Portal.Models;
using Dapper;
using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using BHMS_Portal.Utility;
using System.Threading.Tasks;
using System.Data;
using System.Globalization;
using System.Configuration;
using System.IO;


namespace BHMS_Portal.Controllers
{
    [Authorize]
    public class MaintenanceLogsController : BaseController
    {
        private readonly DapperContext _dapperContext;

        // Parameterless constructor required by MVC
        public MaintenanceLogsController()
        {
            _dapperContext = new DapperContext();
        }

        // Optional constructor for DI (if you configure DI later)
        public MaintenanceLogsController(DapperContext dapperContext)
        {
            _dapperContext = dapperContext;
        }



        // FeedbackViewModel
        public ActionResult FeedbackForm(int bookingId, string key)
        {
            var model = new FeedbackViewModel
            {
                BookingId = bookingId,
                FeedbackKey = key
            };
            return PartialView("~/Views/Feedback/_FeedbackForm.cshtml", model);
        }

        // GET: Maintenance Logs screen
        public ActionResult Index()
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            if (session == null || !session.IsAdmin)
                return RedirectToAction("Index", "BHMS");

            return View();
        }

        // API: Get past bookings eligible for maintenance logs (date passed, status Booked, approved)
        [HttpGet]
        public JsonResult GetPastBookings()
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            if (session == null || !session.IsAdmin)
                return Json(new { success = false, message = "Unauthorized" }, JsonRequestBehavior.AllowGet);

            using (var conn = _dapperContext.CreateConnection())
            {
                var today = DateTime.Today;
                var sql = @"
SELECT b.BookingId, b.BookingDate, u.FullName AS UserName, u.DecibelId, 
       b.HutId, h.HutName,
       b.CostOfHut, b.NomineeName,
       ISNULL(SUM(ml.Cost), 0) AS TotalMaintenanceCost
FROM Bookings b
JOIN Users u ON b.UserId = u.Id
JOIN Huts h ON b.HutId = h.HutId
LEFT JOIN MaintenanceLogs ml ON ml.BookingId = b.BookingId
WHERE b.BookingStatus = 'Booked' 
  AND (b.IsApproved = 1 OR b.IsHODApproved = 1)
  AND b.BookingDate < @Today
GROUP BY b.BookingId, b.BookingDate, u.FullName, u.DecibelId, b.HutId, h.HutName, b.CostOfHut, b.NomineeName
ORDER BY b.BookingDate DESC";

                var bookings = conn.Query(sql, new { Today = today }).ToList();
                return Json(bookings, JsonRequestBehavior.AllowGet);
            }
        }


        // API: Get maintenance items for a booking
        [HttpGet]
        public JsonResult GetMaintenanceItems(int bookingId)
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            if (session == null || !session.IsAdmin)
                return Json(new { success = false, message = "Unauthorized" }, JsonRequestBehavior.AllowGet);

            using (var conn = _dapperContext.CreateConnection())
            {
                var sql = @"SELECT MaintenanceLogId, ItemDescription, Cost FROM MaintenanceLogs WHERE BookingId = @BookingId";
                var items = conn.Query(sql, new { BookingId = bookingId }).ToList();
                return Json(items, JsonRequestBehavior.AllowGet);
            }
        }

        // API: Save maintenance items (bulk insert/update)
        [HttpPost]
        public JsonResult SaveMaintenanceItems(int bookingId, List<MaintenanceLog> items)
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            if (session == null || !session.IsAdmin)
                return Json(new { success = false, message = "Unauthorized" });

            using (var conn = _dapperContext.CreateConnection())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // Delete existing items
                        conn.Execute("DELETE FROM MaintenanceLogs WHERE BookingId = @BookingId", new { BookingId = bookingId }, tran);

                        // Insert new items
                        foreach (var item in items)
                        {
                            conn.Execute(@"INSERT INTO MaintenanceLogs (BookingId, ItemDescription, Cost, CreatedAt, UpdatedAt)
                                       VALUES (@BookingId, @ItemDescription, @Cost, GETDATE(), GETDATE())",
                                         new { BookingId = bookingId, item.ItemDescription, item.Cost }, tran);
                        }

                        // Update total maintenance cost
                        decimal totalCost = items.Sum(i => i.Cost);
                        conn.Execute("UPDATE Bookings SET TotalMaintenanceCost = @TotalCost WHERE BookingId = @BookingId",
                                     new { TotalCost = totalCost, BookingId = bookingId }, tran);

                        tran.Commit();
                        return Json(new { success = true });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return Json(new { success = false, message = ex.Message });
                    }
                }
            }
        }

        //Mark As Returned Method

        [HttpPost]
        public async Task<JsonResult> MarkReturned(int bookingId)
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            if (session == null || !session.IsAdmin)
                return Json(new { success = false, message = "Unauthorized" });

            using (var conn = _dapperContext.CreateConnection())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // Update IsReturned flag
                        string updateSql = "UPDATE Bookings SET IsReturned = 1 WHERE BookingId = @BookingId";
                        conn.Execute(updateSql, new { BookingId = bookingId }, tran);

                        // Generate feedback key
                        Guid feedbackKey = Guid.NewGuid();

                        // Upsert feedback key
                        string upsertSql = @"
                            MERGE BookingFeedbackKeys AS target
                            USING (SELECT @BookingId AS BookingId) AS source
                            ON (target.BookingId = source.BookingId)
                            WHEN MATCHED THEN 
                                UPDATE SET FeedbackKey = @FeedbackKey, IsUsed = 0, CreatedAt = GETDATE()
                            WHEN NOT MATCHED THEN
                                INSERT (BookingId, FeedbackKey, IsUsed, CreatedAt)
                                VALUES (@BookingId, @FeedbackKey, 0, GETDATE());";

                        conn.Execute(upsertSql, new { BookingId = bookingId, FeedbackKey = feedbackKey }, tran);

                        // Get user email and booking info
                        var bookingInfo = conn.QueryFirstOrDefault<(string PrimaryEmail, string FullName, DateTime BookingDate, string HutName)>(
                        @"SELECT u.PrimaryEmail, u.FullName, b.BookingDate, h.HutName
                        FROM Bookings b 
                        JOIN Users u ON b.UserId = u.Id 
                        JOIN Huts h ON b.HutId = h.HutId
                        WHERE b.BookingId = @BookingId",
                       new { BookingId = bookingId }, tran);

                        if (string.IsNullOrEmpty(bookingInfo.PrimaryEmail))
                            throw new Exception("User email not found.");

                        // Prepare feedback link
                        // Use your local development URL here
                        string baseUrl = ConfigurationManager.AppSettings["AppBaseUrl"];
                        string feedbackUrl = $"{baseUrl}/Feedback/Submit?bookingId={bookingId}&key={feedbackKey}";


                        // Get email template and fill placeholders
                        string template = Utility.Utility.GetEmailTemplate("FeedbackRequest");
                        var values = new Dictionary<string, string>
                        {
                            { "UserName", bookingInfo.FullName },
                            { "BookingDate", bookingInfo.BookingDate.ToString("yyyy-MM-dd") },
                            { "HutName", bookingInfo.HutName },
                            { "FeedbackLink", feedbackUrl }
                        };
                        string emailBody = Utility.Utility.FillEmailTemplate(template, values);

                        // Send email with try-catch for logging
                        try
                        {
                            bool emailSent = await Utility.Utility.SendEmail(bookingInfo.PrimaryEmail, "Please provide your feedback", emailBody);
                            if (!emailSent)
                                throw new Exception("Failed to send feedback email.");
                        }
                        catch (Exception emailEx)
                        {
                            // Attempt to log error safely
                            try
                            {
                                string logPath = @"C:\Logs\EmailErrors.log";
                                string logDir = Path.GetDirectoryName(logPath);
                                if (!Directory.Exists(logDir))
                                    Directory.CreateDirectory(logDir);

                                System.IO.File.AppendAllText(logPath, $"{DateTime.Now}: {emailEx}\n");
                            }
                            catch
                            {
                                // Swallow logging errors to avoid crash
                            }

                            // Return friendly error message to client
                            return Json(new { success = false, message = "An error occurred while sending the feedback email. Please try again later." });
                        }

                        tran.Commit();
                        return Json(new { success = true, message = "User marked as returned and feedback email sent." });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return Json(new { success = false, message = "An unexpected error occurred. Please try again later." });
                    }
                }
            }
        }



    }
}
