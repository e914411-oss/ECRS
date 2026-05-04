using ECRS_WEB;
using ECRS_WEB.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog;
using NLog.Web;


var logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config", optional: true).GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // NLog: 設定NLog作為日誌記錄器
    // 清掉 ASP.NET Core 內建 providers，改用 NLog
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Add services to the container.
    builder.Services.AddControllersWithViews();
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddDistributedMemoryCache();


    // Cookie Authentication 加上Cookie驗證
    builder.Services.AddAuthentication("AppCookie")
        .AddCookie("AppCookie", opt =>
        {
            opt.LoginPath = "/Auth/Login";
            opt.AccessDeniedPath = "/Auth/Denied";
            opt.Cookie.HttpOnly = true;
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

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Entry/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
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

