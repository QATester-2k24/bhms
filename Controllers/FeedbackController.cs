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
    public class FeedbackController : Controller
    {
        private readonly DapperContext _dapperContext = new DapperContext();


        private void SetFeedbackMenuVisibility()
        {
            var session = (SessionModel)Session["BHMS_PortalSession"];
            if (session == null)
            {
                ViewBag.ShowFeedbackMenu = false;
                return;
            }

            using (var conn = _dapperContext.CreateConnection())
            {
                var count = conn.ExecuteScalar<int>(@"
            SELECT COUNT(1) 
            FROM Bookings b
            INNER JOIN BookingFeedbackKeys fbk ON b.BookingId = fbk.BookingId
            WHERE b.UserId = @UserId 
              AND b.IsReturned = 1 
              AND fbk.IsUsed = 0",
                    new { UserId = session.UserId });

                ViewBag.ShowFeedbackMenu = count > 0;
            }
        }

        [Authorize]
        public ActionResult Submit(int bookingId, string key)
        {
            if (!IsValidFeedbackKey(bookingId, key))
                return View("InvalidFeedbackLink");

            var model = new FeedbackViewModel
            {
                BookingId = bookingId,
                FeedbackKey = key
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Submit(FeedbackViewModel model)
        {
            if (!IsValidFeedbackKey(model.BookingId, model.FeedbackKey))
                return View("InvalidFeedbackLink");

            if (ModelState.IsValid)
            {
                SaveFeedback(model);
                MarkFeedbackKeyAsUsed(model.BookingId, model.FeedbackKey);
                return View("ThankYou");
            }
            return View(model);
        }

        private bool IsValidFeedbackKey(int bookingId, string key)
        {
            using (var conn = _dapperContext.CreateConnection())
            {
                var sql = "SELECT COUNT(1) FROM BookingFeedbackKeys WHERE BookingId = @BookingId AND FeedbackKey = @Key AND IsUsed = 0";
                return conn.ExecuteScalar<int>(sql, new { BookingId = bookingId, Key = key }) > 0;
            }
        }

        private void SaveFeedback(FeedbackViewModel model)
        {
            using (var conn = _dapperContext.CreateConnection())
            {
                var sql = @"INSERT INTO Feedbacks 
                            (BookingId, HutConditionRating, HygieneRating, StaffRating, ItemsRating, Comments, CreatedAt) 
                            VALUES (@BookingId, @HutConditionRating, @HygieneRating, @StaffRating, @ItemsRating, @Comments, GETDATE())";
                conn.Execute(sql, model);
            }
        }

        private void MarkFeedbackKeyAsUsed(int bookingId, string key)
        {
            using (var conn = _dapperContext.CreateConnection())
            {
                var sql = "UPDATE BookingFeedbackKeys SET IsUsed = 1 WHERE BookingId = @BookingId AND FeedbackKey = @Key";
                conn.Execute(sql, new { BookingId = bookingId, Key = key });
            }
        }
    }
}




