using ECRS_WEB.Models;
//using ECRS_WEB.Models.ECRS;
using ECRS_WEB.Models.PMDS;
using ECRS_WEB.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;
using static ECRS_WEB.Services.ReadPMDSDTApiClient;
using ECRS_WEB.DTOs.FormManageDTO.FormEditer;
using ECRS_WEB.DTOs.InspectionDTO.PReview;
using ECRS_WEB.DTOs.InspectionDTO.Fquery;
using ECRS_WEB.DTOs.InspectionDTO.InspectionQry;
using ECRS_WEB.DTOs.InspectionDTO.Flist;
using ECRS_WEB.Helpers;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Razor;

namespace CoreWebApp.Controllers
{
    [Authorize]
    public class InspectionController : Controller
    {
        private readonly ReadPMDSDTApiClient _apiPMDS;
        private readonly ReadECRSDTApiClient _apiECRS;
        private readonly ILogger<InspectionController> _logger;
        private readonly IRazorViewEngine _razorViewEngine;

        public InspectionController(ReadPMDSDTApiClient apiPMDS, ReadECRSDTApiClient apiECRS, ILogger<InspectionController> logger, IRazorViewEngine razorViewEngine)
        {
            _apiPMDS = apiPMDS;
            _apiECRS = apiECRS;
            _logger = logger;
            _razorViewEngine = razorViewEngine;
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
            companyId = QueryStringSecurityHelper.UrlDecode(companyId);
            formName = QueryStringSecurityHelper.UrlDecode(formName);

            ViewBag.CompanyId = companyId ?? string.Empty;
            ViewBag.FormName = formName ?? string.Empty;

            var queryCondition = new QueryCondiction
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
                projectNames = await Get_專案名稱代碼表(queryCondition) ?? [];
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


        public async Task<List<AddProject_Result>> Get_專案名稱代碼表(QueryCondiction queryCondition)
        {
            return await _apiECRS.Query_專案名稱代碼表(queryCondition);
        }

        /// <summary>
        /// 從廠商進來後點選稽查記錄總覽進來的單，
        /// </summary>
        /// <param name="companyId"></param>
        /// <returns></returns>
        [RequireQueryStringParameter("projectId")]
        public async Task<IActionResult> InspectionForms(string? companyId, int projectId)
        {
            companyId = QueryStringSecurityHelper.UrlDecode(companyId);

            if (string.IsNullOrWhiteSpace(companyId) || projectId <= 0)
            {
                return RedirectToAction("Fquery", "Inspection");
            }

            var vm = new InspectionFormsViewModel
            {
                CompanyId = companyId ?? string.Empty,
                ProjectId = projectId,
                InspectionDate = DateTime.Now.ToString("yyyy/MM/dd")
            };

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

            var selectedProject = new InspectionProjectItemGroup
            {
                ProjectId = projectId,
                ProjectName = projectId.ToString()
            };

            try
            {
                var itemGroups = await _apiECRS.Query_專案稽查項目附表(new[] { projectId });
                var itemGroup = itemGroups.FirstOrDefault(group => group.ProjectId == projectId);

                if (itemGroup != null)
                {
                    selectedProject.ProjectName = !string.IsNullOrWhiteSpace(itemGroup.ProjectName)
                        ? itemGroup.ProjectName
                        : selectedProject.ProjectName;
                    selectedProject.Items = SplitInspectionItemLinks(itemGroup.Items);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InspectionForms 專案稽查項目附表查詢失敗，projectId={ProjectId}", projectId);
                ModelState.AddModelError(string.Empty, "查詢稽查項目失敗");
            }

            vm.ProjectGroups = new List<InspectionProjectItemGroup> { selectedProject };
            return View(vm);
        }

        public async Task<IActionResult> InspectionForms(string? companyId, int[]? projectIds, string[]? projectNames)
        {
            companyId = QueryStringSecurityHelper.UrlDecode(companyId);
            projectNames = projectNames?
                .Select(QueryStringSecurityHelper.UrlDecode)
                .ToArray();

            if (companyId != null && projectIds is { Length: > 0 })
            {
                #region 稽查事件入庫，取得稽查事件編號

                // 先生成一筆稽查事件入庫，取得稽查事件編號
                var now = DateTime.Now;
                var 稽查事件_主表新增資料 = new ECRS_WEB.Models.ECRS.稽查事件_主表
                {
                    稽查縣市編號 = HttpContext.Session.GetString("InspectionLocation"),
                    業者編號 = int.Parse(companyId),
                    專案名稱編號 = projectIds != null && projectIds.Length > 0 ? projectIds[0].ToString() : string.Empty,
                    專案名稱 = projectNames != null && projectNames.Length > 0 ? projectNames[0] : string.Empty,
                    稽查日期 = now,
                    國曆稽查日期 = $"{now.Year - 1911}{now:MMdd}",
                    為複查案件 = "N",
                    結案狀態 = "N",
                    稽查人員編號 = HttpContext.Session.GetString("InspectionId"),
                    稽查人員姓名 = HttpContext.Session.GetString("DisplayName"),
                    建立時間 = now,
                    異動時間 = now
                };

                var result = await _apiECRS.Add_新增稽查事件(稽查事件_主表新增資料);
                if (!result.Success)
                {
                    _logger.LogError("InspectionForms 新增稽查事件失敗，companyId={CompanyId}, projectIds={ProjectIds}", companyId, string.Join(',', projectIds ?? new int[] { 0 }));
                    ModelState.AddModelError(string.Empty, "新增稽查事件失敗");
                    return RedirectToAction("Fquery", "Inspection");
                }

                #endregion

                #region 查詢資料出來做顯示
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
                    var selectedProjects = projectIds
                        .Select((projectId, index) => new InspectionProjectItemGroup
                        {
                            ProjectId = projectId,
                            ProjectName = projectNames != null && index < projectNames.Length && !string.IsNullOrWhiteSpace(projectNames[index])
                                ? projectNames[index]
                                : projectId.ToString()
                        })
                        .ToList();

                    try
                    {
                        var itemGroups = await _apiECRS.Query_專案稽查項目附表(projectIds);
                        var itemGroupMap = itemGroups.ToDictionary(group => group.ProjectId);

                        vm.ProjectGroups = selectedProjects
                            .Select(project =>
                            {
                                if (itemGroupMap.TryGetValue(project.ProjectId, out var itemGroup))
                                {
                                    project.ProjectName = !string.IsNullOrWhiteSpace(itemGroup.ProjectName)
                                        ? itemGroup.ProjectName
                                        : project.ProjectName;
                                    project.Items = SplitInspectionItemLinks(itemGroup.Items);  //Items = InspectionItemLink
                                                                                                // 這裡的 SplitInspectionItemLinks 是一個方法，用來將原本的稽查項目資料轉換成 InspectionItemLink 的列表，用在第二層的顯示
                                }

                                return project;
                            })
                            .ToList();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "InspectionForms 專案稽查項目附表查詢失敗，projectIds={ProjectIds}", string.Join(',', projectIds));
                        ModelState.AddModelError(string.Empty, "查詢稽查項目失敗");
                        vm.ProjectGroups = selectedProjects;
                    }
                }
                return View(vm);

                #endregion
            }
            else
            {
                //如果是沒帶參數companyId進來，就導回業者稽查查詢
                return RedirectToAction("Fquery", "Inspection");
            }

        }

