using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ECRS_WEB.DTOs.InspectionDTO.Fquery;
using ECRS_WEB.DTOs.InspectionDTO.PReview;
using ECRS_WEB.Models;
using ECRS_WEB.Models.PMDS;
using Newtonsoft.Json.Linq;
using static CoreWebApp.Controllers.InspectionController;

namespace ECRS_WEB.Services
{
    public class ReadPMDSDTApiClient
    {
        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ReadPMDSDTApiClient(HttpClient http, IHttpContextAccessor httpContextAccessor)
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

        public async Task<List<系統_部門表>> Query_系統_部門表(string _cityies, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();

            var action = Uri.EscapeDataString("系統_部門表");
            var url = $"/Api/FormManage/{action}";

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = JsonContent.Create(_cityies);

            using var resp = await _http.SendAsync(req, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception($"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}");
            }

            var result = await resp.Content.ReadFromJsonAsync<List<系統_部門表>>(cancellationToken: ct);
            return result ?? new List<系統_部門表>();
        }

        public async Task<List<PMDS_機構_縣市匹配>> Query_PMDS_機構_縣市匹配(string _cityies, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();

            var action = Uri.EscapeDataString("PMDS_機構_縣市匹配");
            var url = $"/Api/FormManage/{action}";

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = JsonContent.Create(_cityies);

            using var resp = await _http.SendAsync(req, ct);
            //var resp = await _http.PostAsJsonAsync(url, _cityies, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception($"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}");
            }

            var result = await resp.Content.ReadFromJsonAsync<List<PMDS_機構_縣市匹配>>(cancellationToken: ct);
            return result ?? new List<PMDS_機構_縣市匹配>();
        }

        public async Task<List<鄉鎮代碼表>> Query_鄉鎮代碼表(string _cityies, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();

            var action = Uri.EscapeDataString("鄉鎮代碼表");
            var url = $"/Api/FormManage/{action}";

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = JsonContent.Create(_cityies);

            using var resp = await _http.SendAsync(req, ct);
            //var resp = await _http.PostAsJsonAsync(url, _cityies, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception($"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}");
            }

            var result = await resp.Content.ReadFromJsonAsync<List<鄉鎮代碼表>>(cancellationToken: ct);
            return result ?? new List<鄉鎮代碼表>();
        }

        public async Task<List<業別主分類表>> Query_業別主分類表(string _key,  CancellationToken ct = default)
        {
            var action = Uri.EscapeDataString("業別主分類表");
            var url = $"/Api/Inspection/{action}";

            var resp = await _http.PostAsJsonAsync<object>(url, _key, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception(
                    $"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}"
                );
            }

            var body = await resp.Content.ReadFromJsonAsync<List<業別主分類表>>(cancellationToken: ct);
            return body ?? new List<業別主分類表>();

        }

        public async Task<List<業別次分類表>> Query_業別次分類表(string _cityies, CancellationToken ct = default)
        {
            var action = Uri.EscapeDataString("業別次分類表");
            var url = $"/Api/Inspection/{action}";

            var resp = await _http.PostAsJsonAsync(url, _cityies, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception(
                    $"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}"
                );
            }

            var body = await resp.Content.ReadFromJsonAsync<List<業別次分類表>>(cancellationToken: ct);
            return body ?? new List<業別次分類表>();

        }

        public async Task<List<Supplier>> Query_Supplier(SupplierQ supplierQ, CancellationToken ct = default)
        {
            var action = Uri.EscapeDataString("Suppliers");
            var url = $"/Api/Inspection/{action}";

            var resp = await _http.PostAsJsonAsync(url, supplierQ, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception(
                    $"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}"
                );
            }

            var body = await resp.Content.ReadFromJsonAsync<List<Supplier>>(cancellationToken: ct);
            return body ?? new List<Supplier>();

        }

        //業者資料表
        public async Task<業者資料表> Query_業者資料表(Supplier supplierQ, CancellationToken ct = default)
        {
            var action = Uri.EscapeDataString("業者資料表");
            var url = $"/Api/Inspection/{action}";

            var resp = await _http.PostAsJsonAsync(url, supplierQ, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception(
                    $"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}"
                );
            }

            var body = await resp.Content.ReadFromJsonAsync<業者資料表>(cancellationToken: ct);
            return body ?? new 業者資料表();

        }

        public async Task<List<CheckRec>> Query_稽查資料(string companyId, CancellationToken ct = default)
        {
            var action = Uri.EscapeDataString("稽查資料");
            var url = $"/Api/Inspection/{action}";

            var resp = await _http.PostAsJsonAsync(url, companyId, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception(
                    $"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}"
                );
            }

            var body = await resp.Content.ReadFromJsonAsync<List<CheckRec>>(cancellationToken: ct);
            return body ?? new List<CheckRec>();

        }

        public async Task<List<CheckRecM>> Query_待審核資料(string companyId, CancellationToken ct = default)
        {
            var action = Uri.EscapeDataString("待審核資料");
            var url = $"/Api/Inspection/{action}";

            var resp = await _http.PostAsJsonAsync(url, companyId, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception(
                    $"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}"
                );
            }

            var body = await resp.Content.ReadFromJsonAsync<List<CheckRecM>>(cancellationToken: ct);
            return body ?? new List<CheckRecM>();

        }

        public async Task<CheckRecM> Query_待審核資料M1(int eventId, CancellationToken ct = default)
        {
            var action = Uri.EscapeDataString("待審核資料M1");
            var url = $"/Api/Inspection/{action}";

            var resp = await _http.PostAsJsonAsync(url, eventId, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception(
                    $"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}"
                );
            }

            var body = await resp.Content.ReadFromJsonAsync<CheckRecM>(cancellationToken: ct);
            return body ?? new CheckRecM();

        }

        public async Task<MyResponse> Upd_待審核資料M1(BackNote eventI, CancellationToken ct = default)
        {
            var action = Uri.EscapeDataString("Upd待審核資料M1");
            var url = $"/Api/Inspection/{action}";

            var resp = await _http.PostAsJsonAsync(url, eventI, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception(
                    $"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}"
                );
            }

            var body = await resp.Content.ReadFromJsonAsync<MyResponse>(cancellationToken: ct);
            return body ?? new MyResponse();

        }

        public class MyResponse
        {
            public bool success
            {
                get; set;
            }
        }


        public async Task<List<CheckRecD>> Query_待審核資料D(int eventId, CancellationToken ct = default)
        {
            var action = Uri.EscapeDataString("待審核資料D");
            var url = $"/Api/Inspection/{action}";

            var resp = await _http.PostAsJsonAsync(url, eventId, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception(
                    $"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}"
                );
            }

            var body = await resp.Content.ReadFromJsonAsync<List<CheckRecD>>(cancellationToken: ct);
            return body ?? new List<CheckRecD>();

        }

    }
}
