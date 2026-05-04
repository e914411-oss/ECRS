using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ECRS_WEB.DTOs.FormManageDTO.FormEditer;
using ECRS_WEB.DTOs.InspectionDTO.Fquery;
using ECRS_WEB.DTOs.InspectionDTO.PReview;
using ECRS_WEB.Models;
using ECRS_WEB.Models.ECRS;
using Newtonsoft.Json.Linq;
//using static CoreWebApp.Controllers.InspectionController;

namespace ECRS_WEB.Services
{
    public class ReadECRSDTApiClient
    {
        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public ReadECRSDTApiClient(HttpClient http, IHttpContextAccessor httpContextAccessor)
        {
            _http = http;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetTokenOrThrow()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                throw new InvalidOperationException("HttpContext 為 null，無法取得 Session。");
            }

            var token = httpContext.Session.GetString("AuthToken");
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new UnauthorizedAccessException("Session 中找不到 AuthToken，請重新登入。");
            }

            return token;
        }
        public async Task<List<專案名稱代碼表>> Query_專案名稱代碼表(QueryCondiction _queryCondiction, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();

            var action = Uri.EscapeDataString("專案名稱代碼表");
            var url = $"/Api/FormManage/{action}";

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            //req.Content = JsonContent.Create(_cityies);

            using var resp = await _http.PostAsJsonAsync(url, _queryCondiction, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception($"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}");
            }

            var result = await resp.Content.ReadFromJsonAsync<List<專案名稱代碼表>>(cancellationToken: ct);
            return result ?? new List<專案名稱代碼表>();
        }



    }
}
