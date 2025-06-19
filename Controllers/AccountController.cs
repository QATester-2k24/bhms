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

    public class AccountController : Controller
    {
        private readonly DapperContext _dapperContext;
        DateTime currentDate = DateTime.UtcNow;
        SessionModel BHMS_Portal_Session = new SessionModel();

        public AccountController()
        {
            _dapperContext = new DapperContext();
        }


        //   RegisterModel User 

        [HttpGet]
        public ActionResult RegisterUser(string v_fcode)
        {
            SessionModel sess = (SessionModel)Session["BHMS_PortalSession"];
            if (sess != null)
            {
                return RedirectToAction("LoginUser", "Account");
            }

            using (var connection = _dapperContext.CreateConnection())
            {
                var sql = "SELECT * FROM VerificationCodes WHERE Code = @Code";
                var usr = connection.QueryFirstOrDefault<VerificationCode>(sql, new { Code = v_fcode });
                return View(usr);
            }
        }

        //  Reset Password


        [HttpGet]
        public ActionResult ResetPassword(string v_fcode)
        {
            SessionModel sess = (SessionModel)Session["BHMS_PortalSession"];
            if (sess != null)
            {
                return RedirectToAction("LoginUser", "Account");
            }

            using (var connection = _dapperContext.CreateConnection())
            {
                var sql = "SELECT * FROM VerificationCodes WHERE Code = @Code";
                var usr = connection.QueryFirstOrDefault<VerificationCode>(sql, new { Code = v_fcode });
                return View(usr);
            }
        }

        [HttpPost]
        public JsonResult ResetPassword(RegisterModel registerModel)
        {
            try
            {
                var res = new ResponseModel<object>();
                var EncryptedPassword = MD5_encode(registerModel.Password);

                using (var connection = _dapperContext.CreateConnection())
                {

                    connection.Open();

                    // Check if user exists
                    var sqlCheckUser = "SELECT COUNT(1) FROM Users WHERE DecibelId = @DecibelId";
                    var userExists = connection.ExecuteScalar<int>(sqlCheckUser, new { DecibelId = registerModel.DecibelId }) > 0;

                    if (!userExists)
                    {
                        res = new ResponseModel<object>() { data = null, statusMessage = "Invalid Credentials! Please contact with Administrator", statusCode = HttpStatusCode.BadRequest };
                    }
                    else
                    {
                        // Using transaction to ensure both operations succeed or fail together
                        using (var transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                // Update password
                                var sqlUpdateUser = "UPDATE Users SET Password = @Password WHERE DecibelId = @DecibelId";
                                connection.Execute(sqlUpdateUser, new { Password = EncryptedPassword, DecibelId = registerModel.DecibelId }, transaction);

                                // Update verification code
                                var sqlUpdateVerificationCode = "UPDATE VerificationCodes SET IsVerified = 1, VerifiedOn = @VerifiedOn WHERE DecibelId = @DecibelId";
                                connection.Execute(sqlUpdateVerificationCode, new { VerifiedOn = currentDate, DecibelId = registerModel.DecibelId }, transaction);

                                transaction.Commit();
                                res = new ResponseModel<object>() { data = null, statusMessage = "success", statusCode = HttpStatusCode.OK };
                            }
                            catch
                            {
                                transaction.Rollback();
                                throw;
                            }
                        }
                    }
                }

                return Json(res, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var res = new ResponseModel<object>() { data = null, message = ex.Message.ToString(), statusMessage = ex.ToString(), statusCode = HttpStatusCode.InternalServerError };
                return Json(res, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public async Task<JsonResult> RegisterUser(RegisterModel registerModel)
        {
            try
            {
                var res = new ResponseModel<object>();
                DataSet ds = await Utility.Utility.DecibelAuthentication(registerModel.DecibelId);

                if (ds.Tables[0].Columns.Count != 1)
                {
                    using (var connection = _dapperContext.CreateConnection())
                    {
                        // Check if user exists
                        var sqlCheckUser = "SELECT COUNT(1) FROM Users WHERE DecibelId = @DecibelId";
                        var userExists = connection.ExecuteScalar<int>(sqlCheckUser, new { DecibelId = registerModel.DecibelId }) > 0;

                        if (userExists)
                        {
                            res = new ResponseModel<object>() { data = null, statusMessage = "User already exists", statusCode = HttpStatusCode.BadRequest };
                        }
                        else
                        {
                            var EncryptedPassword = MD5_encode(registerModel.Password);

                            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                            {
                                // Insert new user
                                var sqlInsertUser = @"
                                INSERT INTO Users (
                                    FullName, PrimaryEmail, MobileNo, 
                                    DepartmentName, DecibelId, Password, LastLogin, CreatedOn, 
                                    Designation, SecondaryEmail, IsActive, IsDeleted, 
                                    IsAgreedTerms,  Grade, Manager_Code, Manager_Name, Manager_Email, UserType
                                ) VALUES (
                                    @FullName, @PrimaryEmail, @MobileNo,  
                                    @DepartmentName, @DecibelId, @Password, @LastLogin, @CreatedOn, 
                                    @Designation, @SecondaryEmail, @IsActive, @IsDeleted, 
                                    @IsAgreedTerms, @Grade, @Manager_Code, @Manager_Name, @Manager_Email, @UserType
                                )";

                                var grade = ds.Tables[i].Rows[0]["V_GRADE"].ToString(); // Get Grade value
                                string userType = DetermineUserType(grade); // Get UserType based on Grade


                                var newUser = new
                                {
                                    FullName = ds.Tables[i].Rows[0]["V_EMPLOYEE_NAME"].ToString(),
                                    PrimaryEmail = ds.Tables[i].Rows[0]["V_EMAIL"].ToString().ToLower(),
                                    MobileNo = ds.Tables[i].Rows[0]["V_PHONE_NUMBER"].ToString(),
                                    //MobileNo2 = ds.Tables[i].Rows[0]["V_PHONE_NUMBER"].ToString(),
                                    DepartmentName = ds.Tables[i].Rows[0]["V_DEPARTMENT"].ToString(),
                                    DecibelId = ds.Tables[i].Rows[0]["V_EMPLOYEE_ID"].ToString(),
                                    Password = EncryptedPassword,
                                    LastLogin = currentDate,
                                    CreatedOn = currentDate,
                                    Designation = ds.Tables[i].Rows[0]["V_DESIGNATION"].ToString(),
                                    SecondaryEmail = ds.Tables[i].Rows[0]["V_EMAIL"].ToString().ToLower(),
                                    IsActive = true,
                                    IsDeleted = false,
                                    IsAgreedTerms = false,
                                    //  Grade_ID = ds.Tables[i].Rows[0]["V_GRADE_ID"].ToString(),
                                    Grade = ds.Tables[i].Rows[0]["V_GRADE"].ToString(),
                                    Manager_Code = ds.Tables[i].Rows[0]["V_MANANGER_CODE"].ToString(),
                                    Manager_Name = ds.Tables[i].Rows[0]["V_MANAGER_NAME"].ToString(),
                                    Manager_Email = ds.Tables[i].Rows[0]["V_MANAGER_EMAIL"].ToString(),
                                    UserType = userType  // Add UserType value here
                                };

                                connection.Execute(sqlInsertUser, newUser);
                                res = new ResponseModel<object>() { data = null, statusMessage = "success", statusCode = HttpStatusCode.OK };
                            }
                        }
                    }
                    return Json(res, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    res = new ResponseModel<object>() { data = null, statusMessage = ds.Tables[0].Rows[0]["V_RESPONSE"].ToString(), statusCode = HttpStatusCode.BadRequest };
                    return Json(res, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                var res = new ResponseModel<object>() { data = null, message = ex.Message.ToString(), statusMessage = ex.ToString(), statusCode = HttpStatusCode.InternalServerError };
                return Json(res, JsonRequestBehavior.AllowGet);
            }
        }

        //// Helper method to determine UserType based on Grade
        //private string DetermineUserType(string grade)
        //{
        //    if (string.IsNullOrEmpty(grade))
        //    {
        //        return "Staff";  // Default to Staff if Grade is empty or null
        //    }

        //    string gradeUpper = grade.ToUpperInvariant();

        //    if (gradeUpper.Contains("NON-MANAGERIAL"))
        //    {
        //        return "Staff";
        //    }
        //    else if (gradeUpper.Contains("MANAGER"))
        //    {
        //        return "Executive";
        //    }
        //    else
        //    {
        //        return "Staff";  // Default to Staff if Grade does not match known types
        //    }
        //}

        //// Helper method to determine UserType based on Grade
        private string DetermineUserType(string grade)
        {
            if (string.IsNullOrEmpty(grade))
                return "Staff";  // Default to Staff if Grade is empty or null

            string gradeUpper = grade.Trim().ToUpperInvariant();

            if (gradeUpper == "NON-MANAGERIAL")
                return "Staff";
            else
                return "Executive";
        }

         // Login User

        [HttpGet]
        public ActionResult LoginUser(string redirect_url)
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> LoginUser(LoginModel loginModel)
        {
            try
            {
                DataSet ds = await Utility.Utility.DecibelAuthentication(loginModel.DecibelId);
                var res = new ResponseModel<object>();

                if (ds.Tables[0].Columns.Count != 1 || loginModel.DecibelId == "9943210")
                {
                    var EncryptedPassword = MD5_encode(loginModel.Password);

                    using (var connection = _dapperContext.CreateConnection())
                    {
                        // Find user by credentials
                        var sql = @"
                        SELECT * FROM Users 
                        WHERE DecibelId = @DecibelId 
                        AND Password = @Password 
                        AND IsActive = 1";

                        var user = connection.QueryFirstOrDefault<User>(sql, new { DecibelId = loginModel.DecibelId, Password = EncryptedPassword });

                        if (user != null)
                        {
                            // Remove any existing session
                            Session.Remove("BHMS_PortalSession");

                            // Dynamically determine UserType based on Grade
                            //string UserType;
                            //// logic
                            //if (!string.IsNullOrEmpty(user.Grade) &&
                            //    (user.Grade.Trim().ToUpper() == "MANAGERS" || user.Grade.Trim().ToUpper() == "MANAGERIAL"))
                            //    UserType = "Executive";
                            //else
                            //    UserType = "Staff"; // Default for NON-MANAGERIAL or others

                            string UserType = DetermineUserType(user.Grade);




                            // Set up your session model
                            BHMS_Portal_Session.UserId = user.Id;
                            BHMS_Portal_Session.FullName = user.FullName;
                            BHMS_Portal_Session.UserTypeId = user.UserTypeId;
                            BHMS_Portal_Session.UserType = UserType; // Set by Grade
                            BHMS_Portal_Session.Email = user.PrimaryEmail;
                            BHMS_Portal_Session.IsAdmin = user.IsAdmin == true;
                            BHMS_Portal_Session.IsAgreedTerms = user.IsAgreedTerms;
                            BHMS_Portal_Session.SessionId = Session.SessionID;
                            BHMS_Portal_Session.DecibelId = user.DecibelId;

                            // Add session to HttpContext
                            Session.Add("BHMS_PortalSession", BHMS_Portal_Session);


                            // Set authentication cookie for [Authorize] to work
                            System.Web.Security.FormsAuthentication.SetAuthCookie(user.PrimaryEmail, false);



                            res.statusMessage = "success";
                            res.statusCode = HttpStatusCode.OK;
                        }
                        else
                        {
                            res = new ResponseModel<object>() { data = null, statusMessage = "Invalid Credential", statusCode = HttpStatusCode.BadRequest };

                        }

                    }
                }
                else
                {

                    res = new ResponseModel<object>() { data = null, statusMessage = ds.Tables[0].Rows[0]["V_RESPONSE"].ToString(), statusCode = HttpStatusCode.BadRequest };

                }

                return Json(res, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {


                var res = new ResponseModel<object>() { data = null, message = ex.Message.ToString(), statusMessage = ex.ToString(), statusCode = HttpStatusCode.InternalServerError };
                return Json(res, JsonRequestBehavior.AllowGet);


            }
        }

        // Forgot Password


        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> ResetPasswordRequest(RegisterModel model)
        {
            try
            {
                var res = new ResponseModel<object>();
                DataSet ds = await Utility.Utility.DecibelAuthentication(model.DecibelId);

                if (ds.Tables[0].Columns.Count != 1)
                {
                    using (var connection = _dapperContext.CreateConnection())
                    {
                        for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                        {
                            string AppBaseUrl = Request.Url.Scheme + "://" + Request.Url.Host + ":" + Request.Url.Port;
                            string code = Guid.NewGuid().ToString();

                            EmailDataModel emailData = new EmailDataModel()
                            {
                                EmailToSend = ds.Tables[i].Rows[0]["V_EMAIL"].ToString(),
                                DisplayName = "Reset Password",
                                SenderName = "BHMS Portal App",
                                VerificationCode = code,
                                VerifyLink = AppBaseUrl + "/Account/ResetPasswordLink?v_fcode=" + code
                            };

                            // Get email template from database
                            var sqlGetTemplate = "SELECT * FROM EmailNotificationTypes WHERE EmailType = @EmailType";
                            var template = connection.QueryFirstOrDefault<EmailNotificationType>(sqlGetTemplate, new { EmailType = "Reset Password" });

                            string url = Regex.Replace(template.EmailTemplate, "#resetpassword", emailData.VerifyLink);
                            await Utility.Utility.SendEmail(ds.Tables[i].Rows[0]["V_EMAIL"].ToString() + "", "Reset Password", url);

                            // Insert verification code
                            var sqlInsertCode = @"
                            INSERT INTO VerificationCodes (Code, DecibelId, VerificationType, CreatedOn)
                            VALUES (@Code, @DecibelId, @VerificationType, @CreatedOn)";

                            connection.Execute(sqlInsertCode, new
                            {
                                Code = code,
                                DecibelId = ds.Tables[i].Rows[0]["V_EMPLOYEE_ID"].ToString(),
                                VerificationType = "Reset Password Link",
                                CreatedOn = currentDate
                            });

                            res = new ResponseModel<object>() { data = null, statusMessage = "Verification Email Sent Successfully", statusCode = HttpStatusCode.OK };
                            return Json(res, JsonRequestBehavior.AllowGet);
                        }
                    }
                }
                else
                {
                    res = new ResponseModel<object>() { data = null, statusMessage = ds.Tables[0].Rows[0]["V_RESPONSE"].ToString(), statusCode = HttpStatusCode.BadRequest };
                    return Json(res, JsonRequestBehavior.AllowGet);
                }

                return Json(res, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var res = new ResponseModel<object>() { data = null, message = ex.Message.ToString(), statusMessage = ex.ToString(), statusCode = HttpStatusCode.InternalServerError };
                return Json(res, JsonRequestBehavior.AllowGet);
            }
        }



        // Verify User

        [HttpGet]
        public ActionResult VerifyUser()
        {
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> VerifyUser(RegisterModel model)
        {
            try
            {
                var res = new ResponseModel<object>();
                DataSet ds = await Utility.Utility.DecibelAuthentication(model.DecibelId);

                if (ds.Tables[0].Columns.Count != 1)
                {
                    using (var connection = _dapperContext.CreateConnection())
                    {
                        for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                        {
                            string AppBaseUrl = Request.Url.Scheme + "://" + Request.Url.Host + ":" + Request.Url.Port;
                            string code = Guid.NewGuid().ToString();

                            if (string.IsNullOrEmpty(ds.Tables[i].Rows[0]["V_EMAIL"].ToString().Trim()))
                            {
                                var resemail = new ResponseModel<object>() { data = null, message = "Email not found contact to HR department", statusMessage = "Email not found contact to HR department", statusCode = HttpStatusCode.OK };
                                return Json(resemail, JsonRequestBehavior.AllowGet);
                            }

                            EmailDataModel emailData = new EmailDataModel()
                            {

                                EmailToSend = ds.Tables[i].Rows[0]["V_EMAIL"].ToString(),
                                DisplayName = "Verify User",
                                SenderName = "BHMS Portal App",
                                VerificationCode = code,
                                VerifyLink = AppBaseUrl + "/Account/VerifyEmail?v_fcode=" + code
                            };

                            // Get email template from database
                            var sqlGetTemplate = "SELECT * FROM EmailNotificationTypes WHERE EmailType = @EmailType";
                            var template = connection.QueryFirstOrDefault<EmailNotificationType>(sqlGetTemplate, new { EmailType = "Activation Link" });

                            string emailbody = Regex.Replace(template.EmailTemplate, "#activation", emailData.VerifyLink);
                            await Utility.Utility.SendEmail(ds.Tables[i].Rows[0]["V_EMAIL"].ToString(), "Activation Link", emailbody);

                            // Insert verification code
                            var sqlInsertCode = @"
                            INSERT INTO VerificationCodes (Code, DecibelId, VerificationType, CreatedOn)
                            VALUES (@Code, @DecibelId, @VerificationType, @CreatedOn)";

                            connection.Execute(sqlInsertCode, new
                            {
                                Code = code,
                                DecibelId = ds.Tables[i].Rows[0]["V_EMPLOYEE_ID"].ToString(),
                                VerificationType = "Activation Link",
                                CreatedOn = currentDate
                            });

                            res = new ResponseModel<object>() { data = null, statusMessage = ds.Tables[i].Rows[0]["V_EMAIL"].ToString(), statusCode = HttpStatusCode.OK };
                            return Json(res, JsonRequestBehavior.AllowGet);
                        }
                    }
                }
                else
                {
                    res = new ResponseModel<object>() { data = null, statusMessage = ds.Tables[0].Rows[0]["V_RESPONSE"].ToString(), statusCode = HttpStatusCode.BadRequest };
                    return Json(res, JsonRequestBehavior.AllowGet);
                }

                return Json(res, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var res = new ResponseModel<object>() { data = null, message = ex.Message.ToString(), statusMessage = ex.ToString(), statusCode = HttpStatusCode.InternalServerError };
                return Json(res, JsonRequestBehavior.AllowGet);
            }
        }
          
        // User Not Found

        [HttpGet]
        public ActionResult UserNotFount()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult VerifyEmail(string v_fcode)
        {
            try
            {
                var res = new ResponseModel<object>();

                using (var connection = _dapperContext.CreateConnection())
                {
                    // Check if verification code exists
                    var sqlCheckCode = "SELECT COUNT(1) FROM VerificationCodes WHERE Code = @Code";
                    var isFound = connection.ExecuteScalar<int>(sqlCheckCode, new { Code = v_fcode });

                    if (isFound == 0)
                    {
                        return RedirectToAction("UserNotFount");
                    }
                    else
                    {
                        // Update verification code
                        var sqlUpdateCode = @"
                        UPDATE VerificationCodes 
                        SET IsVerified = 1, VerifiedOn = @VerifiedOn 
                        WHERE Code = @Code";

                        connection.Execute(sqlUpdateCode, new { VerifiedOn = currentDate, Code = v_fcode });
                    }
                }

                return RedirectToAction("RegisterUser", "Account", new { v_fcode = v_fcode });
            }
            catch (Exception ex)
            {
                var res = new ResponseModel<object>() { data = null, message = ex.Message.ToString(), statusMessage = ex.ToString(), statusCode = HttpStatusCode.InternalServerError };
                return Json(res, JsonRequestBehavior.AllowGet);
            }
        }



        //Reset Password

        [AllowAnonymous]
        public ActionResult ResetPasswordLink(string v_fcode)
        {
            try
            {
                var res = new ResponseModel<object>();

                using (var connection = _dapperContext.CreateConnection())
                {
                    // Check if verification code exists
                    var sqlCheckCode = "SELECT COUNT(1) FROM VerificationCodes WHERE Code = @Code";
                    var isFound = connection.ExecuteScalar<int>(sqlCheckCode, new { Code = v_fcode });

                    if (isFound == 0)
                    {
                        return RedirectToAction("UserNotFount");
                    }
                    else
                    {
                        // Update verification code
                        var sqlUpdateCode = @"
                        UPDATE VerificationCodes 
                        SET IsVerified = 1, VerifiedOn = @VerifiedOn 
                        WHERE Code = @Code";

                        connection.Execute(sqlUpdateCode, new { VerifiedOn = currentDate, Code = v_fcode });
                    }
                }

                return RedirectToAction("ResetPassword", "Account", new { v_fcode = v_fcode });
            }
            catch (Exception ex)
            {
                var res = new ResponseModel<object>() { data = null, message = ex.Message.ToString(), statusMessage = ex.ToString(), statusCode = HttpStatusCode.InternalServerError };
                return Json(res, JsonRequestBehavior.AllowGet);
            }
        }



        // Logout 
        public ActionResult LogOut()
        {
            Session.Remove("BHMS_PortalSession");
            FormsAuthentication.SignOut();
            return RedirectToAction("LoginUser", "Account");
        }

        public string MD5_encode(string str_encode)
        {
            MD5 md5Hash = MD5.Create();
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(str_encode));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }
    }
}
