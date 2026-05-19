using ECRS_WEB;
using ECRS_WEB.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.HttpOverrides;
using NLog;
using NLog.Web;
using Microsoft.AspNetCore.CookiePolicy;


var logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config", optional: true).GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // 避免 Kestrel 在回應中輸出 Server 標頭；IIS 標頭另由 web.config 移除。
    builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

    // NLog: 設定NLog作為日誌記錄器
    // 清掉 ASP.NET Core 內建 providers，改用 NLog
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Add services to the container.
    builder.Services.AddControllersWithViews();
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddDistributedMemoryCache();

    // 弱點掃描修正：強化 Cookie 屬性，避免登入/驗證 Cookie 在非 HTTPS 情境下傳送。
    builder.Services.Configure<CookiePolicyOptions>(options =>
    {
        options.MinimumSameSitePolicy = SameSiteMode.Strict;
        options.HttpOnly = HttpOnlyPolicy.Always;
        options.Secure = CookieSecurePolicy.Always;
    });

    builder.Services.AddAntiforgery(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });


    // Cookie Authentication 加上Cookie驗證
    builder.Services.AddAuthentication("AppCookie")
        .AddCookie("AppCookie", opt =>
        {
            opt.LoginPath = "/Auth/Login";
            opt.AccessDeniedPath = "/Auth/Denied";
            opt.Cookie.HttpOnly = true;
            opt.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            opt.Cookie.SameSite = SameSiteMode.Strict;
            opt.SlidingExpiration = true;
            opt.ExpireTimeSpan = TimeSpan.FromHours(1);
        });

    builder.Services.AddAuthorization();

    builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\ECRS\WebKeys"))
    .SetApplicationName("ECRS_WEB");

    builder.Services.AddSession(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.IsEssential = true;
        options.IdleTimeout = TimeSpan.FromMinutes(60);
    });

    // HttpClient: AuthApiClient 註冊連接資料庫的CoreAPI
    // Auth API Client
    builder.Services.AddHttpClient<AuthApiClient>((sp, client) =>
    {
        var cfg = sp.GetRequiredService<IConfiguration>();
        var baseUrl = cfg["Api:BaseUrl"]
            ?? throw new InvalidOperationException("Api:BaseUrl not set");

        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(60);
    });

    //ReadPMDSDTApiClient
    builder.Services.AddHttpClient<ReadPMDSDTApiClient>((sp, client) =>
    {
        var cfg = sp.GetRequiredService<IConfiguration>();
        var baseUrl = cfg["Api:BaseUrl"]
            ?? throw new InvalidOperationException("Api:BaseUrl not set");

        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(60);
    });

    //ReadECRSDTApiClient
    builder.Services.AddHttpClient<ReadECRSDTApiClient>((sp, client) =>
    {
        var cfg = sp.GetRequiredService<IConfiguration>();
        var baseUrl = cfg["Api:BaseUrl"]
            ?? throw new InvalidOperationException("Api:BaseUrl not set");

        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(60);
    });


    //註冊裝置偵測服務（用於判斷行動裝置或桌面裝置）
    builder.Services.AddSingleton<IDeviceDetector, DeviceDetector>();


    // 若正式環境前方有反向 Proxy / Load Balancer，需還原 X-Forwarded-Proto，讓 IsHttps 判斷正確。
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // IIS 同機部署通常不需額外設定；若前方有固定 Proxy，正式上線時建議加 KnownProxies / KnownNetworks。
    });

    //註冊政府網站登入的網址以及用於回傳Token的服務器
    builder.Services.Configure<GovLoginOptions>(
        builder.Configuration.GetSection("GovLogin")
    );

    builder.Services.AddHttpClient<GspOAuthClient>((sp, http) =>
    {
        var opt = sp.GetRequiredService<IOptions<GovLoginOptions>>().Value;

        if (string.IsNullOrWhiteSpace(opt.BaseUrl))
            throw new InvalidOperationException("GovLogin1:BaseUrl 未設定");

        http.BaseAddress = new Uri(opt.BaseUrl);
        http.Timeout = TimeSpan.FromSeconds(20);
    });


    var app = builder.Build();

    app.UseForwardedHeaders();

    // 弱點掃描修正：所有正式環境 HTTP 請求直接轉 HTTPS，避免帳密從 HTTP POST 傳送。
    app.Use(async (context, next) =>
    {
        if (!context.Request.IsHttps && !app.Environment.IsDevelopment())
        {
            var httpsUrl = $"https://{context.Request.Host}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
            context.Response.StatusCode = StatusCodes.Status308PermanentRedirect;
            context.Response.Headers.Location = httpsUrl;
            return;
        }

        await next();
    });

    // 弱點掃描修正：補齊安全標頭並移除 ASP.NET/IIS 技術指紋標頭。
    app.Use(async (context, next) =>
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            headers.Remove("X-Powered-By");
            headers.Remove("X-AspNet-Version");
            headers.Remove("X-AspNetMvc-Version");
            headers.Remove("Server");

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "SAMEORIGIN";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=(), fullscreen=(self)";
            headers["Content-Security-Policy"] = string.Join("; ", new[]
            {
                "default-src 'self'",
                "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.datatables.net",
                "style-src 'self' 'unsafe-inline' https://cdn.datatables.net https://cdn.jsdelivr.net",
                "font-src 'self' data: https://cdn.jsdelivr.net",
                "font-src 'self' data:",
                "connect-src 'self'",
                "object-src 'none'",
                "base-uri 'self'",
                "form-action 'self'",
                "frame-ancestors 'self'",
                "upgrade-insecure-requests"
            });

            return Task.CompletedTask;
        });

        await next();
    });

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Entry/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseCookiePolicy();
    app.UseStaticFiles();
    app.UseRouting();

    app.UseSession(); // 注意要放在 Routing 後面

    app.UseAuthentication(); // 一定要在 UseAuthorization 前
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Entry}/{action=Index}/{id?}");

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Application stopped because of exception");
    throw;

}
finally
{
    LogManager.Shutdown();
}

