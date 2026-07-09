using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Net;
using ECRS_WEB.Services;
using Microsoft.Extensions.Logging;

namespace ECRS_WEB.Controllers
{
    public class EntryController : Controller
    {
        private string _LoginUrl = string.Empty;
        private readonly AuthApiClient _authApi;
        private readonly GspOAuthClient _gsp;

        private readonly IDeviceDetector _device;
        private readonly ILogger<EntryController> _logger;


        public EntryController(AuthApiClient authApi, GspOAuthClient gsp, IDeviceDetector device, ILogger<EntryController> logger)
        {
            _authApi = authApi;
            _gsp = gsp;
            _device = device;
            _logger = logger;
        }

        public IActionResult Index()
        {
            _logger.LogInformation("進入 Entry/Index，IP={Ip}", HttpContext.Connection.RemoteIpAddress?.ToString());

            // 將資料帶到 View
            ViewBag.Message = @"
這裡放三個登入方式，由左至右分別為：
1.	使用者帳號密碼登入：AD登入頁面，可輸入署內AD帳密進行登入電子稽查紀錄系統。
2.	E政府帳號登入：E政府一般帳密登入頁面，可透過登入E政府完成登入後，導回電子稽查紀錄系統。
3.	實體自然人憑證登入：E政府實體自然人憑證登入頁面，可透過登入E政府實體自然人憑證後，導回電子稽查紀錄系統。
4.	行動自然人憑證登入：E政府行動自然人憑證登入頁面，可透過登入E政府行動自然人憑證後，導回電子稽查紀錄系統。
5.	醫事憑證登入：E政府醫事憑證登入頁面，可透過登入E政府醫事憑證後，導回電子稽查紀錄系統。
";
            return View();
        }

        [Authorize]
        [HttpGet]
        public IActionResult AfterLogin(string? returnUrl = null)
        {
            _logger.LogInformation("進入 Entry/AfterLogin，User={User}，ReturnUrl={ReturnUrl}",
                User?.Identity?.Name,
                returnUrl);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                _logger.LogInformation("AfterLogin 導向本地 returnUrl：{ReturnUrl}", returnUrl);
                return Redirect(returnUrl);
            }

            const string CookieName = "pmds_layout_mode";

            var cookieMode = Request.Cookies[CookieName];
            string mode;

            if (string.Equals(cookieMode, "Mobile", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(cookieMode, "Desktop", StringComparison.OrdinalIgnoreCase))
            {
                mode = cookieMode!;
                _logger.LogInformation("AfterLogin 使用 Cookie 指定版型，Mode={Mode}", mode);
            }
            else
            {
                var isMobile = _device.IsMobile(HttpContext);
                mode = isMobile ? "Mobile" : "Desktop";
                _logger.LogInformation("AfterLogin 自動判斷版型，IsMobile={IsMobile}，Mode={Mode}", isMobile, mode);
            }

            HttpContext.Session.SetString("LayoutMode", mode);

            _logger.LogInformation("AfterLogin 完成，導向 Inspection/Index，User={User}，Mode={Mode}",
                User?.Identity?.Name,
                mode);

            return RedirectToAction("Index", "Inspection");
        }


        //E政府帳號登入驗證
        [HttpPost]
        public IActionResult LoginB()
        {
            var state = Guid.NewGuid().ToString("N");
            HttpContext.Session.SetString("EgovLoginBState", state);

            var myCallback = Url.Action(
                action: "LoginBCallback",
                controller: "Entry",
                values: new { state },
                protocol: Request.Scheme
            ) ?? string.Empty;

            var loginUrl = "https://www.cp.gov.tw/portal/Clogin.aspx"
                + "?ver=Simple"
                + "&Level=1"
                + "&ReturnUrl=" + WebUtility.UrlEncode(myCallback);

            _logger.LogInformation("啟動 E 政府登入導頁，State={State}，Callback={CallbackUrl}", state, myCallback);

            return Redirect(loginUrl);
        }

