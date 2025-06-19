using BHMS_Portal.Models;
using BHMS_Portal.DataAccess;
using BHMS_Portal.Utility;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Security;
using System.Web;
using System.Configuration;
using System.Diagnostics;



namespace BHMS_Portal.Controllers
{
    [Authorize]
    public class BaseController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            if (session == null)
            {
                // Redirect to login page if session is missing
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary(
                        new { controller = "Account", action = "Login" }
                    )
                );
                return;
            }
            // --- Add this block to always set hut names/IDs for the sidebar ---
            using (var conn = new DapperContext().CreateConnection())
            {
                var staffHut = conn.QueryFirstOrDefault<Hut>("SELECT TOP 1 * FROM Huts WHERE HutType = 'Staff' AND IsActive = 1 ORDER BY HutId");
                var executiveHut = conn.QueryFirstOrDefault<Hut>("SELECT TOP 1 * FROM Huts WHERE HutType = 'Executive' AND IsActive = 1 ORDER BY HutId");

                filterContext.Controller.ViewBag.StaffHutId = staffHut?.HutId ?? 0;
                filterContext.Controller.ViewBag.StaffHutName = staffHut?.HutName ?? "Staff Hut";
                filterContext.Controller.ViewBag.ExecutiveHutId = executiveHut?.HutId ?? 0;
                filterContext.Controller.ViewBag.ExecutiveHutName = executiveHut?.HutName ?? "Executive Hut";
            }
            // --- End block ---



            // --- Add this block to set IsAdmin and other role flags for sidebar visibility ---
            filterContext.Controller.ViewBag.IsAdmin = session.IsAdmin;
            filterContext.Controller.ViewBag.IsHoD = session.UserType == "HoD"; // example, adjust as needed
            // Add other flags as needed


            base.OnActionExecuting(filterContext);
        }
    }
}

