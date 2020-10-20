using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using JamboPayInterview.Models;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace JamboPayInterview.Controllers
{
    public class HomeController : Controller
    {

        ///uncomment this while publishing on live server

        // public static string Baseurl = ConfigurationManager.AppSettings["API_SERVER_URL"];

        ///uncomment this while publishing on live server

        ///for use on localhost testings

        public static string Baseurl = ConfigurationManager.AppSettings["API_LOCALHOST_URL"];

        ///for use on localhost testings

        /// API Authentications
        public static string ApiUsername = ConfigurationManager.AppSettings["API_USERNAME"];
        public static string ApiPassword = ConfigurationManager.AppSettings["API_PWD"];
        /// API Authentications
       
        /// MysqlConnection String define
        public static readonly string conString = @"datasource=localhost;port=3306;username=root;password=root;database=jambopay";
        public static readonly MySqlConnection connection  = new MySqlConnection("datasource=localhost;port=3306;username=root;password=root;database=jambopay");
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Index_supporter()
        {
            return View();
        }

        public ActionResult Register_User()
        {
            return View();
        }

        public ActionResult Post_Transactions()
        {
            return View();
        }

        public ActionResult View_Transactions()
        {
            WebClient wc = new WebClient();
            wc.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(ApiUsername + ":" + ApiPassword)));
            string json = wc.DownloadString(Baseurl + "api/GetPostedTransactions");
            var  trx = JsonConvert.DeserializeObject<List<TransactionsModel>>(json);
            var jsresult = (from a in trx select a).ToList();
            return View(jsresult);
        }

        public ActionResult Login()
        {
            connection.Close();
            return View();
        }

        public ActionResult Recruit_Supporter()
        {
            connection.Close();
            return View();
        }
        

        [HttpPost]
        [AllowAnonymous]
        public JsonResult CalculateCommision(CommissionModel commissionmodel)
        {
            try
            {
                var totalCommission = (dynamic)null;

                if (string.IsNullOrWhiteSpace(commissionmodel.ServiceFee.ToString(CultureInfo.InvariantCulture)))
                    return Json("lsvcFeeEmpty", JsonRequestBehavior.AllowGet);
               
                decimal srvComm = Decimal.Parse(commissionmodel.ServiceTypeCommision);
                decimal srvFee = Convert.ToDecimal(commissionmodel.ServiceFee);
                totalCommission = (srvComm / 100) * srvFee;
                return Json("success*"+totalCommission, JsonRequestBehavior.AllowGet);
            }
            catch (MySqlException ex)
            {
                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }
           
        }

        [HttpPost]
        [AllowAnonymous]
        public JsonResult PostTransaction(CommissionModel commissionmodel)
        {
            try
            {
                var totalCommission = (dynamic)null;
                var ambassadorId = (dynamic)null;
                var supporterId = Session["email"] as string;

                decimal srvComm = Decimal.Parse(commissionmodel.ServiceTypeCommision);
                decimal srvFee = Convert.ToDecimal(commissionmodel.ServiceFee);
                totalCommission = (srvComm / 100) * srvFee;

                if (string.IsNullOrWhiteSpace(commissionmodel.ServiceFee.ToString(CultureInfo.InvariantCulture)))
                    return Json("lsvcFeeEmpty", JsonRequestBehavior.AllowGet);

                using (MySqlConnection con = new MySqlConnection(conString))
                {

                    con.Open();
                    MySqlCommand command = con.CreateCommand();
                    command.CommandType = CommandType.Text;
                    command.CommandText = "SELECT * FROM supporters WHERE supporter_id= '" + supporterId + "' ";
                    command.ExecuteNonQuery();
                    DataTable dt = new DataTable();
                    MySqlDataAdapter da = new MySqlDataAdapter(command);
                    da.Fill(dt);
                   
                    foreach (DataRow dr in dt.Rows)
                    {
                        //get all ambassadors the supporter represents.
                        ambassadorId = dr["ambassador_id"] as string;

                        string insertQry =
                            "INSERT INTO transactions(supporter_id, ambassador_id, transaction_cost, ambassador_commission ) VALUES('" +
                            supporterId + "', '" + ambassadorId + "', '" + srvFee + "', '" +totalCommission + "' )";
                       
                        MySqlCommand command2 = new MySqlCommand(insertQry, con);
                        if (command2.ExecuteNonQuery() == 1)
                        {
                            return Json("success", JsonRequestBehavior.AllowGet);
                        }
                    }
                    con.Close();
                }
               
                return Json("error", JsonRequestBehavior.AllowGet);
            }
            catch (MySqlException ex)
            {
                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }

        }

        [HttpPost]
        [AllowAnonymous]
        public JsonResult RegisterAmbassador(SignUpModel vendormodel)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(vendormodel.FName))
                    return Json("fnameEmpty", JsonRequestBehavior.AllowGet);

                if (string.IsNullOrWhiteSpace(vendormodel.LName))
                    return Json("lnameEmpty", JsonRequestBehavior.AllowGet);

                if (string.IsNullOrWhiteSpace(vendormodel.Phonenumber))
                    return Json("phonenoEmpty", JsonRequestBehavior.AllowGet);

                if (string.IsNullOrWhiteSpace(vendormodel.IdNumber))
                    return Json("IDEmpty", JsonRequestBehavior.AllowGet);

                if (string.IsNullOrWhiteSpace(vendormodel.Email))
                    return Json("EmailEmpty", JsonRequestBehavior.AllowGet);

                if (string.IsNullOrWhiteSpace(vendormodel.Password1))
                    return Json("PasswordEmpty", JsonRequestBehavior.AllowGet);

                if (string.IsNullOrWhiteSpace(vendormodel.Password2))
                    return Json("Password2Empty", JsonRequestBehavior.AllowGet);

                if (vendormodel.Password1 != vendormodel.Password2)
                    return Json("PasswordMismatched", JsonRequestBehavior.AllowGet);

                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    string insertQry =
                        "INSERT INTO users(fname, lname, phone_no, id_number, user_type, email, password) VALUES('" +
                        vendormodel.FName + "', '" + vendormodel.LName + "', '" + vendormodel.Phonenumber + "', '" +
                        vendormodel.IdNumber + "', 'ambassador', '" + vendormodel.Email + "', '" +
                        EncryptP(vendormodel.Password2) + "' )";

                    con.Open();
                    MySqlCommand command = new MySqlCommand(insertQry, con);
                    if (command.ExecuteNonQuery() == 1)
                    {
                        return Json("success", JsonRequestBehavior.AllowGet);
                    }
                    con.Close();
                    return Json("error", JsonRequestBehavior.AllowGet);
                }
            }
            catch (MySqlException ex)
            {
                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        [AllowAnonymous]
        public JsonResult RegisterSupporter(SignUpModel vendormodel)
        {
            try
            {
                var ambassadorId = Session["email"];

                if (string.IsNullOrWhiteSpace(vendormodel.FName))
                    return Json("fnameEmpty", JsonRequestBehavior.AllowGet);

                if (string.IsNullOrWhiteSpace(vendormodel.LName))
                    return Json("lnameEmpty", JsonRequestBehavior.AllowGet);

                if (string.IsNullOrWhiteSpace(vendormodel.Phonenumber))
                    return Json("phonenoEmpty", JsonRequestBehavior.AllowGet);

                if (string.IsNullOrWhiteSpace(vendormodel.IdNumber))
                    return Json("IDEmpty", JsonRequestBehavior.AllowGet);

                if (string.IsNullOrWhiteSpace(vendormodel.Email))
                    return Json("EmailEmpty", JsonRequestBehavior.AllowGet);

                if (string.IsNullOrWhiteSpace(vendormodel.Password1))
                    return Json("PasswordEmpty", JsonRequestBehavior.AllowGet);

                if (string.IsNullOrWhiteSpace(vendormodel.Password2))
                    return Json("Password2Empty", JsonRequestBehavior.AllowGet);

                if (vendormodel.Password1 != vendormodel.Password2)
                    return Json("PasswordMismatched", JsonRequestBehavior.AllowGet);

                using (MySqlConnection con = new MySqlConnection(conString))
                {
                   string insertQry =
                        "INSERT INTO users(fname, lname, phone_no, id_number, user_type, email, password) VALUES('" +
                        vendormodel.FName + "', '" + vendormodel.LName + "', '" + vendormodel.Phonenumber + "', '" +
                        vendormodel.IdNumber + "', 'supporter', '" + vendormodel.Email + "', '" +
                        EncryptP(vendormodel.Password2) + "' )";

                    con.Open();
                    string checkifExists = "SELECT * FROM supporters WHERE supporter_id = '" + vendormodel.Email + "' AND ambassador_id = '" + ambassadorId + "'  LIMIT 1";

                    MySqlCommand command0 = new MySqlCommand(checkifExists, con);
                    if (command0.ExecuteNonQuery() == 1)
                    {
                        return Json("Already Exists under supporters!", JsonRequestBehavior.AllowGet);
                    }
                    MySqlCommand command = new MySqlCommand(insertQry, con);
                    if (command.ExecuteNonQuery() == 1)
                    {
                        con.Open();
                        string insertQry2 = "INSERT INTO supporters(ambassador_id, supporter_id) VALUES('" +
                                            ambassadorId + "','" + vendormodel.Email + "' )";

                        MySqlCommand command2 = new MySqlCommand(insertQry2, con);
                        if (command2.ExecuteNonQuery() == 1)
                        {
                            return Json("success", JsonRequestBehavior.AllowGet);
                        }
                        con.Close();
                    }
                    con.Close();
                }
                return Json("error", JsonRequestBehavior.AllowGet);
            }
            catch (MySqlException ex)
            {
                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public JsonResult CalculateCommisionBalance()
        {
            try
            {
                var totalCommission = (dynamic)null;
                var ambassadorId = Session["email"] as string;
                WebClient wc = new WebClient();
                wc.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(ApiUsername + ":" + ApiPassword)));
                string json = wc.DownloadString(Baseurl + "api/GetPostedTransactions");
                var trx = JsonConvert.DeserializeObject<List<TransactionsModel>>(json);
                var jsresult = (from a in trx where a.ambassador_id == ambassadorId select a.ambassador_commission).ToList();
                totalCommission = jsresult.Sum();
                return Json("success*" + totalCommission, JsonRequestBehavior.AllowGet);
            }
            catch (MySqlException ex)
            {
                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }

        }

        [HttpPost]
        [AllowAnonymous]
        public JsonResult CheckLogin(string myUsername, string myPassword)
        {
            try
            {

                if (string.IsNullOrWhiteSpace(myUsername))
                    return Json("UsernameEmpty", JsonRequestBehavior.AllowGet);
                if (string.IsNullOrWhiteSpace(myPassword))
                    return Json("PasswordEmpty", JsonRequestBehavior.AllowGet);
                
                WebClient wc = new WebClient();
                wc.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(ApiUsername + ":" + ApiPassword)));

                //get Total commission
                string json = wc.DownloadString(Baseurl + "api/GetPostedTransactions");
                var trx = JsonConvert.DeserializeObject<List<TransactionsModel>>(json);
                var jsresult = (from a in trx where a.ambassador_id == myUsername select a.ambassador_commission).ToList();
                Session["totalCommission"]  = jsresult.Sum();

                //get number of supporters
                string json2 = wc.DownloadString(Baseurl + "api/GetSupporters");
                var sups = JsonConvert.DeserializeObject<List<SupporterModel>>(json2);
                var jsresult2 = (from a in sups where a.ambassador_id == myUsername select a).ToList();
                Session["totalSupporters"] = jsresult2.Count();

                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();
                    MySqlCommand command = con.CreateCommand();
                    command.CommandType = CommandType.Text;
                    command.CommandText = "SELECT * FROM users WHERE email= '" + myUsername + "' AND password = '" +
                                          EncryptP(myPassword) + "' ";
                    command.ExecuteNonQuery();
                    DataTable dt = new DataTable();
                    MySqlDataAdapter da = new MySqlDataAdapter(command);
                    da.Fill(dt);

                    foreach (DataRow dr in dt.Rows)
                    {
                        //assign variables the way you want.
                        Session["uid"] = dr["u_id"] as string;
                        Session["fname"] = dr["fname"] as string;
                        Session["lname"] = dr["lname"] as string;
                        Session["mobileno"] = dr["phone_no"] as string;
                        Session["idnumber"] = dr["id_number"] as string;
                        Session["usertype"] = dr["user_type"] as string;
                        Session["email"] = dr["email"] as string;
                        if ((string) Session["usertype"] == "ambassador")
                        {
                            return Json("Loginambassador", JsonRequestBehavior.AllowGet);
                        }
                        return Json("Loginsupporter", JsonRequestBehavior.AllowGet);
                    }

                    con.Close();
                }
                //wrong credentials
                return Json("wrongcredentials", JsonRequestBehavior.AllowGet);
               
            }
            catch (Exception ex)
            {
                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult CheckLogout()
        {
            Session.RemoveAll();
            Session.Clear();
            Session.Abandon();
            Response.AppendHeader("Cache-Control", "no-store");
            Response.Cookies.Add(new HttpCookie("ASP.NET_SessionId", ""));
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Home");
        }

        static string EncryptP(string mypass)
        {
            //encryptpassword:
            using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
            {
                UTF8Encoding utf8 = new UTF8Encoding();
                byte[] data = md5.ComputeHash(utf8.GetBytes(mypass));
                return Convert.ToBase64String(data);
            }
        }

    }
}