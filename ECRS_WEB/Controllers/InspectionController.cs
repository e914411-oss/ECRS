using ECRS_WEB.Models;
//using ECRS_WEB.Models.ECRS;
using ECRS_WEB.Models.PMDS;
using ECRS_WEB.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using static CoreWebApp.Controllers.InspectionController;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static ECRS_WEB.Services.ReadPMDSDTApiClient;
using ECRS_WEB.DTOs.InspectionDTO.PReview;
using ECRS_WEB.DTOs.InspectionDTO.Fquery;
using ECRS_WEB.DTOs.InspectionDTO.InspectionQry;
using ECRS_WEB.DTOs.FormManageDTO.FormEditer;

namespace CoreWebApp.Controllers
{
    [Authorize]
    public class InspectionController : Controller
    {
        private readonly ReadPMDSDTApiClient _apiPMDS;
        private readonly ReadECRSDTApiClient _apiECRS;
        private readonly ILogger<InspectionController> _logger;

        public InspectionController(ReadPMDSDTApiClient apiPMDS, ReadECRSDTApiClient apiECRS, ILogger<InspectionController> logger)
        {
            _apiPMDS = apiPMDS;
            _apiECRS = apiECRS;
            _logger = logger;
        }

        public IActionResult Index()
        {
            // ...組 model，可忽略
            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                return PartialView("Index" /*, model */);
            }

            return View(/* model */);
        }

        [HttpGet]
        public async Task<IActionResult> InspectionQry(string? companyId, string? formName)
        {
            ViewBag.CompanyId = companyId ?? string.Empty;
            ViewBag.FormName = formName ?? string.Empty;

            var queryCondiction = new QueryCondiction
            {
                CreateDepartment = string.Empty,
                ProjectName = formName ?? string.Empty,
                FormStatus = string.Empty,
                ProjectDeadlineStart = string.Empty,
                ProjectDeadlineEnd = string.Empty
            };

            List<AddProject_Result> projectNames = [];

            try
            {
                projectNames = await Get_專案名稱代碼表(queryCondiction) ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InspectionQry 專案名稱代碼表查詢失敗");
                ModelState.AddModelError(string.Empty, "查詢專案名稱代碼表失敗");
            }

            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                return PartialView("InspectionQry", projectNames);
            }

