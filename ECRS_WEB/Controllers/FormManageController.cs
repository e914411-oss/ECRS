using System.Net.Http;
using System.Threading.Tasks;
using ECRS_WEB.DTOs.FormManageDTO.FormEditer;
using ECRS_WEB.DTOs.FormManageDTO.FormQryByPJ;
using ECRS_WEB.Models;
//using ECRS_WEB.Models.ECRS;
using ECRS_WEB.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static System.Net.WebRequestMethods;

namespace ECRS_WEB.Controllers
{
    [Authorize]
    public class FormManageController : Controller
    {
        private readonly ReadPMDSDTApiClient _apiPMDS;
        private readonly ReadECRSDTApiClient _apiECRS;
        private readonly ILogger<FormManageController> _logger;

        public FormManageController(ReadPMDSDTApiClient apiPMDS, ReadECRSDTApiClient apiECRS, ILogger<FormManageController> logger)
        {
            _apiPMDS = apiPMDS;
            _apiECRS = apiECRS;
            _logger = logger;
        }

        public IActionResult FIndex()
        {
            // ...組 model，可忽略
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("FIndex" /*, model */);
            }

            return View(/* model */);
        }

        public async Task<IActionResult> FormQryByPJ()
        {
            ViewBag.Departments = await Get_系統_部門表("") ?? [];

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("FormQryByPJ");
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> FormQryByPJSearch(QueryCondiction queryCondiction)
        {
            queryCondiction.CreateDepartment ??= string.Empty;
            queryCondiction.ProjectName ??= string.Empty;
            queryCondiction.FormStatus ??= string.Empty;
            queryCondiction.ProjectDeadlineStart ??= string.Empty;
            queryCondiction.ProjectDeadlineEnd ??= string.Empty;

            try
            {
                List<AddProject_Result> projectNames = await Get_專案名稱代碼表_PMDS(queryCondiction) ?? [];
                var options = projectNames.Select(project => new
                {
                    value = project.專案主鍵.ToString(),
                    text = project.專案名稱
                });

                return Ok(options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FormQryByPJ 專案查詢失敗");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "查詢專案資料失敗"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> FormQryByPJSave([FromBody] ProjectCopy projectCopy, CancellationToken ct)
        {
            if (projectCopy is null || projectCopy.ProjectIds.Count == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "請先選擇專案"
                });
            }

            ApiAddProjectResult apiResult = await _apiECRS.Save_PMDS專案名稱代碼(projectCopy, ct);

            if (!apiResult.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = apiResult.Message ?? "API 儲存失敗"
                });
            }

            return Ok(new
            {
                success = true,
                id = apiResult.Id,
                message = apiResult.Message ?? "儲存成功"
            });
        }


        #region 表單管理 by Peter

        public async Task<IActionResult> FormEditer(QueryCondiction queryCondiction)
        {
            queryCondiction.CreateDepartment ??= string.Empty;
            queryCondiction.ProjectName ??= string.Empty;
            queryCondiction.FormStatus ??= string.Empty;
            queryCondiction.ProjectDeadlineStart ??= string.Empty;
            queryCondiction.ProjectDeadlineEnd ??= string.Empty;

            if (!string.IsNullOrEmpty(queryCondiction.ProjectDeadlineStart))
            {
                queryCondiction.ProjectDeadlineStart = queryCondiction.ProjectDeadlineStart.Replace(@"/", "");
            }
            if (!string.IsNullOrEmpty(queryCondiction.ProjectDeadlineEnd))
            {
                queryCondiction.ProjectDeadlineEnd = queryCondiction.ProjectDeadlineEnd.Replace(@"/", "");
            }

            List<ECRS_WEB.Models.PMDS.系統_部門表> _departments = await Get_系統_部門表("") ?? [];
            ViewBag.Departments = _departments;


            List<AddProject_Result> _projectNames = await Get_專案名稱代碼表(queryCondiction) ?? [];

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                if (_projectNames.Count == 0)
                {
                    return PartialView("_FormEditerPartial");
                }

                return PartialView("_FormEditerPartial", _projectNames);
            }

