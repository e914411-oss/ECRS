using ECRS_WEB.Models;
using ECRS_WEB.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;


namespace ECRS_WEB.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthApiClient _api;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AuthApiClient api, ILogger<AuthController> logger)
        {
            _api = api;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            _logger.LogInformation("進入 Auth/Login(GET)，ReturnUrl={ReturnUrl}", returnUrl);

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpGet]
        public IActionResult Denied()
        {
            _logger.LogWarning("使用者進入 Auth/Denied，User={User}", User?.Identity?.Name);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null, CancellationToken ct = default)
        {
            ViewBag.ReturnUrl = returnUrl;

            _logger.LogInformation("開始執行 Auth/Login(POST)，UserName={UserName}，ReturnUrl={ReturnUrl}",
                vm?.UserName,
                returnUrl);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("登入模型驗證失敗，UserName={UserName}", vm?.UserName);
                return View(vm);
            }

            try
            {
                var apiResp = await _api.LoginAsync(new LoginRequest
                {
                    UserId = vm.UserName,
                    Password = vm.Password
                }, ct);

                if (!apiResp.Success || string.IsNullOrWhiteSpace(apiResp.Token))
                {
                    _logger.LogWarning("登入失敗，UserName={UserName}，ApiMessage={ApiMessage}",
                        vm.UserName,
                        apiResp.Message);

                    ModelState.AddModelError(string.Empty, apiResp.Message);
                    return View(vm);
                }

                HttpContext.Session.SetString("AuthToken", apiResp.Token);
                var displayName = apiResp.User?.DisplayName ?? apiResp.User?.UserName ?? vm.UserName;
                HttpContext.Session.SetString("DisplayName", displayName);

                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, apiResp.User?.UserName ?? vm.UserName),
            new Claim(ClaimTypes.Name, displayName),
            new Claim("access_token", apiResp.Token)
        };

                var identity = new ClaimsIdentity(claims, "AppCookie");
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    scheme: "AppCookie",
                    principal: principal,
                    properties: new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
                    });

                TempData["LoginSuccess"] = "登入成功";
                ViewBag.loginTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

                _logger.LogInformation("登入成功，UserName={UserName}，DisplayName={DisplayName}，ReturnUrl={ReturnUrl}",
                    vm.UserName,
                    displayName,
                    returnUrl);

                return RedirectToAction("AfterLogin", "Entry", new { returnUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登入流程發生例外，UserName={UserName}，ReturnUrl={ReturnUrl}",
                    vm?.UserName,
                    returnUrl);

                ModelState.AddModelError(string.Empty, "登入時發生系統錯誤，請稍後再試。");
                return View(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userName = User?.Identity?.Name;

            _logger.LogInformation("開始登出，User={User}", userName);

            await HttpContext.SignOutAsync("AppCookie");
            HttpContext.Session.Remove("AuthToken");
            HttpContext.Session.Remove("DisplayName");
            TempData["LoginSuccess"] = null;

            _logger.LogInformation("登出完成，User={User}", userName);

            return RedirectToAction("Index", "Entry");
        }
    }
}

