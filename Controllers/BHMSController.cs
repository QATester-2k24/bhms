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
using System.Data.SqlClient;
using System.Configuration;
using System.Globalization;

namespace BHMS_Portal.Controllers
{
    [Authorize]
    public class BHMSController : BaseController
    {
        private readonly DapperContext _dapperContext = new DapperContext();

        private bool IsAdminUser()
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            return session != null && session.IsAdmin;
        }

        public ActionResult Index(int? hutId)
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            ViewBag.UserType = session.UserType;
            ViewBag.UserId = session.UserId;
            ViewBag.IsAdmin = session.IsAdmin;
            ViewBag.FullName = session.FullName;



            // ADDED THIS FOR HOD PENDING APPROVAL SESSION 
            ViewBag.IsHoD = IsCurrentUserHoD(session.DecibelId);

            using (var conn = _dapperContext.CreateConnection())
            {
                // Fetch active huts for Staff and Executive
                var staffHut = conn.QueryFirstOrDefault<Hut>("SELECT TOP 1 * FROM Huts WHERE HutType = 'Staff' AND IsActive = 1 ORDER BY HutId");
                var executiveHut = conn.QueryFirstOrDefault<Hut>("SELECT TOP 1 * FROM Huts WHERE HutType = 'Executive' AND IsActive = 1 ORDER BY HutId");

                int staffHutId = staffHut?.HutId ?? 0;
                int executiveHutId = executiveHut?.HutId ?? 0;

                ViewBag.StaffHutId = staffHut?.HutId ?? 0;
                ViewBag.StaffHutName = staffHut?.HutName ?? "Staff Hut";
                ViewBag.ExecutiveHutId = executiveHut?.HutId ?? 0;
                ViewBag.ExecutiveHutName = executiveHut?.HutName ?? "Executive Hut";

                // Set selected hut ID based on logic (userType, hutId param)
                int selectedHutId;
                if (hutId.HasValue && (hutId == staffHut?.HutId || hutId == executiveHut?.HutId))
                    selectedHutId = hutId.Value;
                else
                    selectedHutId = session.UserType == "Executive" ? (executiveHut?.HutId ?? 0) : (staffHut?.HutId ?? 0);

                ViewBag.SelectedHutId = selectedHutId;
                ViewBag.HutName = selectedHutId == staffHut?.HutId ? staffHut?.HutName : executiveHut?.HutName;


                // Fetch all huts and pass to view for JS
                var huts = conn.Query<Hut>("SELECT HutId, HutName, HutType, CostOfHut FROM Huts WHERE IsActive = 1").ToList();
                ViewBag.Huts = huts;

            }