            return View("FormEditer");

        }

        public IActionResult FormPreview(int _projectId)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("FormPreview");
            }
            return View();
        }

        public async Task<IActionResult> FormUpdate(int projectId)
        {
            if (projectId > 0)
            {
                var queryCondiction = new QueryCondiction
                {
                    ProjectId = projectId.ToString()
                };

                List<AddProject_Result> projectNames = await Get_專案名稱代碼表(queryCondiction) ?? [];
                AddProject_Result? project = projectNames.FirstOrDefault();

                if (project is not null)
                {
                    ViewData["newInspectionIDs"] = project.專案主鍵.ToString();
                    ViewData["FormName"] = project.專案名稱 ?? string.Empty;
                    ViewData["ProjectDeadline"] = FormatRocDate(project.專案截止日期);
                    ViewData["SelectedInspectionItemValues"] = SplitInspectionItems(project.稽查項目);
                }
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("FormUpdate");
            }
            return View();
        }

        public IActionResult FormDelete(int _projectId)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("FormDelete");
            }
            return View();
        }

        public Task<IActionResult> FormAdd()
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Task.FromResult<IActionResult>(PartialView("FormAdd"));
            }

            return Task.FromResult<IActionResult>(View());
        }

        private static string FormatRocDate(string? rocDate)
        {
            if (string.IsNullOrWhiteSpace(rocDate))
            {
                return string.Empty;
            }

            string digits = rocDate.Trim().Replace("/", string.Empty);
            if (digits.Length != 7)
            {
                return rocDate.Trim();
            }

            return $"{digits[..3]}/{digits.Substring(3, 2)}/{digits.Substring(5, 2)}";
        }

        private static string[] SplitInspectionItems(string? inspectionItems)
        {
            return (inspectionItems ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        [HttpPost]
        public async Task<IActionResult> FormAdd([FromForm] AddProject_Form addProject_Form, CancellationToken ct)
        {
            if (addProject_Form is null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "未收到新增資料"
                });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value!.Errors.Select(e => e.ErrorMessage).ToList()
                    );

                return BadRequest(new
                {
                    success = false,
                    message = "表單資料驗證失敗",
                    errors
                });
            }

            ApiAddProjectResult apiResult = await _apiECRS.Add_新增專案名稱代碼(addProject_Form, ct);

            if (!apiResult.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = apiResult.Message ?? "API 新增失敗"
                });
            }

            return Ok(new
            {
                success = true,
                id = apiResult.Id,
                message = apiResult.Message ?? "儲存成功"
            });
        }

        [HttpPost]
        public async Task<IActionResult> FormAddFloatColumn([FromForm] AddProject_FloatColumn _FloatColumn, CancellationToken ct)
        {
            if (_FloatColumn is null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "未收到編輯資料"
                });
            }
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value!.Errors.Select(e => e.ErrorMessage).ToList()
                    );
                return BadRequest(new
                {
                    success = false,
                    message = "表單資料驗證失敗",
                    errors
                });
            }
            ApiAddProject_FloatColumn apiResult = await _apiECRS.Add_專案浮動欄位設定表(_FloatColumn, ct);
            if (!apiResult.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = apiResult.Message ?? "API，專案浮動欄位設定失敗"
                });
            }
            return Ok(new
            {
                success = true,
                id = apiResult.Id,
                message = apiResult.Message ?? "設定成功"
            });
        }

        #endregion


        #region 呼叫Service端

        public async Task<List<ECRS_WEB.Models.PMDS.系統_部門表>> Get_系統_部門表(string cities = "")
        {
            try
            {
                return await _apiPMDS.Query_系統_部門表(cities);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<AddProject_Result>> Get_專案名稱代碼表(QueryCondiction queryCondiction)
        {
            try
            {
                return await _apiECRS.Query_專案名稱代碼表(queryCondiction);

            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<AddProject_Result>> Get_專案名稱代碼表_PMDS(QueryCondiction queryCondiction)
        {
            try
            {
                return await _apiECRS.Query_專案名稱代碼表_PMDS(queryCondiction);

            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion

    }
}
