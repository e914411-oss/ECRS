using System.Net.Http;
using System.Threading.Tasks;
using ECRS_WEB.DTOs.FormManageDTO.FormEditer;
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
            // 這個方法沒有任何 await，建議移除 async 修飾詞並回傳 Task.FromResult
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return await Task.FromResult(PartialView("FormQryByPJ"));
            }

            return await Task.FromResult(View());
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
            List<AddProject_Result> _projectNames = await Get_專案名稱代碼表(queryCondiction) ?? [];

            ViewBag.Departments = _departments;


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

        public IActionResult FormUpdate(int _projectId)
        {

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

        #endregion

    }
}