            return View("Index");
        }

        // Pending Approval For ADMIN ONLY
        public ActionResult PendingApprovals()
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            if (session == null || !session.IsAdmin)
                return RedirectToAction("Index", "BHMS");

            ViewBag.UserType = session.UserType;
            ViewBag.UserId = session.UserId;
            ViewBag.IsAdmin = session.IsAdmin;
            ViewBag.FullName = session.FullName;
            ViewBag.ShowPendingApprovals = true;
            ViewBag.ShowHoDPendingApprovals = false;

            using (var conn = _dapperContext.CreateConnection())
            {
                var staffHut = conn.QueryFirstOrDefault<Hut>("SELECT TOP 1 * FROM Huts WHERE HutType = 'Staff' AND IsActive = 1 ORDER BY HutId");
                var executiveHut = conn.QueryFirstOrDefault<Hut>("SELECT TOP 1 * FROM Huts WHERE HutType = 'Executive' AND IsActive = 1 ORDER BY HutId");
                int selectedHutId = session.UserType == "Executive" ? (executiveHut?.HutId ?? 0) : (staffHut?.HutId ?? 0);
                ViewBag.SelectedHutId = selectedHutId;
                var huts = conn.Query<Hut>("SELECT HutId, HutName, HutType, CostOfHut FROM Huts WHERE IsActive = 1").ToList();
                ViewBag.Huts = huts;
            }

            return View("Index");
        }

        // Pending Approval For HOD ONLY
        public ActionResult HoDPendingApprovals()
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            if (session == null || string.IsNullOrEmpty(session.DecibelId))
                return RedirectToAction("Index", "BHMS");

            ViewBag.UserType = session.UserType;
            ViewBag.UserId = session.UserId;
            ViewBag.FullName = session.FullName;
            ViewBag.ShowHoDPendingApprovals = true;
            ViewBag.ShowPendingApprovals = false;
            ViewBag.IsHoD = IsCurrentUserHoD(session.DecibelId);

            using (var conn = _dapperContext.CreateConnection())
            {
                var staffHut = conn.QueryFirstOrDefault<Hut>("SELECT TOP 1 * FROM Huts WHERE HutType = 'Staff' AND IsActive = 1 ORDER BY HutId");
                var executiveHut = conn.QueryFirstOrDefault<Hut>("SELECT TOP 1 * FROM Huts WHERE HutType = 'Executive' AND IsActive = 1 ORDER BY HutId");
                int selectedHutId = session.UserType == "Executive" ? (executiveHut?.HutId ?? 0) : (staffHut?.HutId ?? 0);
                ViewBag.SelectedHutId = selectedHutId;
                var huts = conn.Query<Hut>("SELECT HutId, HutName, HutType, CostOfHut FROM Huts WHERE IsActive = 1").ToList();
                ViewBag.Huts = huts;
            }

            return View("Index");
        }


        // Pending Approval (HoD) menu and screen only show for users who are actually assigned as HoD in any pending bookings.

        private bool IsCurrentUserHoD(string decibelId)
        {
            if (string.IsNullOrEmpty(decibelId))
                return false;

            using (var connection = _dapperContext.CreateConnection())
            {
                // Checks if there are any bookings waiting for this HoD's approval
                string sql = @"SELECT COUNT(1) FROM Bookings WHERE BookingStatus = 'Booked' AND HODDecibelId = @DecibelId";
                int count = connection.ExecuteScalar<int>(sql, new { DecibelId = decibelId });
                return count > 0;
            }
        }

        [HttpGet]
        public JsonResult GetBookings(int? hutId = null)
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            int currentUserId = session?.UserId ?? 0;
            bool isAdmin = session?.IsAdmin ?? false;

            int selectedHutId = hutId ?? 0;
            using (var connection = _dapperContext.CreateConnection())
            {
                // Dynamically pick default hut if not provided
                if (selectedHutId == 0)
                {
                    string hutType = session.UserType == "Executive" ? "Executive" : "Staff";
                    var hut = connection.QueryFirstOrDefault<Hut>(
                        "SELECT TOP 1 * FROM Huts WHERE HutType = @Type AND IsActive = 1 ORDER BY HutId",
                        new { Type = hutType });
                    selectedHutId = hut?.HutId ?? 0;
                }

                var startDate = DateTime.Today;
                var endDate = startDate.AddMonths(3);

                var bookings = connection.Query(
                  @"SELECT 
                b.BookingId,
                CONVERT(varchar(10), b.BookingDate, 120) AS BookingDate,
                b.BookingStatus,
                b.UserId,
                b.IsApproved,
                b.AdminComments,
                b.CancelledByAdmin,
                b.CostOfHut,
                b.DecibelId,
                h.HutName,
                h.HutType
            FROM Bookings b
            JOIN Huts h ON b.HutId = h.HutId
            WHERE b.HutId = @HutId
                AND b.BookingDate BETWEEN @StartDate AND @EndDate
                AND b.BookingStatus IN ('Booked', 'PendingApproval', 'Cancelled')",
                  new { HutId = selectedHutId, StartDate = startDate, EndDate = endDate }
              ).ToList();
                return Json(bookings, JsonRequestBehavior.AllowGet);
            }
        }



        [HttpPost]
        public JsonResult CheckBookingPolicy(DateTime date, int? hutId = null)
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            if (session == null)
                return Json(new { allowed = false, message = "Session expired. Please login again." });

            // ALLOW ADMIN TO BOOK ANY DATE
            if (session.IsAdmin)
                return Json(new { allowed = true });

            int selectedHutId = hutId ?? 0;
            int userId = session.UserId;
            string userType = (session.UserType ?? "").ToLower();

            using (var connection = _dapperContext.CreateConnection())
            {
                // Dynamically pick default hut if not provided
                if (selectedHutId == 0)
                {
                    string hutType = userType == "executive" ? "Executive" : "Staff";
                    var hut = connection.QueryFirstOrDefault<Hut>(
                        "SELECT TOP 1 * FROM Huts WHERE HutType = @Type AND IsActive = 1 ORDER BY HutId",
                        new { Type = hutType });
                    selectedHutId = hut?.HutId ?? 0;
                }

                bool isExecutive = userType == "executive";
                int maxBookings = isExecutive ? 1 : 2;
                DateTime periodStart, periodEnd;

                if (isExecutive)
                {
                    int currentQuarter = (date.Month - 1) / 3 + 1;
                    periodStart = new DateTime(date.Year, (currentQuarter - 1) * 3 + 1, 1);
                    periodEnd = periodStart.AddMonths(3).AddDays(-1);
                }
                else
                {
                    periodStart = new DateTime(date.Year, 1, 1);
                    periodEnd = new DateTime(date.Year, 12, 31);
                }

                int userBookings = connection.ExecuteScalar<int>(
                    @"SELECT COUNT(1) FROM Bookings
                WHERE UserId = @UserId AND HutId = @HutId
                AND BookingDate BETWEEN @PeriodStart AND @PeriodEnd
                AND BookingStatus = 'Booked'",
                    new { UserId = userId, HutId = selectedHutId, PeriodStart = periodStart, PeriodEnd = periodEnd }
                );

                if (userBookings >= maxBookings)
                {
                    var message = isExecutive
                        ? "Executives can only book once per quarter."
                        : "Staff can only book twice per year.";
                    return Json(new { allowed = false, message });
                }

                int count = connection.ExecuteScalar<int>(
                    @"SELECT COUNT(1) FROM Bookings
                WHERE HutId = @HutId AND BookingDate = @Date AND BookingStatus = 'Booked'",
                    new { HutId = selectedHutId, Date = date }
                );
                if (count > 0)
                {
                    return Json(new { allowed = false, message = "This date is not available for Booking." });
                }

                return Json(new { allowed = true });
            }
        }


        [HttpGet]
        public JsonResult GetCurrentUserDetails()
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            if (session == null)
                return Json(new { error = "Unauthorized" }, JsonRequestBehavior.AllowGet);

            using (var connection = _dapperContext.CreateConnection())
            {
                var user = connection.QueryFirstOrDefault(
                    @"SELECT DecibelId, FullName, PrimaryEmail, DepartmentName, Grade, Designation, MobileNo, IsAdmin
                        FROM Users WHERE Id = @UserId",
                    new { UserId = session.UserId });

                return Json(user, JsonRequestBehavior.AllowGet);
            }
        }

        
