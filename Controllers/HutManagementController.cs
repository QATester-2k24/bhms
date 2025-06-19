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


namespace BHMS_Portal.Controllers
{

    [Authorize]
    public class HutManagementController : BaseController
    {
        private readonly DapperContext _dapperContext = new DapperContext();

        private bool IsAdminUser()
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            return session != null && session.IsAdmin;
        }

        public ActionResult Index()
        {
            if (!IsAdminUser())
                return RedirectToAction("Index", "BHMS");

            using (var conn = _dapperContext.CreateConnection())
            {
                var huts = conn.Query<Hut>("SELECT * FROM Huts ORDER BY HutId ASC");
                return View(huts);
            }
        }

        [HttpPost]
        public JsonResult SaveHut(Hut hut)
        {
            if (!IsAdminUser())
                return Json(new { success = false, message = "Unauthorized" });

            using (var conn = _dapperContext.CreateConnection())
            {
                if (hut.HutId == 0)
                {
                    conn.Execute(@"INSERT INTO Huts (HutName, HutType, CostOfHut, IsActive) VALUES (@HutName, @HutType, @CostOfHut, @IsActive)", hut);
                }
                else
                {
                    conn.Execute(@"UPDATE Huts SET HutName = @HutName, HutType = @HutType, CostOfHut = @CostOfHut, IsActive = @IsActive WHERE HutId = @HutId", hut);
                }
                return Json(new { success = true });
            }
        }

        [HttpPost]
        public JsonResult DeleteHut(int hutId)
        {
            if (!IsAdminUser())
                return Json(new { success = false, message = "Unauthorized" });

            using (var conn = _dapperContext.CreateConnection())
            {
                // Check if hut is referenced in Bookings
                var bookingCount = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM Bookings WHERE HutId = @HutId", new { HutId = hutId });
                if (bookingCount > 0)
                {
                    return Json(new { success = false, message = "Cannot delete hut: it is referenced in bookings." });
                }

                conn.Execute("DELETE FROM Huts WHERE HutId = @HutId", new { HutId = hutId });
                return Json(new { success = true });
            }
        }



    }

}





