using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ECRS_WEB.DTOs.FormManageDTO.FormEditer;
using ECRS_WEB.DTOs.FormManageDTO.FormQryByPJ;
using ECRS_WEB.DTOs.InspectionDTO.Fquery;
using ECRS_WEB.DTOs.InspectionDTO.PReview;
using ECRS_WEB.Models;
using ECRS_WEB.Models.ECRS;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

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
        public async Task<List<AddProject_Result>> Query_專案名稱代碼表(QueryCondiction _queryCondiction, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();

            var action = Uri.EscapeDataString("專案名稱代碼表");
            var url = $"/Api/FormManage/{action}";

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = JsonContent.Create(_queryCondiction);

            using var resp = await _http.PostAsJsonAsync(url, _queryCondiction, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception($"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}");
            }

            var result = await resp.Content.ReadFromJsonAsync<List<AddProject_Result>>(cancellationToken: ct);
            return result ?? new List<AddProject_Result>();
        }

        public async Task<List<AddProject_Result>> Query_專案名稱代碼表_PMDS(QueryCondiction _queryCondiction, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();

            var action = Uri.EscapeDataString("專案名稱代碼表_PMDS");
            var url = $"/Api/FormManage/{action}";

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = JsonContent.Create(_queryCondiction);

            using var resp = await _http.PostAsJsonAsync(url, _queryCondiction, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception($"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}");
            }

            var result = await resp.Content.ReadFromJsonAsync<List<AddProject_Result>>(cancellationToken: ct);
            return result ?? new List<AddProject_Result>();
        }

        public async Task<ApiAddProjectResult> Add_新增專案名稱代碼(AddProject_Form addProject_Form, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();

            var action = Uri.EscapeDataString("新增專案名稱代碼");
            var url = $"/Api/FormManage/{action}";

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = JsonContent.Create(addProject_Form);

            using var resp = await _http.SendAsync(req, ct);

            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                return new ApiAddProjectResult
                {
                    Success = false,
                    Id = 0,
                    Message = $"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}"
                };
            }

            var apiResult = JsonSerializer.Deserialize<ApiAddProjectResult>(
                raw,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            if (apiResult is null)
            {
                return new ApiAddProjectResult
                {
                    Success = false,
                    Id = 0,
                    Message = "API 回傳資料格式錯誤"
                };
            }

            return new ApiAddProjectResult
            {
                Success = apiResult.Success,
                Id = apiResult.Id,
                Message = apiResult.Message
            };
        }

        public async Task<ApiAddProject_FloatColumn> Add_專案浮動欄位設定表(AddProject_FloatColumn _FloatColumn, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();

            var action = Uri.EscapeDataString("新增專案名稱浮動欄位");
            var url = $"/Api/FormManage/{action}";

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = JsonContent.Create(_FloatColumn);

            using var resp = await _http.SendAsync(req, ct);

            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                return new ApiAddProject_FloatColumn
                {
                    Success = false,
                    Message = $"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}"
                };
            }

            var apiResult = JsonSerializer.Deserialize<ApiAddProjectResult>(
                raw,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            if (apiResult is null)
            {
                return new ApiAddProject_FloatColumn
                {
                    Success = false,
                    Id = 0,
                    Message = "API 回傳資料格式錯誤"
                };
            }

            return new ApiAddProject_FloatColumn
            {
                Success = apiResult.Success,
                Id = apiResult.Id,
                Message = apiResult.Message
            };
        }
        public async Task<ApiAddProjectResult> Save_PMDS專案名稱代碼(ProjectCopy projectCopy, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();

            var action = Uri.EscapeDataString("儲存PMDS專案名稱代碼");
            var url = $"/Api/FormManage/{action}";

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = JsonContent.Create(projectCopy);

            using var resp = await _http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                return new ApiAddProjectResult
                {
                    Success = false,
                    Id = 0,
                    Message = $"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}"
                };
            }

            var apiResult = JsonSerializer.Deserialize<ApiAddProjectResult>(
                raw,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (apiResult is null)
            {
                return new ApiAddProjectResult
                {
                    Success = false,
                    Id = 0,
                    Message = "API 回傳資料格式錯誤"
                };
            }

            return new ApiAddProjectResult
            {
                Success = apiResult.Success,
                Id = apiResult.Id,
                Message = apiResult.Message
            };
        }
    }
}