            return View(projectNames);
        }


        public async Task<List<AddProject_Result>> Get_專案名稱代碼表(QueryCondiction queryCondiction)
        {
            return await _apiECRS.Query_專案名稱代碼表(queryCondiction);
        }

        public async Task<IActionResult> InspectionForms(string? _IsCompleted, string? _FormName, string? companyId, int[]? projectIds, string[]? projectNames)
        {
            if (!string.IsNullOrEmpty(_IsCompleted) || !string.IsNullOrEmpty(_FormName))
            {
                TempData["IsCompleted"] = (_IsCompleted == "1");
                TempData["FormName"] = _FormName;

                return RedirectToAction(nameof(InspectionForms));
            }

            bool isCompleted = false;
            if (TempData["IsCompleted"] != null)
            {
                isCompleted = Convert.ToBoolean(TempData["IsCompleted"]);
            }

            ViewBag.IsCompletedForm = isCompleted;
            ViewBag.FormName = TempData["FormName"]?.ToString();

            var vm = new InspectionFormsViewModel
            {
                CompanyId = companyId ?? string.Empty,
                InspectionDate = DateTime.Now.ToString("yyyy/MM/dd")
            };

            if (!string.IsNullOrWhiteSpace(companyId))
            {
                try
                {
                    var supplier = new Supplier { 業者編號 = companyId };
                    vm.Company = await Get_Company(supplier) ?? new 業者資料表();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "InspectionForms 業者資料查詢失敗，companyId={CompanyId}", companyId);
                    ModelState.AddModelError(string.Empty, "查詢業者資料失敗");
                }
            }

            if (projectIds is { Length: > 0 })
            {
                vm.ProjectGroups = projectIds
                    .Select((projectId, index) => new InspectionProjectItemGroup
                    {
                        ProjectId = projectId,
                        ProjectName = projectNames != null && index < projectNames.Length && !string.IsNullOrWhiteSpace(projectNames[index])
                            ? projectNames[index]
                            : projectId.ToString(),
                        Items =
                        [
                            new InspectionItemLink { Id = 1, ItemName = "預留項目一", ItemCode = "ReservedItem1" },
                            new InspectionItemLink { Id = 2, ItemName = "預留項目二", ItemCode = "ReservedItem2" }
                        ]
                    })
                    .ToList();
            }

            return View(vm);
        }

        public IActionResult InspectionFormContent()
        {
            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                return PartialView("InspectionFormContent");
            }

            return View();
        }

        public async Task<IActionResult> Fquery(SupplierQ supplierQ, int page = 1)
        {
            ViewData.Clear();
            ModelState.Clear();

            supplierQ.商業登記名稱 ??= "";
            supplierQ.市招名稱 ??= "";
            supplierQ.市招名稱 ??= "";
            supplierQ.負責人 ??= "";
            supplierQ.食品登錄字號 ??= "";
            supplierQ.統一編號 ??= "";
            supplierQ.電話號碼 ??= "";
            supplierQ.縣市代碼 ??= "";
            supplierQ.鄉鎮區代碼 ??= "";
            supplierQ.業者地址 ??= "";
            supplierQ.業者業別主類 ??= "";
            supplierQ.業者業別次類 ??= "";
            supplierQ.業者編號 ??= "";
            supplierQ.營業狀況 ??= "";


            var DeptDt = await Get_系統_部門表(string.Empty);
            var IndustryClass = await Get_業別主分類表(string.Empty);
            ViewBag.DeptList = DeptDt;
            ViewBag.IndustryClassList = IndustryClass;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var supplier = await Get_Supplier(supplierQ);
                int pageSize = 10;
                var totalCount = supplier.Count;
                var data = supplier;

                var vm = new SupplierPageViewModel
                {
                    Suppliers = data.ToList(),
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    TotalCount = totalCount
                };

                if (vm.Suppliers.Count == 0)
                {
                    return PartialView("_FqueryPartial");
                }

                //點擊查詢或其他按鈕後要呈現資料的方式，PartialView
                return PartialView("_FqueryPartial", vm);
            }

            //首次進入頁面在還沒查詢以前不用帶資料進去
            return View("Fquery");
        }


        public IActionResult FormQuery()
        {
            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                return PartialView("FormQuery");
            }

            return View();
        }

        public IActionResult FormContent()
        {
            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                return PartialView("FormContent");
            }

            return View();
        }

        public async Task<IActionResult> PReview()
        {
            var checkRecM = await Get_CheckRecM();

            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                return PartialView("PReview");
            }

            return View("PReview", checkRecM);
        }

        public async Task<IActionResult> ReviewPerform(int eventId)
        {
            var vmM = await Get_CheckRecM1(eventId);
            if (!string.IsNullOrEmpty(vmM.業者地址_鄉鎮區主鍵))
            {
                vmM.業者地址_鄉鎮區主鍵 = vmM.業者地址_鄉鎮區主鍵.Trim();
            }
            List<CheckRecD> checkRecD = await Get_CheckRecD(eventId);
            var vm = new CheckRecMDViewModel();
            vm.Mast = vmM;
            vm.CheckRecs = checkRecD;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return RedirectToAction("ReviewPerform", "Inspection", eventId);
                //return PartialView("ReviewPerform", vm);
            }

            return View("ReviewPerform", vm);
        }

        //案件退回
        [HttpPost]
        public async Task<IActionResult> ReviewUpdate(BackNote eventI)
        {
            try
            {
                var vmM = await Upd_CheckRecM1(eventI);
                //return RedirectToAction("ReviewPerform", "Inspection", eventI.eventId);

                return Ok(vmM);

            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IActionResult> Flist(string companyId)
        {
            Supplier supplierQ1 = new Supplier();
            supplierQ1.業者編號 = companyId;

            var vmC = await Get_Company(supplierQ1);
            if (vmC.營業地址_鄉鎮區主鍵 != null)
            {
                vmC.營業地址_鄉鎮區主鍵 = vmC.營業地址_鄉鎮區主鍵.Trim();
            }

            var vmR = await Get_CheckRec(companyId);
            var vm = new CompanyPageViewModel();
            vm.Company = vmC;
            vm.CheckRecs = vmR;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                //return PartialView("Flist", vm);
                return RedirectToAction("Flist", "Inspection", companyId); //
            }
            return View("Flist", vm);
        }

        //ExportExcelF
        public async Task<IActionResult> ExportExcelF(SupplierQ supplierQ)
        {
            // ❗不要用分頁條件
            var suppliers = await Get_Supplier(supplierQ);
            ;

            var sb = new StringBuilder();

            sb.AppendLine("<table border='1'>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>業者編號</th><th>業者名稱</th><th>統一編號</th><th>電話號碼</th><th>業者地址</th>");
            sb.AppendLine("</tr>");

            foreach (var s in suppliers)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{s.業者編號}</td>");
                sb.AppendLine($"<td>{s.業者名稱}</td>");
                sb.AppendLine($"<td>{s.統一編號}</td>");
                sb.AppendLine($"<td>{s.電話號碼}</td>");
                sb.AppendLine($"<td>{s.業者地址}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");

            return File(Encoding.UTF8.GetBytes(sb.ToString()),
                "application/vnd.ms-excel",
                "業者資料.xls");

        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<List<系統_部門表>> Get_系統_部門表(string cities)
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

        public async Task<List<業別主分類表>> Get_業別主分類表(string _key)
        {
            try
            {
                return await _apiPMDS.Query_業別主分類表(_key);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<業別次分類表>> Get_業別次分類表(string _key)
        {
            try
            {
                return await _apiPMDS.Query_業別次分類表(_key);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<PMDS_機構_縣市匹配>> GetCityAreaByCity(string cityId)
        {
            try
            {
                if (cityId != null)
                {
                    return await _apiPMDS.Query_PMDS_機構_縣市匹配(cityId);
                }
                else
                {
                    return null;
                }

            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<鄉鎮代碼表>> GetAreaByCity(string cityId)
        {
            try
            {
                if (cityId != null)
                {
                    return await _apiPMDS.Query_鄉鎮代碼表(cityId);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        //業者業別次類
        public async Task<List<業別次分類表>> GetSubByKind(string kindId)
        {
            try
            {
                return await _apiPMDS.Query_業別次分類表(kindId);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<Supplier>> Get_Supplier(SupplierQ supplierQ)
        {
            try
            {
                return await _apiPMDS.Query_Supplier(supplierQ);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<業者資料表> Get_Company(Supplier supplierQ)
        {
            try
            {
                return await _apiPMDS.Query_業者資料表(supplierQ);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<CheckRec>> Get_CheckRec(string companyId)
        {
            try
            {
                return await _apiPMDS.Query_稽查資料(companyId);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<CheckRecM>> Get_CheckRecM()
        {
            try
            {
                return await _apiPMDS.Query_待審核資料("");
            }
            catch (Exception)
            {
                throw;
            }
        }



        public async Task<ReadPMDSDTApiClient.MyResponse> Upd_CheckRecM1(BackNote eventI)
        {
            try
            {
                return await _apiPMDS.Upd_待審核資料M1(eventI);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<CheckRecM> Get_CheckRecM1(int eventId)
        {
            try
            {
                return await _apiPMDS.Query_待審核資料M1(eventId);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<CheckRecD>> Get_CheckRecD(int eventId)
        {
            try
            {
                return await _apiPMDS.Query_待審核資料D(eventId);
            }
            catch (Exception)
            {
                throw;
            }
        }

    }
}