        private static List<InspectionItemLink> SplitInspectionItemLinks(IEnumerable<InspectionItemLink>? sourceItems)
        {
            var links = new List<InspectionItemLink>();

            foreach (var sourceItem in sourceItems ?? [])
            {
                var itemNames = SplitCommaSeparatedText(sourceItem.ItemName);
                var itemCodes = SplitCommaSeparatedText(sourceItem.ItemCode);

                for (var index = 0; index < itemNames.Count; index++)
                {
                    links.Add(new InspectionItemLink
                    {
                        Id = sourceItem.Id,
                        ItemName = itemNames[index],
                        ItemCode = index < itemCodes.Count ? itemCodes[index] : string.Empty
                    });
                }
            }

            return links;
        }

        private static List<string> SplitCommaSeparatedText(string? value)
        {
            return (value ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        public async Task<IActionResult> InspectionFormContent(string? InspectionId, string? inspectionItemName)
        {
            InspectionId = QueryStringSecurityHelper.UrlDecode(InspectionId);
            inspectionItemName = QueryStringSecurityHelper.UrlDecode(inspectionItemName);

            var hasInspectionId = !string.IsNullOrWhiteSpace(InspectionId);
            ViewBag.InspectionItemName = inspectionItemName?.Trim() ?? string.Empty;
            ViewBag.HasInspectionId = hasInspectionId;
            ViewBag.PartialViewNames = new List<string>();

            if (hasInspectionId)
            {
                try
                {
                    var itemNames = await _apiECRS.Query_InspectionItemNames(InspectionId!);
                    ViewBag.PartialViewNames = itemNames
                        .Select(ToInspectionPartialViewName)
                        .OfType<string>()
                        .Where(InspectionPartialViewExists)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "InspectionFormContent 查詢 Partial View 失敗，InspectionId={InspectionId}", InspectionId);
                    ModelState.AddModelError(string.Empty, "查詢稽查項目表單失敗");
                }
            }

            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                return PartialView("InspectionFormContent");
            }

            return View();
        }

