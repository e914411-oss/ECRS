using ECRS_WEB.Models;
//using ECRS_WEB.Models.ECRS;
using ECRS_WEB.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static System.Net.WebRequestMethods;
using System.Threading.Tasks;
using ECRS_WEB.DTOs.FormManageDTO.FormEditer;




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
            List<ECRS_WEB.Models.ECRS.專案名稱代碼表> _projectNames = await Get_專案名稱代碼表(queryCondiction) ?? [];

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

        //[HttpGet]
        public Task<IActionResult> FormAdd()
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Task.FromResult<IActionResult>(PartialView("FormAdd"));
            }

            return Task.FromResult<IActionResult>(View());
        }

        [HttpPost]
        public IActionResult FormAdd(string FormName)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                ViewData["FormName"] = string.IsNullOrEmpty(FormName) ? string.Empty : FormName;
                return PartialView("FormAdd");
            }

            return View();
        }


        #endregion


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

        public async Task<List<ECRS_WEB.Models.ECRS.專案名稱代碼表>> Get_專案名稱代碼表(QueryCondiction queryCondiction)
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

    }
}