[HttpPost]
public async Task<JsonResult> BookDate(BookingRequestModel model)
{

    var session = (SessionModel)Session["BHMS_PortalSession"];
    if (session == null)
        return Json(new ResponseModel<object>
        {
            data = null,
            statusMessage = "Session expired. Please login again.",
            statusCode = HttpStatusCode.Unauthorized
        });

    int userId = session.UserId;
    int hutId = model.HutId;
    bool isAdmin = session.IsAdmin;

    using (var connection = _dapperContext.CreateConnection())
    {
        connection.Open();

                // **DUPLICATE CHECK - Prevent same user booking same hut on same date**
                var existingBooking = connection.QueryFirstOrDefault(
                    @"SELECT BookingId FROM Bookings 
              WHERE UserId = @UserId AND HutId = @HutId AND BookingDate = @BookingDate 
              AND BookingStatus IN ('Booked', 'PendingApproval')",
                    new { UserId = userId, HutId = hutId, BookingDate = model.Date });

                if (existingBooking != null)
                {
                    return Json(new ResponseModel<object>
                    {
                        data = null,
                        statusMessage = "You have already submitted a booking for this date. Please check your existing bookings.",
                        statusCode = HttpStatusCode.BadRequest
                    });
                }


                // 1. Fetch hut info and cost dynamically
                var hut = connection.QueryFirstOrDefault<Hut>("SELECT * FROM Huts WHERE HutId = @HutId AND IsActive = 1", new { HutId = hutId });
        if (hut == null)
        {
            return Json(new ResponseModel<object>
            {
                data = null,
                statusMessage = "Selected hut is not available.",
                statusCode = HttpStatusCode.BadRequest
            });
        }

                using (var transaction = connection.BeginTransaction())
        {
            try
            {
                string nomineeNameToSave = null;
                if (model.BookingFor == "Myself")
                {
                    var userDetails = model.UserDetails;
                    userDetails.UserId = userId;
                    userDetails.CreatedOn = DateTime.Now;
                    userDetails.CostOfHut = model.UserDetails.CostOfHut;

                            connection.Execute(
                        @"INSERT INTO UserDetails (
                            UserId, DecibelId, FullName, PrimaryEmail, DepartmentName, Grade, Designation, MobileNo, CNIC, NoOfPersons, CreatedOn,
                            CostOfHut, HasAdditionalRequirements, AdditionalRequirements
                        )
                        VALUES (
                            @UserId, @DecibelId, @FullName, @PrimaryEmail, @DepartmentName, @Grade, @Designation, @MobileNo, @CNIC, @NoOfPersons, @CreatedOn,
                            @CostOfHut, @HasAdditionalRequirements, @AdditionalRequirements
                        )",
                        userDetails, transaction);

                    nomineeNameToSave = null;
                }
                else if (model.BookingFor == "Nominee")
                {
                    var nomineeDetails = model.NomineeDetails;
                    nomineeDetails.UserId = userId;
                    nomineeDetails.CreatedOn = DateTime.Now;
                    nomineeDetails.CostOfHut = model.NomineeDetails.CostOfHut;

                            connection.Execute(
                        @"INSERT INTO NomineeDetails (
                            UserId, NomineeName, PrimaryEmail, DepartmentName, Grade, Designation, MobileNo, CNIC, NoOfPersons, CreatedOn,
                            CostOfHut, HasAdditionalRequirements, AdditionalRequirements, HeadOfDepartment, HeadOfDepartmentId, HeadOfDepartmentEmail
                        )
                        VALUES (
                            @UserId, @NomineeName, @PrimaryEmail, @DepartmentName, @Grade, @Designation, @MobileNo, @CNIC, @NoOfPersons, @CreatedOn,
                            @CostOfHut, @HasAdditionalRequirements, @AdditionalRequirements, @HeadOfDepartment, @HeadOfDepartmentId, @HeadOfDepartmentEmail
                        )",
                        nomineeDetails, transaction);

                    nomineeNameToSave = nomineeDetails.NomineeName;
                }
                else
                {
                    throw new Exception("Invalid bookingFor value.");
                }

                // 2. Insert into Bookings table - cost from hut
                if (model.BookingFor == "Myself")
                {
                    connection.Execute(
                        @"INSERT INTO Bookings (
                    UserId, HutId, BookingDate, BookingStatus, NomineeName, CreatedOn, IsApproved, CostOfHut
                )
                VALUES (
                    @UserId, @HutId, @BookingDate, 'Booked', @NomineeName, @CreatedOn, 0, @CostOfHut
                )",
                        new
                        {
                            UserId = userId,
                            HutId = hutId,
                            BookingDate = model.Date,
                            NomineeName = nomineeNameToSave,
                            CreatedOn = DateTime.Now,
                            CostOfHut = model.UserDetails.CostOfHut

                        }, transaction);
                }
                else if (model.BookingFor == "Nominee")
                {
                    connection.Execute(
                        @"INSERT INTO Bookings (
                    UserId, HutId, BookingDate, BookingStatus, NomineeName, CreatedOn,
                    IsHODApproved, HODDecibelId, CostOfHut
                )
                VALUES (
                    @UserId, @HutId, @BookingDate, 'Booked', @NomineeName, @CreatedOn,
                    0, @HODDecibelId, @CostOfHut
                )",
                        new
                        {
                            UserId = userId,
                            HutId = hutId,
                            BookingDate = model.Date,
                            NomineeName = nomineeNameToSave,
                            CreatedOn = DateTime.Now,
                            HODDecibelId = model.NomineeDetails.HeadOfDepartmentId,
                            CostOfHut = model.NomineeDetails.CostOfHut

                        }, transaction);
                }

                transaction.Commit();

                        // Fetch user info for email
                        var user = connection.QueryFirstOrDefault<BHMS_Portal.Models.User>(
                            "SELECT FullName, PrimaryEmail FROM Users WHERE Id = @UserId", new { UserId = userId });

                        // Determine if HoD approval required
                        bool requiresHodApproval = model.BookingFor == "Nominee" && !string.IsNullOrEmpty(model.NomineeDetails?.HeadOfDepartmentEmail);
                        string approvalSide = requiresHodApproval ? "Head of Department" : "Admin";


                        decimal actualCost = 0;
                        if (model.BookingFor == "Myself")
                            actualCost = model.UserDetails?.CostOfHut ?? 0;
                        else if (model.BookingFor == "Nominee")
                            actualCost = model.NomineeDetails?.CostOfHut ?? 0;

                        // Prepare and send email to user
                        string userTemplate = Utility.Utility.GetEmailTemplate("Booking Pending Approval");
                        var userValues = new Dictionary<string, string>
                {
                    { "UserName", user?.FullName ?? "User" },
                    { "BookingDate", model.Date.ToString("yyyy-MM-dd") },
                    { "HutName", hut?.HutName ?? "Selected Hut" },
                    { "CostOfHut", actualCost.ToString("C", CultureInfo.CreateSpecificCulture("en-US")).Replace("$", "Rs ") },                   
                    { "ApproverRole", approvalSide },
                    { "AdditionalRequirements", model.UserDetails?.AdditionalRequirements ?? model.NomineeDetails?.AdditionalRequirements ?? "None" }
                };
                        string userEmailBody = Utility.Utility.FillEmailTemplate(userTemplate, userValues);
                        await Utility.Utility.SendEmail(user.PrimaryEmail, "Booking Received – Awaiting Approval", userEmailBody);

                        // Send email to approver(s)
                        if (requiresHodApproval)
                        {
                            // Send to HoD email from NomineeDetails
                            string hodEmail = model.NomineeDetails.HeadOfDepartmentEmail;

                            if (!string.IsNullOrEmpty(hodEmail))
                            {
                                string approverTemplate = Utility.Utility.GetEmailTemplate("Booking Pending Approval Reminder");
                                var approverValues = new Dictionary<string, string>
                        {
                            { "ApproverName", "Head of Department" },
                            { "UserName", user?.FullName ?? "User" },
                            { "BookingDate", model.Date.ToString("yyyy-MM-dd") },
                            { "HutName", hut?.HutName ?? "Selected Hut" },
                            { "CostOfHut", actualCost.ToString("C", CultureInfo.CreateSpecificCulture("en-US")).Replace("$", "Rs ") },
                            { "AdditionalRequirements", model.UserDetails?.AdditionalRequirements ?? model.NomineeDetails?.AdditionalRequirements ?? "None" }
                        };
                                string approverEmailBody = Utility.Utility.FillEmailTemplate(approverTemplate, approverValues);
                                await Utility.Utility.SendEmail(hodEmail, "New Booking Submitted – Approval Required", approverEmailBody);
                            }
                        }
                        else
                        {
                            // Booking for self: send to all active admins
                            var adminEmails = connection.Query<string>(
                                @"SELECT PrimaryEmail FROM Users WHERE IsAdmin = 1 AND IsActive = 1", transaction).ToList();

                            if (adminEmails.Any())
                            {
                                string approverTemplate = Utility.Utility.GetEmailTemplate("Booking Pending Approval Reminder");

                                foreach (var adminEmail in adminEmails)
                                {
                                    var approverValues = new Dictionary<string, string>
                            {
                                { "ApproverName", "Admin" },
                                { "UserName", user?.FullName ?? "User" },
                                { "BookingDate", model.Date.ToString("yyyy-MM-dd") },
                                { "HutName", hut?.HutName ?? "Selected Hut" },
                                { "CostOfHut", actualCost.ToString("C", CultureInfo.CreateSpecificCulture("en-US")).Replace("$", "Rs ") },
                                { "AdditionalRequirements", model.UserDetails?.AdditionalRequirements ?? model.NomineeDetails?.AdditionalRequirements ?? "None" }
                            };

                                    string approverEmailBody = Utility.Utility.FillEmailTemplate(approverTemplate, approverValues);
                                    await Utility.Utility.SendEmail(adminEmail, "New Booking Submitted – Approval Required", approverEmailBody);
                                }
                            }
                        }

                        // --- EMAIL SENDING LOGIC END ---

                        return Json(new ResponseModel<object>
                {
                    data = null,
                    statusMessage = "Date marked successfully! Sent for Approval <br> Status : Pending!",
                    statusCode = HttpStatusCode.OK
                });
            }
            catch (Exception ex)
            {
                        transaction.Rollback();

                        string userMessage;

                        // User-friendly messages for common DB errors
                        if (ex.Message.Contains("Cannot insert the value NULL into column 'NomineeName'"))
                        {
                            userMessage = "Please fill in all required nominee details before submitting.";
                        }
                        else if (ex.Message.Contains("Cannot insert the value NULL into column 'CNIC'"))
                        {
                            userMessage = "Please provide a valid CNIC.";
                        }
                        else
                        {
                            Debug.WriteLine("No admin emails found!");
                            userMessage = "Booking failed. Please check your input and try again.";
                        }

                        return Json(new
                        {
                            success = false,
                            message = userMessage
                        });
                    }
                }
            }
        }

        [HttpGet]
        public JsonResult GetPendingBookings()
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            if (session == null || !session.IsAdmin)
                return Json(new { success = false, message = "Unauthorized" }, JsonRequestBehavior.AllowGet);

            using (var connection = _dapperContext.CreateConnection())
            {
                var bookings = connection.Query(
     @"SELECT b.BookingId, 
             CONVERT(varchar(10), b.BookingDate, 120) AS BookingDate,
             u.FullName AS UserName,
             u.DecibelId,
             b.HutId,
             h.HutName,
             b.CostOfHut
      FROM Bookings b
      JOIN Users u ON b.UserId = u.Id
      JOIN Huts h ON b.HutId = h.HutId
      WHERE b.IsApproved = 0 AND b.BookingStatus = 'Booked' AND b.IsHODApproved IS NULL"
      ).AsList();

                ///The condition b.IsHODApproved IS NULL ensures nominee bookings are excluded from admin’s pending list.

                return Json(bookings, JsonRequestBehavior.AllowGet);
            }
        }

        //HoD Pending Bookings (for nominee approvals)

        [HttpGet]
        public JsonResult GetPendingBookingsForHOD()
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            if (session == null)
                return Json(new { success = false, message = "Unauthorized" }, JsonRequestBehavior.AllowGet);

            string decibelId = session.DecibelId;
            using (var connection = _dapperContext.CreateConnection())
            {
                var bookings = connection.Query(
                    @"SELECT b.BookingId, 
                     CONVERT(varchar(10), b.BookingDate, 120) AS BookingDate,
                     u.FullName AS UserName,
                     u.DecibelId,
                     b.HutId,
                     CASE WHEN b.HutId = 1 THEN 'Staff Hut' ELSE 'Executive Hut' END AS HutName,
                     b.CostOfHut,
                     b.NomineeName
              FROM Bookings b
              JOIN Users u ON b.UserId = u.Id
              WHERE b.IsHODApproved = 0 AND b.HODDecibelId = @DecibelId AND b.BookingStatus = 'Booked'",
                    new { DecibelId = decibelId }
                ).AsList();

                return Json(bookings, JsonRequestBehavior.AllowGet);
            }
        }

        //Admin Approval 
        [HttpPost]
        public async Task<JsonResult> ApproveBooking(int bookingId, string comments = "")
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            if (session == null || !session.IsAdmin)
                return Json(new { success = false, message = "Unauthorized" });

            using (var connection = _dapperContext.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Update booking as approved
                        connection.Execute(
                            @"UPDATE Bookings 
                              SET IsApproved = 1, AdminComments = @Comments
                              WHERE BookingId = @BookingId",
                            new { BookingId = bookingId, Comments = comments }, transaction);

                        // Log admin action
                        connection.Execute(
                            @"INSERT INTO AdminBookingAuditLogs (BookingId, AdminUserId, Action, Comments)
                              VALUES (@BookingId, @AdminUserId, 'Approved', @Comments)",
                            new { BookingId = bookingId, AdminUserId = session.UserId, Comments = comments }, transaction);

                        // Fetch booking and user details for email
                        var booking = connection.QueryFirstOrDefault(
     @"SELECT b.BookingDate, b.CostOfHut, b.HutId, b.AdminComments, u.FullName, u.PrimaryEmail, h.HutName
      FROM Bookings b 
      JOIN Users u ON b.UserId = u.Id 
      JOIN Huts h ON b.HutId = h.HutId
      WHERE b.BookingId = @BookingId",
     new { BookingId = bookingId }, transaction);

                        transaction.Commit();

                        
                        // === EMAIL SENDING START ===
                        if (booking != null)
                        {
                            string hutName = booking.HutName;
                            string template = Utility.Utility.GetEmailTemplate("Booking Approved");

                            // Format cost as Rs
                            string costFormatted = booking.CostOfHut.ToString("C", CultureInfo.CreateSpecificCulture("en-US")).Replace("$", "Rs ");

                            var values = new Dictionary<string, string>
                    {
                        { "UserName", booking.FullName },
                        { "BookingDate", ((DateTime)booking.BookingDate).ToString("yyyy-MM-dd") },
                        { "HutName", hutName },
                        { "CostOfHut", costFormatted },
                        { "AdminComments", booking.AdminComments ?? "No comments provided" },
                        { "ApproverRole", "Admin" }  // Set as Admin here
                            };

                            string emailBody = Utility.Utility.FillEmailTemplate(template, values);
                            await Utility.Utility.SendEmail(booking.PrimaryEmail, "Booking Approved", emailBody);
                        }
                        // === EMAIL SENDING END ===

                        return Json(new { success = true, email = booking?.PrimaryEmail });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = ex.Message });
                    }
                }
            }
        }

        //HOD Approval 
        [HttpPost]
        public async Task<JsonResult> ApproveBookingByHOD(int bookingId, string comments = "")
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            if (session == null)
                return Json(new { success = false, message = "Unauthorized" });

            string decibelId = session.DecibelId;
            using (var connection = _dapperContext.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                       
                        connection.Execute(
                        @"UPDATE Bookings 
                        SET IsHODApproved = 1,
                        IsApproved = 1,                 -- Mark fully approved like Admin
                        BookingStatus = 'Booked',       -- Update status to Booked
                        HODComments = @Comments
                        WHERE BookingId = @BookingId AND HODDecibelId = @DecibelId",
                        new { BookingId = bookingId, Comments = comments, DecibelId = decibelId }, transaction);


                        connection.Execute(
                            @"INSERT INTO HODBookingAuditLogs (BookingId, HODDecibelId, Action, Comments, ActionDate)
                      VALUES (@BookingId, @HODDecibelId, 'Approved', @Comments, GETDATE())",
                            new { BookingId = bookingId, HODDecibelId = decibelId, Comments = comments }, transaction);

                        // Fetch booking and nominee user details for email
                        var booking = connection.QueryFirstOrDefault(
                        @"SELECT b.BookingDate, b.CostOfHut, b.HutId, b.HODComments, u.FullName, u.PrimaryEmail, h.HutName
                        FROM Bookings b
                        LEFT JOIN Users u ON b.UserId = u.Id
                        JOIN Huts h ON b.HutId = h.HutId
                        WHERE b.BookingId = @BookingId",
                        new { BookingId = bookingId }, transaction);


                        transaction.Commit();

                        if (booking != null && !string.IsNullOrEmpty(booking.PrimaryEmail))
                        {
                            string hutName = booking.HutName;
                            string template = Utility.Utility.GetEmailTemplate("Booking Approved");
                            // Format cost as Rs
                            string costFormatted = booking.CostOfHut.ToString("C", CultureInfo.CreateSpecificCulture("en-US")).Replace("$", "Rs ");

                            var values = new Dictionary<string, string>
                    {
                        { "UserName", booking.FullName },
                        { "BookingDate", ((DateTime)booking.BookingDate).ToString("yyyy-MM-dd") },
                        { "HutName", hutName },
                        { "CostOfHut", costFormatted },
                        { "AdminComments", booking.HODComments ?? "No comments provided" },
                        { "ApproverRole", "Head of Department" }  // Set as HoD here
                            
                            };

                            string emailBody = Utility.Utility.FillEmailTemplate(template, values);
                            await Utility.Utility.SendEmail(booking.PrimaryEmail, "Booking Approved by Head of Department", emailBody);
                        }

                        return Json(new { success = true, email = booking.PrimaryEmail });

                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = ex.Message });
                    }
                }
            }
        }


        [HttpPost]
        public async Task<JsonResult> AdminCancelBooking(int bookingId, string comments)
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            if (session == null || !session.IsAdmin)
                return Json(new { success = false, message = "Unauthorized" });

            using (var connection = _dapperContext.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Update booking as cancelled by admin
                        connection.Execute(
                            @"UPDATE Bookings 
                              SET BookingStatus = 'Cancelled', CancellationDate = GETDATE(),
                                  CancelledByAdmin = 1, AdminComments = @Comments
                              WHERE BookingId = @BookingId",
                            new { BookingId = bookingId, Comments = comments }, transaction);

                        // Log admin action
                        connection.Execute(
                            @"INSERT INTO AdminBookingAuditLogs (BookingId, AdminUserId, Action, Comments)
                              VALUES (@BookingId, @AdminUserId, 'Cancelled', @Comments)",
                            new { BookingId = bookingId, AdminUserId = session.UserId, Comments = comments }, transaction);

                        // Get booking details and user details for email
                        var booking = connection.QueryFirstOrDefault(
     @"SELECT b.BookingDate, b.CostOfHut, b.HutId, b.AdminComments, u.FullName, u.PrimaryEmail, b.PenaltyAmount, h.HutName
      FROM Bookings b 
      JOIN Users u ON b.UserId = u.Id 
      JOIN Huts h ON b.HutId = h.HutId
      WHERE b.BookingId = @BookingId",
     new { BookingId = bookingId }, transaction);

                        transaction.Commit();

                        //                // Prepare and send email
                        //                string hutName = booking.HutId == 1 ? "Staff Hut" : "Executive Hut";
                        //                string template = Utility.Utility.GetEmailTemplate("Booking Cancelled");
                        //                var values = new Dictionary<string, string>
                        //                {
                        //                    { "UserName", booking.FullName },
                        //                    { "BookingDate", ((DateTime)booking.BookingDate).ToString("yyyy-MM-dd") },
                        //                    { "HutName", hutName },
                        //                    { "CostOfHut", booking.CostOfHut.ToString("C") },
                        //                    { "PenaltyAmount", booking.PenaltyAmount?.ToString("C") ?? "0" },
                        //                    { "AdminComments", booking.AdminComments ?? "" }
                        //                };
                        //                string emailBody = Utility.Utility.FillEmailTemplate(template, values);

                        //                await Utility.Utility.SendEmail(booking.PrimaryEmail, "Booking Cancelled", emailBody);

                        //                return Json(new { success = true });
                        //            }
                        //            catch (Exception ex)
                        //            {
                        //                transaction.Rollback();
                        //                return Json(new
                        //                {
                        //                    success = false,
                        //                    message = ex.Message
                        //                });
                        //            }
                        //        }
                        //    }
                        //}


                        // === EMAIL SENDING START ===
                        if (booking != null)
                        {
                            string hutName = booking.HutName; string template = Utility.Utility.GetEmailTemplate("Booking Cancelled");

                            // Format cost as Rs
                            string costFormatted = booking.CostOfHut.ToString("C", CultureInfo.CreateSpecificCulture("en-US")).Replace("$", "Rs ");

                            var values = new Dictionary<string, string>
                    {
                        { "UserName", booking.FullName },
                        { "BookingDate", ((DateTime)booking.BookingDate).ToString("yyyy-MM-dd") },
                        { "HutName", hutName },
                        { "CostOfHut", costFormatted },
                        { "PenaltyAmount", booking.PenaltyAmount?.ToString("C") ?? "0" },
                        { "AdminComments", booking.AdminComments ?? "No comments provided" },
                        { "ApproverRole", "Admin" }  // Set as Admin here
                    };

                            string emailBody = Utility.Utility.FillEmailTemplate(template, values);
                            await Utility.Utility.SendEmail(booking.PrimaryEmail, "Booking Cancelled", emailBody);
                        }
                        // === EMAIL SENDING END ===

                        // Show notification to admin (in your UI, after AJAX success, show: 
                        // "Booking cancelled email sent to [booking.PrimaryEmail]")

                        return Json(new { success = true, email = booking?.PrimaryEmail });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = ex.Message });
                    }
                }
            }
        }



        // HOD Cancel Booking 

        [HttpPost]
        public async Task<JsonResult> CancelBookingByHOD(int bookingId, string comments = "")
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            if (session == null)
                return Json(new { success = false, message = "Unauthorized" });

            string decibelId = session.DecibelId;
            using (var connection = _dapperContext.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        connection.Execute(
                            @"UPDATE Bookings SET BookingStatus = 'Cancelled', CancelledByHOD = 1, HODComments = @Comments WHERE BookingId = @BookingId AND HODDecibelId = @DecibelId",
                            new { BookingId = bookingId, Comments = comments, DecibelId = decibelId }, transaction);

                        connection.Execute(
                            @"INSERT INTO HODBookingAuditLogs (BookingId, HODDecibelId, Action, Comments, ActionDate)
                      VALUES (@BookingId, @HODDecibelId, 'Cancelled', @Comments, GETDATE())",
                            new { BookingId = bookingId, HODDecibelId = decibelId, Comments = comments }, transaction);



                        // Fetching details for email 
                        var booking = connection.QueryFirstOrDefault(
                     @"SELECT b.BookingDate, b.CostOfHut, b.HutId, b.HODComments, u.FullName, u.PrimaryEmail, h.HutName
                     FROM Bookings b
                     LEFT JOIN Users u ON b.UserId = u.Id
                     JOIN Huts h ON b.HutId = h.HutId
                     WHERE b.BookingId = @BookingId",
                     new { BookingId = bookingId }, transaction);

                        transaction.Commit();

                        if (booking != null && !string.IsNullOrEmpty(booking.PrimaryEmail))
                        {
                            string hutName = booking.HutName;
                            string template = Utility.Utility.GetEmailTemplate("Booking Cancelled");

                            // Format cost as Rs
                            string costFormatted = booking.CostOfHut.ToString("C", CultureInfo.CreateSpecificCulture("en-US")).Replace("$", "Rs ");


                            var values = new Dictionary<string, string>
                    {
                        { "UserName", booking.FullName },
                        { "BookingDate", ((DateTime)booking.BookingDate).ToString("yyyy-MM-dd") },
                        { "HutName", hutName },
                        { "CostOfHut", costFormatted },
                        { "AdminComments", booking.HODComments ?? "No comments provided" },
                        { "ApproverRole", "Head of Department" }  // Set as HoD here
                    };

                            string emailBody = Utility.Utility.FillEmailTemplate(template, values);
                            await Utility.Utility.SendEmail(booking.PrimaryEmail, "Booking Cancelled by Head of Department", emailBody);
                        }

                        return Json(new { success = true, email = booking.PrimaryEmail });

                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = ex.Message });
                    }
                }
            }
        }


  

        [HttpGet]
        public JsonResult GetCancellationInfo(int bookingId)
        {
            using (var connection = _dapperContext.CreateConnection())
            {
                var booking = connection.QueryFirstOrDefault<Booking>(
                    "SELECT * FROM Bookings WHERE BookingId = @BookingId AND BookingStatus = 'Booked'",
                    new { BookingId = bookingId });

                if (booking == null)
                    return Json(new { success = false, message = "Booking not found or already cancelled." }, JsonRequestBehavior.AllowGet);

                var daysLeft = (booking.BookingDate - DateTime.Today).Days;

                string penaltyMessage;
                decimal penaltyPercent;

                if (daysLeft >= 7)
                {
                    penaltyMessage = "No penalty will be charged for cancellation made 7 or more days prior to visit date.";
                    penaltyPercent = 0m;
                }
                else if (daysLeft >= 0 && daysLeft <= 3)
                {
                    penaltyMessage = "50% penalty will be charged for cancellation made 3 or fewer days prior to visit date.";
                    penaltyPercent = 50m;
                }
                else if (daysLeft < 0)
                {
                    penaltyMessage = "100% penalty will be charged for no-show (no cancellation informed).";
                    penaltyPercent = 100m;
                }
                else
                {
                    penaltyMessage = "Cancellation policy does not allow cancellation at this time.";
                    penaltyPercent = 100m;
                }

                return Json(new
                {
                    success = true,
                    bookingDate = booking.BookingDate.ToString("yyyy-MM-dd"),
                    daysLeft,
                    penaltyMessage,
                    penaltyPercent,
                    costOfHut = booking.CostOfHut
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // Cancel booking with penalty calculation for Users
        //[HttpPost]
        //public JsonResult CancelBooking(CancelBookingRequest model)
        //{
        //    // Log start of cancellation
        //    Debug.WriteLine($"CancelBooking called for BookingId={model.BookingId}, UserId={model.UserId}");


        //    using (var connection = _dapperContext.CreateConnection())
        //    {
        //        var booking = connection.QueryFirstOrDefault<Booking>(
        //            "SELECT * FROM Bookings WHERE BookingId = @BookingId AND UserId = @UserId AND BookingStatus = 'Booked'",
        //            new { BookingId = model.BookingId, UserId = model.UserId });

        //        if (booking == null)
        //            return Json(new { success = false, message = "Booking not found or already cancelled." });

        //        var daysLeft = (booking.BookingDate - DateTime.Today).Days;

        //        decimal penaltyPercent = 100m;
        //        if (daysLeft >= 7)
        //            penaltyPercent = 0m;
        //        else if (daysLeft >= 0 && daysLeft <= 3)
        //            penaltyPercent = 50m;

        //        decimal penaltyAmount = (booking.CostOfHut * penaltyPercent) / 100m;

        //        // If emergency cancellation with waiver, penalty can be overridden (implement your logic here)

        //        // Update booking status to Cancelled
        //        connection.Execute(
        //            "UPDATE Bookings SET BookingStatus = 'Cancelled', CancellationDate = @CancelDate, PenaltyAmount = @PenaltyAmount WHERE BookingId = @BookingId",
        //            new { CancelDate = DateTime.Now, PenaltyAmount = penaltyAmount, BookingId = model.BookingId });

        //        return Json(new
        //        {
        //            success = true,
        //            message = penaltyPercent == 0 ? "Booking cancelled without penalty." :
        //                      $"Booking cancelled with {penaltyPercent}% penalty. Penalty amount: {penaltyAmount:C}."
        //        });
        //    }
        //}
        [HttpPost]
        public async Task<JsonResult> CancelBooking(CancelBookingRequest model)
        {
            Debug.WriteLine($"CancelBooking called for BookingId={model.BookingId}, UserId={model.UserId}");

            using (var connection = _dapperContext.CreateConnection())
            {
                // 1. Fetch booking
                var booking = connection.QueryFirstOrDefault<Booking>(
                    "SELECT * FROM Bookings WHERE BookingId = @BookingId AND UserId = @UserId AND BookingStatus = 'Booked'",
                    new { BookingId = model.BookingId, UserId = model.UserId });

                if (booking == null)
                    return Json(new { success = false, message = "Booking not found or already cancelled." });

                // 2. Get CostOfHut from UserDetails or NomineeDetails if missing in Bookings
                decimal costOfHut = 0m;
                if (booking.CostOfHut > 0)
                {
                    costOfHut = booking.CostOfHut;
                }
                else
                {
                    // Try to get from UserDetails (for self booking)
                    costOfHut = connection.ExecuteScalar<decimal?>(
                        @"SELECT TOP 1 CostOfHut FROM UserDetails WHERE UserId = @UserId ORDER BY CreatedOn DESC",
                        new { UserId = booking.UserId }
                    ) ?? 0;

                    // If still zero, try from NomineeDetails (for nominee booking)
                    if (costOfHut == 0 && !string.IsNullOrEmpty(booking.NomineeName))
                    {
                        costOfHut = connection.ExecuteScalar<decimal?>(
                            @"SELECT TOP 1 CostOfHut FROM NomineeDetails WHERE UserId = @UserId AND NomineeName = @NomineeName ORDER BY CreatedOn DESC",
                            new { UserId = booking.UserId, NomineeName = booking.NomineeName }
                        ) ?? 0;
                    }
                }

                // 3. Calculate penalty percent
                var daysLeft = (booking.BookingDate - DateTime.Today).Days;
                decimal penaltyPercent = 100m;
                if (daysLeft >= 7)
                    penaltyPercent = 0m;
                else if (daysLeft >= 0 && daysLeft <= 3)
                    penaltyPercent = 50m;

                // 4. Calculate penalty amount in Rs
                decimal penaltyAmount = Math.Round((costOfHut * penaltyPercent) / 100m, 0);

                // 5. Update booking status to Cancelled and store penalty in Rs
                connection.Execute(
                    @"UPDATE Bookings 
              SET BookingStatus = 'Cancelled', 
                  CancellationDate = @CancelDate, 
                  PenaltyAmount = @PenaltyAmount,
                  CostOfHut = @CostOfHut
              WHERE BookingId = @BookingId",
                    new { CancelDate = DateTime.Now, PenaltyAmount = penaltyAmount, CostOfHut = costOfHut, BookingId = model.BookingId });

                // 6. Fetch user info for email
                var user = connection.QueryFirstOrDefault<BHMS_Portal.Models.User>(
                    "SELECT FullName, PrimaryEmail FROM Users WHERE Id = @UserId",
                    new { UserId = booking.UserId });

                // 7. Fetch hut info for email
                var hutInfo = connection.QueryFirstOrDefault<Hut>(
                    "SELECT HutName FROM Huts WHERE HutId = @HutId",
                    new { HutId = booking.HutId });

                // 8. Prepare email template and values
                string template = Utility.Utility.GetEmailTemplate("Booking Cancelled Confirmation");

                var values = new Dictionary<string, string>
        {
            { "UserName", user?.FullName ?? "User" },
            { "BookingDate", booking.BookingDate.ToString("yyyy-MM-dd") },
            { "HutName", hutInfo?.HutName ?? "Selected Hut" },
            { "CostOfHut", costOfHut.ToString("C", CultureInfo.CreateSpecificCulture("en-US")).Replace("$", "Rs ") },
            { "PenaltyAmount", penaltyAmount.ToString("C", CultureInfo.CreateSpecificCulture("en-US")).Replace("$", "Rs ") },
           // { "AdminComments", "User cancelled the booking." }, 
         //   { "ApproverRole", "User" }
        };

                string emailBody = Utility.Utility.FillEmailTemplate(template, values);

                // 9. Send cancellation email to user asynchronously
                await Utility.Utility.SendEmail(user.PrimaryEmail, "Booking Cancelled Confirmation", emailBody);

                // 10. Prepare penalty message once (avoid duplicate variable)
                string penaltyMessage = penaltyPercent == 0
                    ? "Booking cancelled without penalty."
                    : $"Booking cancelled with {penaltyPercent}% penalty. Penalty amount: Rs {penaltyAmount:N0}.";

                // 11. Return success message with penalty info
                return Json(new
                {
                    success = true,
                    message = penaltyMessage
                });
            }
        }


        [HttpGet]
        public JsonResult GetHeadsOfDepartment()
        {
            try
            {
                // Force TLS 1.2 for secure connections
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

                // Prepare parameters for stored procedure call
                var args = new DecibelAPI.getRequestList[]
                {
            new DecibelAPI.getRequestList
            {
                name = "P_EMP_ID",
                value = "",
                p_type = DecibelAPI.OracleDbType.Varchar2
            },
            new DecibelAPI.getRequestList
            {
                name = "P_OPT",
                value = "2",
                p_type = DecibelAPI.OracleDbType.Varchar2
            },
            new DecibelAPI.getRequestList
            {
                name = "P_EMP_RECORDSET",
                direction = DecibelAPI.ParameterDirection.Output,
                p_type = DecibelAPI.OracleDbType.RefCursor
            }
                };

                // Call the stored procedure via SOAP API
                var ds = new DecibelAPI.getDataSoap().callProcedure("ISF_Decibel", "TMS_DEC_EMP_DETAIL", args);

                var heads = new List<object>();

                if (ds != null && ds.Tables.Count > 0)
                {
                    // Debug: Log all column names returned by the stored procedure
                    foreach (DataColumn col in ds.Tables[0].Columns)
                    {
                        System.Diagnostics.Debug.WriteLine("Column: " + col.ColumnName);
                    }

                    // Iterate over each row and map to anonymous object
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        // Check for null or empty values to avoid exceptions
                        var id = row["GROUP_HEAD_ID"]?.ToString();
                        var name = row["GROUP_HEAD_NAME"]?.ToString();
                        var email = row["GROUP_HEAD_EMAIL"]?.ToString();

                        if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(name))
                        {
                            heads.Add(new
                            {
                                DecibelId = id,
                                FullName = name,
                                DepartmentName = email ?? string.Empty
                            });
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No data returned from stored procedure.");
                }

                return Json(heads, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Log the exception details for debugging
                System.Diagnostics.Debug.WriteLine("Error in GetHeadsOfDepartment: " + ex.Message);
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);

                // Return an empty JSON array or an error message as needed
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }
        }



        [HttpGet]
        public JsonResult GetRestrictedDates(int hutId)
        {
            using (var conn = _dapperContext.CreateConnection())
            {
                var dates = conn.Query<DateTime>(
                    "SELECT RestrictDate FROM RestrictedDates WHERE HutId = @HutId",
                    new { HutId = hutId }).ToList();

                // Return as string array in "yyyy-MM-dd" format
                var dateStrings = dates.Select(d => d.ToString("yyyy-MM-dd")).ToArray();
                return Json(dateStrings, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult RestrictDate(int hutId, DateTime date)
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            if (session == null || !session.IsAdmin)
                return Json(new { success = false, message = "Unauthorized" });

            using (var conn = _dapperContext.CreateConnection())
            {
                var exists = conn.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM RestrictedDates WHERE HutId = @HutId AND RestrictDate = @Date",
                    new { HutId = hutId, Date = date });

                if (exists > 0)
                    return Json(new { success = false, message = "Date already restricted." });

                conn.Execute(
                    "INSERT INTO RestrictedDates (HutId, RestrictDate, CreatedBy) VALUES (@HutId, @Date, @UserId)",
                    new { HutId = hutId, Date = date, UserId = session.UserId });

                return Json(new { success = true, message = "Date restricted successfully." });
            }
        }

        [HttpPost]
        public JsonResult UnrestrictDate(int hutId, DateTime date)
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            if (session == null || !session.IsAdmin)
                return Json(new { success = false, message = "Unauthorized" });

            using (var conn = _dapperContext.CreateConnection())
            {
                conn.Execute(
                    "DELETE FROM RestrictedDates WHERE HutId = @HutId AND RestrictDate = @Date",
                    new { HutId = hutId, Date = date });

                return Json(new { success = true, message = "Restriction removed successfully." });
            }
        }

        [HttpGet]
        public ActionResult GetBookingDetails(int bookingId)
        {
            using (var conn = _dapperContext.CreateConnection())
            {
                var sql = @"
SELECT 
    b.BookingId,
    b.UserId,
    b.HutId,
    h.HutName,
    b.BookingDate,
    b.BookingStatus,
    u.FullName AS UserFullName,
    u.DecibelId AS UserDecibelId,
    u.PrimaryEmail AS UserEmail,
    u.DepartmentName AS UserDepartment,
    u.Grade AS UserGrade,
    u.Designation AS UserDesignation,
    u.MobileNo AS UserMobileNo,
    ud.CNIC AS UserCNIC,
    b.NomineeName,
    b.IsApproved as IsAdminApproved,
    b.AdminComments,
    b.IsHODApproved,
    b.HODComments,
    nd.NomineeDetailsId,
    nd.NomineeName AS NomineeFullName,
    nd.PrimaryEmail AS NomineeEmail,
    nd.DepartmentName AS NomineeDepartment,
    nd.Grade AS NomineeGrade,
    nd.Designation AS NomineeDesignation,
    nd.MobileNo AS NomineeMobileNo,
    nd.CNIC AS NomineeCNIC,
    nd.HeadOfDepartment,
    nd.HeadOfDepartmentId,
    nd.HeadOfDepartmentEmail,
    COALESCE(nd.NoOfPersons, ud.NoOfPersons) AS TotalPersonsForBooking,
    COALESCE(nd.CostOfHut, ud.CostOfHut) AS BookingCostOfHut,
    COALESCE(nd.HasAdditionalRequirements, ud.HasAdditionalRequirements) AS BookingHasAdditionalRequirements,
    COALESCE(NULLIF(nd.AdditionalRequirements, ''), ud.AdditionalRequirements) AS BookingAdditionalRequirements,
    b.CancellationDate AS BookingCancellationDate,
    b.CancelledByAdmin,
    b.CancelledByHOD,
    b.PenaltyAmount,
    b.TotalMaintenanceCost,
    b.IsReturned
FROM 
    Bookings b
LEFT JOIN Huts h ON b.HutId = h.HutId
LEFT JOIN Users u ON b.UserId = u.Id
LEFT JOIN (
    SELECT *, ROW_NUMBER() OVER (
        PARTITION BY UserId, CAST(CreatedOn AS DATE), NomineeName
        ORDER BY CreatedOn DESC
    ) rn
    FROM NomineeDetails
) nd
    ON nd.UserId = b.UserId
    AND CAST(nd.CreatedOn AS DATE) = CAST(b.CreatedOn AS DATE)
    AND nd.NomineeName = b.NomineeName
    AND nd.rn = 1
LEFT JOIN (
    SELECT *, ROW_NUMBER() OVER (
        PARTITION BY UserId, CAST(CreatedOn AS DATE)
        ORDER BY CreatedOn DESC
    ) rn
    FROM UserDetails
) ud
    ON ud.UserId = b.UserId
    AND CAST(ud.CreatedOn AS DATE) = CAST(b.CreatedOn AS DATE)
    AND ud.rn = 1
WHERE b.BookingId = @BookingId";

                var result = conn.QueryFirstOrDefault(sql, new { BookingId = bookingId });
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }



        [HttpGet]
        public JsonResult GetProcessedBookings()
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            if (session == null || !session.IsAdmin)
                return Json(new { success = false, message = "Unauthorized" }, JsonRequestBehavior.AllowGet);

            using (var connection = _dapperContext.CreateConnection())
            {
                var bookings = connection.Query(
        @"SELECT b.BookingId, 
        CONVERT(varchar(10), b.BookingDate, 120) AS BookingDate,
        u.FullName AS UserName,
        u.DecibelId,
        b.HutId,
        h.HutName,
        b.CostOfHut
 FROM Bookings b
 JOIN Users u ON b.UserId = u.Id
 JOIN Huts h ON b.HutId = h.HutId
 WHERE (b.IsApproved = 1 OR b.IsApproved = -1) AND b.BookingStatus = 'Booked'"
                ).AsList();

                return Json(bookings, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetProcessedBookingsForHOD()
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            if (session == null)
                return Json(new { success = false, message = "Unauthorized" }, JsonRequestBehavior.AllowGet);

            string decibelId = session.DecibelId;

            using (var connection = _dapperContext.CreateConnection())
            {
                var bookings = connection.Query(
                    @"SELECT b.BookingId, 
                CONVERT(varchar(10), b.BookingDate, 120) AS BookingDate,
                u.FullName AS UserName,
                u.DecibelId,
                b.HutId,
                CASE WHEN b.HutId = 1 THEN 'Staff Hut' ELSE 'Executive Hut' END AS HutName,
                b.CostOfHut,
                b.NomineeName
            FROM Bookings b
            JOIN Users u ON b.UserId = u.Id
            WHERE (b.IsHODApproved = 1 OR b.IsHODApproved = -1) 
              AND b.HODDecibelId = @DecibelId 
              AND b.BookingStatus = 'Booked'",
                    new { DecibelId = decibelId }
                ).AsList();

                return Json(bookings, JsonRequestBehavior.AllowGet);
            }
        }



    }

}