        //自然人憑證登入驗證
        [HttpPost]
        public IActionResult LoginC()
        {
            _LoginUrl = "";
            return View();
        }

        //醫事憑證登入驗證
        [HttpPost]
        public IActionResult LoginD()
        {
            _LoginUrl = "";
            return View();
        }



        // 這個 action 是「你系統的 callback」
        // 外部成功後，你要讓它回到：/Entry/LoginBCallback?code=...&ver=...&Level=...
        [HttpGet]
        [AllowAnonymous]
        public Task<IActionResult> LoginBCallback(string state, CancellationToken ct)
        {
            _logger.LogInformation("收到 LoginBCallback，State={State}", state);

            var myOauthCallback = Url.Action(
                action: "LoginBOAuthCallback",
                controller: "Entry",
                values: new { state },
                protocol: Request.Scheme
            ) ?? string.Empty;

            var gspUrl =
                "https://gsp.fda.gov.tw/oAuth/"
                + "?code=pmds"
                + "&ver=Simple"
                + "&Level=1"
                + "&ReturnUrl=" + WebUtility.UrlEncode(myOauthCallback);

            _logger.LogInformation("轉導到 GSP OAuth，State={State}，OAuthCallback={OAuthCallback}", state, myOauthCallback);

            return Task.FromResult<IActionResult>(Redirect(gspUrl));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult LoginBOAuthCallback(string state, string token, string access_token, string code)
        {
            state = WebUtility.UrlDecode(state ?? string.Empty);
            token = WebUtility.UrlDecode(token ?? string.Empty);
            access_token = WebUtility.UrlDecode(access_token ?? string.Empty);
            code = WebUtility.UrlDecode(code ?? string.Empty);

            var rawQs = HttpContext.Request.QueryString.Value;
            var fullUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}";
            var expected = HttpContext.Session.GetString("EgovLoginBState");

            _logger.LogInformation("收到 LoginBOAuthCallback，State={State}，HasToken={HasToken}，HasAccessToken={HasAccessToken}，HasCode={HasCode}，Url={FullUrl}",
                state,
                !string.IsNullOrWhiteSpace(token),
                !string.IsNullOrWhiteSpace(access_token),
                !string.IsNullOrWhiteSpace(code),
                fullUrl);

            if (string.IsNullOrWhiteSpace(state) || expected != state)
            {
                _logger.LogWarning("LoginBOAuthCallback 驗證失敗，State={State}，ExpectedState={ExpectedState}，RawQuery={RawQuery}",
                    state,
                    expected,
                    rawQs);

                return Unauthorized("Invalid state");
            }

            var finalToken = token ?? access_token;

            if (!string.IsNullOrWhiteSpace(finalToken))
            {
                _logger.LogInformation("LoginBOAuthCallback 成功取得 Token，State={State}", state);

                return RedirectToAction("Index", "Entry");
            }

            if (!string.IsNullOrWhiteSpace(code))
            {
                _logger.LogInformation("LoginBOAuthCallback 收到授權碼 Code，State={State}，Code={Code}", state, code);

                return RedirectToAction("Index", "Home");
            }

            _logger.LogWarning("LoginBOAuthCallback 未收到 token/code，State={State}，RawQuery={RawQuery}", state, rawQs);

            return BadRequest("No token/code returned from GSP.");
        }


    }




    //// POST: /Hello/Greet
    //[HttpPost]
    //public IActionResult Greet(string name)
    //{
    //    // 這裡示範最簡單的表單接收（先不用 ViewModel）
    //    if (string.IsNullOrWhiteSpace(name))
    //    {
    //        ViewBag.Result = "你沒有輸入名字。";
    //    }
    //    else
    //    {
    //        ViewBag.Result = $"你好，{name}！歡迎進入 ASP.NET Core MVC。";
    //    }

    //    // 重用同一個 View 顯示結果
    //    ViewBag.Message = "這是我的第一個 MVC 網頁（.NET 8）";
    //    return View("Index");
    //}
}

