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
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;
using NLog;


var logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config", optional: true).GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
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

    //builder.Services.AddControllers();
    //把虛擬路徑統一為單一名稱，完了後面再接調用功能的名稱，例如 MyApi/Hello
    //builder.Services.AddControllers(
    //    options =>
    //    {
    //        options.Conventions.Insert(0, new GlobalRoutePrefixConvention("PMDSAPI"));
    //    }
    //);

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // EF Core
    builder.Services.AddDbContext<PMDSDbContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("PMDS")));
    builder.Services.AddDbContext<ECRSDbContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("ECRS")));
    builder.Services.AddDbContext<ISMSDbContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("ISMS")));


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
