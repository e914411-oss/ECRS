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

namespace CoreWebApp.Controllers
{
    [Authorize]
    public class InspectionController : Controller
    {
        private readonly ReadPMDSDTApiClient _api;
        private readonly ILogger<InspectionController> _logger;

        public InspectionController(ReadPMDSDTApiClient api, ILogger<InspectionController> logger)
        {
            _api = api;
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

        public IActionResult InspectionQry()
        {
            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                return PartialView("InspectionQry");
            }

            return View();
        }

        public IActionResult InspectionForms(string? _IsCompleted, string? _FormName)
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

            return View();
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
                return await _api.Query_系統_部門表(cities);
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
                return await _api.Query_業別主分類表(_key);
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
                return await _api.Query_業別次分類表(_key);
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
                    return await _api.Query_PMDS_機構_縣市匹配(cityId);
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
                    return await _api.Query_鄉鎮代碼表(cityId);
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
                return await _api.Query_業別次分類表(kindId);
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
                return await _api.Query_Supplier(supplierQ);
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
                return await _api.Query_業者資料表(supplierQ);
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
                return await _api.Query_稽查資料(companyId);
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
                return await _api.Query_待審核資料("");
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
                return await _api.Upd_待審核資料M1(eventI);
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
                return await _api.Query_待審核資料M1(eventId);
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
                return await _api.Query_待審核資料D(eventId);
            }
            catch (Exception)
            {
                throw;
            }
        }

    }
}
