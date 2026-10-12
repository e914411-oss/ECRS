using NLog.Web;
using ECRS_API.Data;
using ECRS_API.DTOs.Security;
using ECRS_API.Infrastructure.Routing;
using ECRS_API.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;
using NLog;


var logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config", optional: true).GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // 只使用 appsettings.json，避免輸出目錄中舊的環境設定覆蓋連線字串。
    for (var i = builder.Configuration.Sources.Count - 1; i >= 0; i--)
    {
        if (builder.Configuration.Sources[i] is JsonConfigurationSource source &&
            source.Path is { } path &&
            path.StartsWith("appsettings.", StringComparison.OrdinalIgnoreCase) &&
            !path.Equals("appsettings.json", StringComparison.OrdinalIgnoreCase) &&
            path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            builder.Configuration.Sources.RemoveAt(i);
        }
    }

    // 對 appsettings.json 中的設定值，以此檔案為最終來源。
    builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Add services to the container.

    builder.Services.AddAuthorization();
    builder.Services.AddControllers(options =>
    {
        // 全域預設：所有 controller/action 都要授權
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

        options.Filters.Add(new AuthorizeFilter(policy));
    });

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();


    //這樣 Swagger schema 名稱會變成類似：
    //ECRS_API.Models.ECRS.專案名稱代碼表
    //ECRS_API.Models.PMDS.專案名稱代碼表
    //就不會撞名。
    builder.Services.AddSwaggerGen(c =>
    {
        c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
    });

    // EF Core：ECRS、PMDS 使用相容舊版 SQL Server 的 SQL，
    // 避免 EF Core 8 將清單 Contains 轉譯為 OPENJSON（需相容性層級 130 以上）。
    builder.Services.AddDbContext<PMDSDbContext>(opt =>
        opt.UseSqlServer(
            builder.Configuration.GetConnectionString("PMDS"),
            sqlOptions => sqlOptions.UseCompatibilityLevel(120)));

    builder.Services.AddDbContext<ECRSDbContext>(opt =>
        opt.UseSqlServer(
            builder.Configuration.GetConnectionString("ECRS"),
            sqlOptions => sqlOptions.UseCompatibilityLevel(120)));

    builder.Services.AddDbContext<ISMSDbContext>(opt =>
        opt.UseSqlServer(
            builder.Configuration.GetConnectionString("ISMS"),
            sqlOptions => sqlOptions.UseCompatibilityLevel(120)));


    // JWT options + service
    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor |
            ForwardedHeaders.XForwardedProto;
    });

    // JWT Authentication
    var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
    if (jwt is null ||
        string.IsNullOrWhiteSpace(jwt.Key) ||
        string.IsNullOrWhiteSpace(jwt.Issuer) ||
        string.IsNullOrWhiteSpace(jwt.Audience))
    {
        throw new InvalidOperationException("Jwt 設定缺失，請檢查 appsettings.json 的 Jwt:Key / Issuer / Audience。");
    }

    builder.Services.AddSingleton<JwtTokenService>();

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opt =>
        {
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });




    var app = builder.Build();
    app.UseForwardedHeaders();

    //app.MapGet("/", () => Results.Json(new { ok = true, app = "PMDSAPI", timeUtc = DateTime.UtcNow }));
    app.MapGet("/diag/conn", (IConfiguration cfg) =>
    {
        var e = cfg.GetConnectionString("ECRS");
        var p = cfg.GetConnectionString("PMDS");
        var i = cfg.GetConnectionString("ISMS");

        return Results.Json(new
        {
            ECRS = string.IsNullOrWhiteSpace(e) ? "(null/empty)" : "HAS_VALUE",
            PMDS = string.IsNullOrWhiteSpace(p) ? "(null/empty)" : "HAS_VALUE",
            ISMS = string.IsNullOrWhiteSpace(i) ? "(null/empty)" : "HAS_VALUE"
        });
    });


    // 不只 Development，正式站也啟用
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = "swagger"; // 進入網址 /swagger
    });


    // 若未來有驗證 / 授權，會加在這裡
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();

    // 4. 對應 Controller
    app.MapControllers();

    app.Run();
}
catch (HostAbortedException)
{
    // Visual Studio / tooling 探測主機時可能正常中止，不記成 error
    throw;
}
catch (Exception ex)
{
    logger.Error(ex, "Program stopped because of exception");
    throw;

}
finally
{
    NLog.LogManager.Shutdown();
}
