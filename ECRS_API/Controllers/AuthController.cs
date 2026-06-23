using System;
using System.Diagnostics;
using ECRS_API.Data;
using ECRS_API.DTOs;
using ECRS_API.DTOs.Security;
using ECRS_API.Models;
using ECRS_API.Models.ECRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreAPI.Controllers
{
    [ApiController]
    [Route("Api/[controller]")]
    public class AuthController : ControllerBase
    {
        //private readonly ECRSDbContext _db;
        private readonly PMDSDbContext _db;
        private readonly JwtTokenService _jwt;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;
        private readonly string _hashKey;



        public AuthController(PMDSDbContext db, JwtTokenService jwt, IConfiguration config, ILogger<AuthController> logger)
        {
            //Console.WriteLine("Login API entered");

            _db = db;
            _jwt = jwt;
            _config = config;
            _logger = logger;

            // 建議：用 _config["Key"] 取值
            _hashKey = _config["CipherTool_Key"];
            if (string.IsNullOrWhiteSpace(_hashKey))
            {
                // 你原本的 fallback（正式環境建議改用環境變數或 Secret Manager）
                //_hashKey = "G86AABN5448E62FY364D24W7";

                // Production 建議直接 throw，避免默默用硬編碼 key
                throw new InvalidOperationException("CipherTool_Key is missing...");
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req)
        {
            try
            {
                // 1) req 可能為 null：先擋掉避免 500
                if (req == null)
                {
                    return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "Request body 不可空白"));
                }
                if (string.IsNullOrWhiteSpace(req.UserId) || string.IsNullOrWhiteSpace(req.Password))
                {
                    return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "Username / Password 不可空白"));
                }

                //補Log
                _logger.LogInformation("Login start. UserId={UserId}", req.UserId);
                var sw = Stopwatch.StartNew();

                var user = await _db.系統_使用者資料表s
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.使用者帳號 == req.UserId);

                _logger.LogInformation("Query user done. Elapsed={Elapsed}ms", sw.ElapsedMilliseconds);


                if (user is null)
                {
                    _logger.LogInformation(null, "帳號錯誤");
                    return Unauthorized(ApiResponse<object>.Fail("INVALID_CREDENTIALS", "帳號或密碼錯誤"));
                }
                // 防呆：DB 密碼欄位若為 null/空，Verify 很可能拋例外或永遠失敗
                if (string.IsNullOrWhiteSpace(user.密碼))
                {
                    _logger.LogInformation(null, "密碼錯誤");
                    return Unauthorized(ApiResponse<object>.Fail("INVALID_CREDENTIALS", "帳號或密碼錯誤"));
                }

                var APUser = new AppUser
                {
                    Id = string.IsNullOrEmpty(user.使用者帳號) ? req.UserId : user.使用者帳號.ToString(),
                    InspectionId = user.使用者編號,
                    UserName = user.姓名,
                    DisplayName = user.姓名,
                    PasswordHash = user.密碼,
                    InspectionLocation = user.所屬單位縣市編號,
                    LastLoginAtUtc = DateTime.UtcNow
                };

                // 2) 修正驗證：通常 Verify 應該吃「明文 + DB hash + key」
                var ok = PasswordHasher.Verify(req.Password, APUser.PasswordHash, _hashKey);
                if (!ok)
                {
                    return Unauthorized(ApiResponse<object>.Fail("INVALID_CREDENTIALS", "帳號或密碼錯誤(HASH)"));
                }
                _logger.LogInformation("Verify done. Elapsed={Elapsed}ms", sw.ElapsedMilliseconds);

                var (token, expiresAtUtc) = _jwt.CreateToken(APUser);

                var resp = new LoginResponse
                {
                    Token = token,
                    ExpiresAtUtc = expiresAtUtc,
                    Success = (bool)ok,
                    User = new UserProfile
                    {
                        Id = APUser.Id,
                        InspectionId = APUser.InspectionId,
                        Username = APUser.UserName,
                        DisplayName = APUser.DisplayName,
                        InspectionLocation = APUser.InspectionLocation
                    }
                };
                _logger.LogInformation("Token created. Elapsed={Elapsed}ms", sw.ElapsedMilliseconds);

                return Ok(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed. UserId={UserId}", req?.UserId);
                return StatusCode(500, ApiResponse<object>.Fail("SERVER_ERROR", "系統發生錯誤，請聯絡系統管理員"));
            }
        }




    }
}
