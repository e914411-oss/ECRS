using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ECRS_WEB.DTOs.FormManageDTO.FormEditer;
using ECRS_WEB.DTOs.FormManageDTO.FormQryByPJ;
using ECRS_WEB.DTOs.InspectionDTO.Fquery;
using ECRS_WEB.DTOs.InspectionDTO.InspectionForms;
using ECRS_WEB.DTOs.InspectionDTO.InspectionQry;
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

        public async Task<List<InspectionProjectItemGroup>> Query_專案稽查項目附表(IEnumerable<int> projectIds, CancellationToken ct = default)
        {
            var action = Uri.EscapeDataString("專案稽查項目附表");
            var url = $"/Api/FormManage/{action}";

            using var resp = await _http.PostAsJsonAsync(url, projectIds?.ToArray() ?? [], ct);

            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception($"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}");
            }

            var result = await resp.Content.ReadFromJsonAsync<List<InspectionProjectItemGroup>>(cancellationToken: ct);
            return result ?? new List<InspectionProjectItemGroup>();
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
                try
                {
                    var apiError = JsonSerializer.Deserialize<ApiAddProjectResult>(
                        raw,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (!string.IsNullOrWhiteSpace(apiError?.Message))
                    {
                        return apiError;
                    }
                }
                catch (JsonException)
                {
                }

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

        public async Task<ApiAddProjectResult> Update_專案名稱代碼(UpdateProject_Form updateProjectForm, CancellationToken cancellationToken = default)
        {
            var token = GetTokenOrThrow();
            var action = Uri.EscapeDataString("修改專案名稱代碼");
            var url = $"/Api/FormManage/{action}";

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = JsonContent.Create(updateProjectForm);

            using var resp = await _http.SendAsync(req, cancellationToken);
            return await ReadApiAddProjectResult(resp, cancellationToken);
        }

        public async Task<ApiAddProjectResult> Delete_專案名稱代碼(int projectId, CancellationToken cancellationToken = default)
        {
            var token = GetTokenOrThrow();
            var action = Uri.EscapeDataString("專案名稱代碼");
            var url = $"/Api/FormManage/{action}/{projectId}";

            using var req = new HttpRequestMessage(HttpMethod.Delete, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var resp = await _http.SendAsync(req, cancellationToken);
            return await ReadApiAddProjectResult(resp, cancellationToken);
        }

        private static async Task<ApiAddProjectResult> ReadApiAddProjectResult(HttpResponseMessage resp, CancellationToken cancellationToken)
        {
            var raw = await resp.Content.ReadAsStringAsync(cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                return new ApiAddProjectResult
                {
                    Success = false,
                    Id = 0,
                    Message = $"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}"
                };
            }

            return DeserializeApiAddProjectResult(raw);
        }

        private static ApiAddProjectResult DeserializeApiAddProjectResult(string raw)
        {
            var apiResult = JsonSerializer.Deserialize<ApiAddProjectResult>(
                raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return apiResult ?? new ApiAddProjectResult
            {
                Success = false,
                Id = 0,
                Message = "API 回傳資料格式錯誤"
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

        public async Task<AddInspectionEventResponse> Add_新增稽查事件(稽查事件_主表 _新增資料, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();

            var action = Uri.EscapeDataString("新增稽查事件");
            var url = $"/Api/Inspection/{action}";

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = JsonContent.Create(_新增資料);

            using var resp = await _http.SendAsync(req, ct);

            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                throw new Exception($"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}");
            }

            var result = JsonSerializer.Deserialize<AddInspectionEventResponse>(
                raw,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (result == null || !result.Success)
            {
                throw new Exception($"新增稽查事件失敗：{raw}");
            }

            return result;
        }

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

        public async Task<List<稽查事件_主表>> Query_稽查資料(string companyId, CancellationToken ct = default)
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

            var body = await resp.Content.ReadFromJsonAsync<List<稽查事件_主表>>(cancellationToken: ct);
            return body ?? new List<稽查事件_主表>();

        }

        public async Task<List<string>> Query_InspectionItemNames(string inspectionId, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();
            var url = $"/Api/Inspection/InspectionItemNames?inspectionId={WebUtility.UrlEncode(inspectionId ?? string.Empty)}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var resp = await _http.SendAsync(req, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception($"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}");
            }

            var body = await resp.Content.ReadFromJsonAsync<List<string>>(cancellationToken: ct);
            return body ?? new List<string>();
        }

        public async Task<ExpiredFoodInspectionResult?> GetExpiredFoodInspection(int eventId, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();
            var url = $"/Api/Inspection/ExpiredFoodInspection/{eventId}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var resp = await _http.SendAsync(req, ct);

            if (resp.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception($"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}");
            }

            return await resp.Content.ReadFromJsonAsync<ExpiredFoodInspectionResult>(cancellationToken: ct);
        }

        public async Task<AddInspectionEventResponse> SaveExpiredFoodInspection(ExpiredFoodInspectionSaveRequest request, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();
            var url = "/Api/Inspection/ExpiredFoodInspection";

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = JsonContent.Create(request);

            using var resp = await _http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            AddInspectionEventResponse? result = null;
            try
            {
                result = JsonSerializer.Deserialize<AddInspectionEventResponse>(
                    raw,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException)
            {
            }

            if (!resp.IsSuccessStatusCode)
            {
                return result ?? new AddInspectionEventResponse
                {
                    Success = false,
                    Message = $"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}"
                };
            }

            return result ?? new AddInspectionEventResponse
            {
                Success = false,
                Message = "API 回傳資料格式錯誤"
            };
        }

        public async Task<AddInspectionEventResponse> UploadExpiredFoodInspectionPhoto(string encodedEventId, IFormFile photo, string zipFileName, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();
            var url = "/Api/Inspection/ExpiredFoodInspectionPhoto";

            await using var photoStream = photo.OpenReadStream();
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(encodedEventId ?? string.Empty), "encodedEventId");
            content.Add(new StringContent(_httpContextAccessor.HttpContext?.Session.GetString("InspectionId") ?? string.Empty), "createUser");
            content.Add(new StringContent(zipFileName ?? string.Empty), "zipFileName");

            var fileContent = new StreamContent(photoStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(photo.ContentType ?? "application/octet-stream");
            content.Add(fileContent, "photo", photo.FileName);

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = content;

            using var resp = await _http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            var result = JsonSerializer.Deserialize<AddInspectionEventResponse>(
                raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (!resp.IsSuccessStatusCode)
            {
                return result ?? new AddInspectionEventResponse
                {
                    Success = false,
                    Message = $"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}"
                };
            }

            return result ?? new AddInspectionEventResponse
            {
                Success = false,
                Message = "API response format is invalid"
            };
        }

        public async Task<AddInspectionEventResponse> UploadExpiredFoodInspectionAttachment(string encodedEventId, IFormFile attachment, string zipFileName, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();
            var url = "/Api/Inspection/ExpiredFoodInspectionAttachment";

            await using var attachmentStream = attachment.OpenReadStream();
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(encodedEventId ?? string.Empty), "encodedEventId");
            content.Add(new StringContent(_httpContextAccessor.HttpContext?.Session.GetString("InspectionId") ?? string.Empty), "createUser");
            content.Add(new StringContent(zipFileName ?? string.Empty), "zipFileName");

            var fileContent = new StreamContent(attachmentStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(attachment.ContentType ?? "application/octet-stream");
            content.Add(fileContent, "attachment", attachment.FileName);

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = content;

            using var resp = await _http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            var result = JsonSerializer.Deserialize<AddInspectionEventResponse>(
                raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (!resp.IsSuccessStatusCode)
            {
                return result ?? new AddInspectionEventResponse
                {
                    Success = false,
                    Message = $"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}"
                };
            }

            return result ?? new AddInspectionEventResponse
            {
                Success = false,
                Message = "API response format is invalid"
            };
        }

        public async Task<List<InspectionUploadFileResult>> GetExpiredFoodInspectionPhotos(string encodedEventId, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();
            var url = $"/Api/Inspection/ExpiredFoodInspectionPhotos/{WebUtility.UrlEncode(encodedEventId ?? string.Empty)}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception($"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}");
            }

            var body = await resp.Content.ReadFromJsonAsync<List<InspectionUploadFileResult>>(cancellationToken: ct);
            return body ?? new List<InspectionUploadFileResult>();
        }

        public async Task<List<InspectionUploadFileResult>> GetExpiredFoodInspectionAttachments(string encodedEventId, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();
            var url = $"/Api/Inspection/ExpiredFoodInspectionAttachments/{WebUtility.UrlEncode(encodedEventId ?? string.Empty)}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception($"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}");
            }

            var body = await resp.Content.ReadFromJsonAsync<List<InspectionUploadFileResult>>(cancellationToken: ct);
            return body ?? new List<InspectionUploadFileResult>();
        }

        public async Task<AddInspectionEventResponse> UploadHealthManagerInspectionPhoto(string encodedEventId, IFormFile photo, string zipFileName, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();
            var url = "/Api/Inspection/HealthManagerInspectionPhoto";

            await using var photoStream = photo.OpenReadStream();
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(encodedEventId ?? string.Empty), "encodedEventId");
            content.Add(new StringContent(_httpContextAccessor.HttpContext?.Session.GetString("InspectionId") ?? string.Empty), "createUser");
            content.Add(new StringContent(zipFileName ?? string.Empty), "zipFileName");

            var fileContent = new StreamContent(photoStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(photo.ContentType ?? "application/octet-stream");
            content.Add(fileContent, "photo", photo.FileName);

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = content;

            using var resp = await _http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            var result = JsonSerializer.Deserialize<AddInspectionEventResponse>(
                raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (!resp.IsSuccessStatusCode)
            {
                return result ?? new AddInspectionEventResponse
                {
                    Success = false,
                    Message = $"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}"
                };
            }

            return result ?? new AddInspectionEventResponse
            {
                Success = false,
                Message = "API response format is invalid"
            };
        }

        public async Task<AddInspectionEventResponse> UploadHealthManagerInspectionAttachment(string encodedEventId, IFormFile attachment, string zipFileName, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();
            var url = "/Api/Inspection/HealthManagerInspectionAttachment";

            await using var attachmentStream = attachment.OpenReadStream();
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(encodedEventId ?? string.Empty), "encodedEventId");
            content.Add(new StringContent(_httpContextAccessor.HttpContext?.Session.GetString("InspectionId") ?? string.Empty), "createUser");
            content.Add(new StringContent(zipFileName ?? string.Empty), "zipFileName");

            var fileContent = new StreamContent(attachmentStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(attachment.ContentType ?? "application/octet-stream");
            content.Add(fileContent, "attachment", attachment.FileName);

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = content;

            using var resp = await _http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            var result = JsonSerializer.Deserialize<AddInspectionEventResponse>(
                raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (!resp.IsSuccessStatusCode)
            {
                return result ?? new AddInspectionEventResponse
                {
                    Success = false,
                    Message = $"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}"
                };
            }

            return result ?? new AddInspectionEventResponse
            {
                Success = false,
                Message = "API response format is invalid"
            };
        }

        public async Task<List<InspectionUploadFileResult>> GetHealthManagerInspectionPhotos(string encodedEventId, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();
            var url = $"/Api/Inspection/HealthManagerInspectionPhotos/{WebUtility.UrlEncode(encodedEventId ?? string.Empty)}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception($"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}");
            }

            var body = await resp.Content.ReadFromJsonAsync<List<InspectionUploadFileResult>>(cancellationToken: ct);
            return body ?? new List<InspectionUploadFileResult>();
        }

        public async Task<List<InspectionUploadFileResult>> GetHealthManagerInspectionAttachments(string encodedEventId, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();
            var url = $"/Api/Inspection/HealthManagerInspectionAttachments/{WebUtility.UrlEncode(encodedEventId ?? string.Empty)}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception($"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}");
            }

            var body = await resp.Content.ReadFromJsonAsync<List<InspectionUploadFileResult>>(cancellationToken: ct);
            return body ?? new List<InspectionUploadFileResult>();
        }

        public async Task<AddInspectionEventResponse> UploadProfessionalLicenseInspectionAttachment(string encodedEventId, IFormFile attachment, string zipFileName, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();
            var url = "/Api/Inspection/ProfessionalLicenseInspectionAttachment";

            await using var attachmentStream = attachment.OpenReadStream();
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(encodedEventId ?? string.Empty), "encodedEventId");
            content.Add(new StringContent(_httpContextAccessor.HttpContext?.Session.GetString("InspectionId") ?? string.Empty), "createUser");
            content.Add(new StringContent(zipFileName ?? string.Empty), "zipFileName");

            var fileContent = new StreamContent(attachmentStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(attachment.ContentType ?? "application/octet-stream");
            content.Add(fileContent, "attachment", attachment.FileName);

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = content;

            using var resp = await _http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            var result = JsonSerializer.Deserialize<AddInspectionEventResponse>(
                raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (!resp.IsSuccessStatusCode)
            {
                return result ?? new AddInspectionEventResponse
                {
                    Success = false,
                    Message = $"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}"
                };
            }

            return result ?? new AddInspectionEventResponse
            {
                Success = false,
                Message = "API response format is invalid"
            };
        }

        public async Task<AddInspectionEventResponse> UploadProfessionalLicenseInspectionPhoto(string encodedEventId, IFormFile photo, string zipFileName, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();
            var url = "/Api/Inspection/ProfessionalLicenseInspectionPhoto";

            await using var photoStream = photo.OpenReadStream();
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(encodedEventId ?? string.Empty), "encodedEventId");
            content.Add(new StringContent(_httpContextAccessor.HttpContext?.Session.GetString("InspectionId") ?? string.Empty), "createUser");
            content.Add(new StringContent(zipFileName ?? string.Empty), "zipFileName");

            var fileContent = new StreamContent(photoStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(photo.ContentType ?? "application/octet-stream");
            content.Add(fileContent, "photo", photo.FileName);

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = content;

            using var resp = await _http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            var result = JsonSerializer.Deserialize<AddInspectionEventResponse>(
                raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (!resp.IsSuccessStatusCode)
            {
                return result ?? new AddInspectionEventResponse
                {
                    Success = false,
                    Message = $"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}"
                };
            }

            return result ?? new AddInspectionEventResponse
            {
                Success = false,
                Message = "API response format is invalid"
            };
        }

        public async Task<List<InspectionUploadFileResult>> GetProfessionalLicenseInspectionPhotos(string encodedEventId, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();
            var url = $"/Api/Inspection/ProfessionalLicenseInspectionPhotos/{WebUtility.UrlEncode(encodedEventId ?? string.Empty)}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception($"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}");
            }

            var body = await resp.Content.ReadFromJsonAsync<List<InspectionUploadFileResult>>(cancellationToken: ct);
            return body ?? new List<InspectionUploadFileResult>();
        }

        public async Task<List<InspectionUploadFileResult>> GetProfessionalLicenseInspectionAttachments(string encodedEventId, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();
            var url = $"/Api/Inspection/ProfessionalLicenseInspectionAttachments/{WebUtility.UrlEncode(encodedEventId ?? string.Empty)}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception($"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}");
            }

            var body = await resp.Content.ReadFromJsonAsync<List<InspectionUploadFileResult>>(cancellationToken: ct);
            return body ?? new List<InspectionUploadFileResult>();
        }

        public async Task<HealthManagerInspectionResult?> GetHealthManagerInspection(int eventId, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();
            var url = $"/Api/Inspection/HealthManagerInspection/{eventId}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var resp = await _http.SendAsync(req, ct);

            if (resp.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception($"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}");
            }

            return await resp.Content.ReadFromJsonAsync<HealthManagerInspectionResult>(cancellationToken: ct);
        }

        public async Task<AddInspectionEventResponse> SaveHealthManagerInspection(HealthManagerInspectionSaveRequest request, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();
            var url = "/Api/Inspection/HealthManagerInspection";

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = JsonContent.Create(request);

            using var resp = await _http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            var result = JsonSerializer.Deserialize<AddInspectionEventResponse>(
                raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (!resp.IsSuccessStatusCode)
            {
                return result ?? new AddInspectionEventResponse
                {
                    Success = false,
                    Message = $"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}"
                };
            }

            return result ?? new AddInspectionEventResponse
            {
                Success = false,
                Message = "API 回傳資料格式錯誤"
            };
        }

        public async Task<SourceDocumentInspectionResult?> GetSourceDocumentInspection(int eventId, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();
            var url = $"/Api/Inspection/SourceDocumentInspection/{eventId}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var resp = await _http.SendAsync(req, ct);
            if (resp.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            var raw = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                throw new Exception($"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}");
            }

            return await resp.Content.ReadFromJsonAsync<SourceDocumentInspectionResult>(cancellationToken: ct);
        }

        public async Task<AddInspectionEventResponse> SaveSourceDocumentInspection(SourceDocumentInspectionSaveRequest request, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();
            var url = "/Api/Inspection/SourceDocumentInspection";

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = JsonContent.Create(request);

            using var resp = await _http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            var result = JsonSerializer.Deserialize<AddInspectionEventResponse>(
                raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (!resp.IsSuccessStatusCode)
            {
                return result ?? new AddInspectionEventResponse
                {
                    Success = false,
                    Message = $"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}"
                };
            }

            return result ?? new AddInspectionEventResponse
            {
                Success = false,
                Message = "API 回傳資料格式錯誤"
            };
        }

        public async Task<AddInspectionEventResponse> UploadSourceDocumentInspectionAttachment(string encodedEventId, IFormFile attachment, string zipFileName, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();
            var url = "/Api/Inspection/SourceDocumentInspectionAttachment";

            await using var attachmentStream = attachment.OpenReadStream();
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(encodedEventId ?? string.Empty), "encodedEventId");
            content.Add(new StringContent(_httpContextAccessor.HttpContext?.Session.GetString("InspectionId") ?? string.Empty), "createUser");
            content.Add(new StringContent(zipFileName ?? string.Empty), "zipFileName");

            var fileContent = new StreamContent(attachmentStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(attachment.ContentType ?? "application/octet-stream");
            content.Add(fileContent, "attachment", attachment.FileName);

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = content;

            using var resp = await _http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            var result = JsonSerializer.Deserialize<AddInspectionEventResponse>(
                raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (!resp.IsSuccessStatusCode)
            {
                return result ?? new AddInspectionEventResponse
                {
                    Success = false,
                    Message = $"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}"
                };
            }

            return result ?? new AddInspectionEventResponse
            {
                Success = false,
                Message = "API response format is invalid"
            };
        }

        public async Task<AddInspectionEventResponse> UploadSourceDocumentInspectionPhoto(string encodedEventId, IFormFile photo, string zipFileName, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();
            var url = "/Api/Inspection/SourceDocumentInspectionPhoto";

            await using var photoStream = photo.OpenReadStream();
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(encodedEventId ?? string.Empty), "encodedEventId");
            content.Add(new StringContent(_httpContextAccessor.HttpContext?.Session.GetString("InspectionId") ?? string.Empty), "createUser");
            content.Add(new StringContent(zipFileName ?? string.Empty), "zipFileName");

            var fileContent = new StreamContent(photoStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(photo.ContentType ?? "application/octet-stream");
            content.Add(fileContent, "photo", photo.FileName);

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = content;

            using var resp = await _http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            var result = JsonSerializer.Deserialize<AddInspectionEventResponse>(
                raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (!resp.IsSuccessStatusCode)
            {
                return result ?? new AddInspectionEventResponse
                {
                    Success = false,
                    Message = $"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}"
                };
            }

            return result ?? new AddInspectionEventResponse
            {
                Success = false,
                Message = "API response format is invalid"
            };
        }

        public async Task<List<InspectionUploadFileResult>> GetSourceDocumentInspectionAttachments(string encodedEventId, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();
            var url = $"/Api/Inspection/SourceDocumentInspectionAttachments/{WebUtility.UrlEncode(encodedEventId ?? string.Empty)}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception($"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}");
            }

            var body = await resp.Content.ReadFromJsonAsync<List<InspectionUploadFileResult>>(cancellationToken: ct);
            return body ?? new List<InspectionUploadFileResult>();
        }

        public async Task<List<InspectionUploadFileResult>> GetSourceDocumentInspectionPhotos(string encodedEventId, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();
            var url = $"/Api/Inspection/SourceDocumentInspectionPhotos/{WebUtility.UrlEncode(encodedEventId ?? string.Empty)}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception($"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}");
            }

            var body = await resp.Content.ReadFromJsonAsync<List<InspectionUploadFileResult>>(cancellationToken: ct);
            return body ?? new List<InspectionUploadFileResult>();
        }

        public async Task<ProfessionalLicenseInspectionResult?> GetProfessionalLicenseInspection(int eventId, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();
            var url = $"/Api/Inspection/ProfessionalLicenseInspection/{eventId}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var resp = await _http.SendAsync(req, ct);

            if (resp.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new Exception($"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}");
            }

            return await resp.Content.ReadFromJsonAsync<ProfessionalLicenseInspectionResult>(cancellationToken: ct);
        }

        public async Task<AddInspectionEventResponse> SaveProfessionalLicenseInspection(ProfessionalLicenseInspectionSaveRequest request, CancellationToken ct = default)
        {
            var token = GetTokenOrThrow();
            var url = "/Api/Inspection/ProfessionalLicenseInspection";

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = JsonContent.Create(request);

            using var resp = await _http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            var result = JsonSerializer.Deserialize<AddInspectionEventResponse>(
                raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (!resp.IsSuccessStatusCode)
            {
                return result ?? new AddInspectionEventResponse
                {
                    Success = false,
                    Message = $"API {(int)resp.StatusCode} {resp.ReasonPhrase}: {raw}"
                };
            }

            return result ?? new AddInspectionEventResponse
            {
                Success = false,
                Message = "API response format is invalid"
            };
        }
    }
}
