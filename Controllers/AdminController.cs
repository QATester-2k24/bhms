using BHMS_Portal.DataAccess;
using BHMS_Portal.Models;
using Dapper;
using System.Collections.Generic;
using System.Web.Mvc;

namespace BHMS_Portal.Controllers
{
    [Authorize]  // Ensure user is authenticated
    public class AdminController : Controller
    {
        private readonly DapperContext _dapperContext = new DapperContext();

        // Helper method to check if current user is admin
        private bool IsAdminUser()
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            return session != null && session.IsAdmin;
        }

        // Booking Logs action - Admin only
        public ActionResult BookingLogs()
        {
            if (!IsAdminUser())
            {
                // Redirect non-admin users to home or access denied page
                return RedirectToAction("Index", "BHMS");
            }

            var session = (SessionModel)Session["BHMS_PortalSession"];
            ViewBag.UserType = session.UserType;
            ViewBag.UserId = session.UserId;
            ViewBag.IsAdmin = session.IsAdmin;
            ViewBag.FullName = session.FullName;
            ViewBag.ShowBookingLogs = true;  // For sidebar active state

            using (var conn = _dapperContext.CreateConnection())
            {
                var bookingLogs = conn.Query<BookingLogViewModel>(@"
                   
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
ORDER BY b.BookingDate DESC, b.BookingId DESC


                ").AsList();

                return View(bookingLogs);
            }
        }
    }
}