        private static string? ToInspectionPartialViewName(string? inspectionItemName)
        {
            if (string.IsNullOrWhiteSpace(inspectionItemName))
            {
                return null;
            }

            var fileName = $"_{inspectionItemName.Trim()}Partial.cshtml";
            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return null;
            }

            return $"PartialPages/{Path.GetFileNameWithoutExtension(fileName)}";
        }

        private bool InspectionPartialViewExists(string partialViewName)
        {
            var getViewResult = _razorViewEngine.GetView(null, partialViewName, isMainPage: false);
            if (getViewResult.Success)
            {
                return true;
            }

            var findViewResult = _razorViewEngine.FindView(ControllerContext, partialViewName, isMainPage: false);
            return findViewResult.Success;
        }

        public async Task<IActionResult> Fquery(SupplierQ supplierQ, int page = 1)
        {
            QueryStringSecurityHelper.UrlDecodeStringProperties(supplierQ);

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


            var departmentList = await Get_系統_部門表(string.Empty);
            var industryClassList = await Get_業別主分類表(string.Empty);
            ViewBag.DeptList = departmentList;
            ViewBag.IndustryClassList = industryClassList;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var supplier = await Get_Supplier(supplierQ);
                var pageSize = 10;
                var totalCount = supplier.Count;

                var vm = new SupplierPageViewModel
                {
                    Suppliers = supplier.ToList(),
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
            var reviewMaster = await Get_CheckRecM1(eventId);
            if (!string.IsNullOrEmpty(reviewMaster.業者地址_鄉鎮區主鍵))
            {
                reviewMaster.業者地址_鄉鎮區主鍵 = reviewMaster.業者地址_鄉鎮區主鍵.Trim();
            }
            var checkRecD = await Get_CheckRecD(eventId);
            var vm = new CheckRecMDViewModel();
            vm.Mast = reviewMaster;
            vm.CheckRecs = checkRecD;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return RedirectToAction("ReviewPerform", "Inspection", new
                {
                    eventId = QueryStringSecurityHelper.UrlEncode(eventId)
                });
                //return PartialView("ReviewPerform", vm);
            }

            return View("ReviewPerform", vm);
        }

        //案件退回
        [HttpPost]
        public async Task<IActionResult> ReviewUpdate(BackNote reviewNote)
        {
            QueryStringSecurityHelper.UrlDecodeStringProperties(reviewNote);

            var updateResult = await Upd_CheckRecM1(reviewNote);
            //return RedirectToAction("ReviewPerform", "Inspection", reviewNote.eventId);

            return Ok(updateResult);
        }

        public async Task<IActionResult> Flist(string companyId)
        {
            companyId = QueryStringSecurityHelper.UrlDecode(companyId);

            var supplierQuery = new Supplier
            {
                業者編號 = companyId
            };

            var company = await _apiECRS.Query_業者資料表(supplierQuery);

            if (company.營業地址_鄉鎮區主鍵 != null)
            {
                company.營業地址_鄉鎮區主鍵 = company.營業地址_鄉鎮區主鍵.Trim();
            }

            var checkRecords = await Get_CheckRec(companyId);
            var vm = new IndustryPageViewModel();
            vm.Company = company;
            vm.CheckRecs = checkRecords;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                //return PartialView("Flist", vm);
                return RedirectToAction("Flist", "Inspection", new
                {
                    companyId = QueryStringSecurityHelper.UrlEncode(companyId)
                }); //
            }
            return View("Flist", vm);
        }

        //ExportExcelF
        public async Task<IActionResult> ExportExcelF(SupplierQ supplierQ)
        {
            QueryStringSecurityHelper.UrlDecodeStringProperties(supplierQ);

            // ❗不要用分頁條件
            var suppliers = await Get_Supplier(supplierQ);

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
            cities = QueryStringSecurityHelper.UrlDecode(cities);

            return await _apiPMDS.Query_系統_部門表(cities);
        }

        public async Task<List<業別主分類表>> Get_業別主分類表(string keyword)
        {
            keyword = QueryStringSecurityHelper.UrlDecode(keyword);

            return await _apiPMDS.Query_業別主分類表(keyword);
        }

        public async Task<List<業別次分類表>> Get_業別次分類表(string keyword)
        {
            keyword = QueryStringSecurityHelper.UrlDecode(keyword);

            return await _apiPMDS.Query_業別次分類表(keyword);
        }

        public async Task<List<PMDS_機構_縣市匹配>> GetCityAreaByCity(string cityId)
        {
            cityId = QueryStringSecurityHelper.UrlDecode(cityId);

            return cityId != null
                ? await _apiPMDS.Query_PMDS_機構_縣市匹配(cityId)
                : [];
        }

        public async Task<List<鄉鎮代碼表>> GetAreaByCity(string cityId)
        {
            cityId = QueryStringSecurityHelper.UrlDecode(cityId);

            return cityId != null
                ? await _apiPMDS.Query_鄉鎮代碼表(cityId)
                : [];
        }

        //業者業別次類
        public async Task<List<業別次分類表>> GetSubByKind(string kindId)
        {
            kindId = QueryStringSecurityHelper.UrlDecode(kindId);

            return await _apiPMDS.Query_業別次分類表(kindId);
        }

        public async Task<List<Supplier>> Get_Supplier(SupplierQ supplierQ)
        {
            QueryStringSecurityHelper.UrlDecodeStringProperties(supplierQ);

            return await _apiPMDS.Query_Supplier(supplierQ);
        }

        public async Task<業者資料表> Get_Company(Supplier supplierQ)
        {
            return await _apiPMDS.Query_業者資料表(supplierQ);
        }

        public async Task<List<ECRS_WEB.Models.ECRS.稽查事件_主表>> Get_CheckRec(string companyId)
        {
            companyId = QueryStringSecurityHelper.UrlDecode(companyId);

            return await _apiECRS.Query_稽查資料(companyId);
        }

        public async Task<List<CheckRecM>> Get_CheckRecM()
        {
            return await _apiPMDS.Query_待審核資料("");
        }



        public async Task<ReadPMDSDTApiClient.MyResponse> Upd_CheckRecM1(BackNote reviewNote)
        {
            return await _apiPMDS.Upd_待審核資料M1(reviewNote);
        }

        public async Task<CheckRecM> Get_CheckRecM1(int eventId)
        {
            return await _apiPMDS.Query_待審核資料M1(eventId);
        }

        public async Task<List<CheckRecD>> Get_CheckRecD(int eventId)
        {
            return await _apiPMDS.Query_待審核資料D(eventId);
        }

    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class RequireQueryStringParameterAttribute : Attribute, IActionConstraint
    {
        private readonly string _parameterName;

        public RequireQueryStringParameterAttribute(string parameterName)
        {
            _parameterName = parameterName;
        }

        public int Order => 0;

        public bool Accept(ActionConstraintContext context)
        {
            return context.RouteContext.HttpContext.Request.Query.ContainsKey(_parameterName);
        }
    }
}
