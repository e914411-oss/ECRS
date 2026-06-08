using System;
using ECRS_API.Data;
using ECRS_API.DTOs;
using ECRS_API.DTOs.FormMangeDTO.FormEditer;
using ECRS_API.DTOs.FormMangeDTO.FormQryByPJ;
using ECRS_API.DTOs.Security;
using ECRS_API.Models;
using ECRS_API.Models.ECRS;

/*using ECRS_API.Models.PMDS;*/
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static CoreAPI.Controllers.FormManageController;

namespace CoreAPI.Controllers
{
    [ApiController]
    [Route("Api/[controller]")]
    [Authorize]
    public class FormManageController : Controller
    {
        private readonly ECRSDbContext _ECRSdb;
        private readonly PMDSDbContext _PMDSdb;
        private readonly JwtTokenService _jwt;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;

        public FormManageController(PMDSDbContext PMDSdb, ECRSDbContext ECRSdb, JwtTokenService jwt, IConfiguration config, ILogger<AuthController> logger)
        {
            _ECRSdb = ECRSdb;
            _PMDSdb = PMDSdb;
            _jwt = jwt;
            _config = config;
            _logger = logger;
        }


        #region DAta取得

        [HttpPost("系統_部門表")]
        public async Task<ActionResult<IEnumerable<ECRS_API.Models.PMDS.系統_部門表>>> 系統_部門表([FromBody] string Citys)
        {
            IQueryable<ECRS_API.Models.PMDS.系統_部門表> result = from n in _PMDSdb.系統_部門表s
                                                             where !n.部門編號.Contains("D") && n.部門編號 != "0"
                                                             select n;

            if (!Citys.Contains("D") && !string.IsNullOrEmpty(Citys))
            {
                //非系統管理者時限制讀取縣市資料
                result = result.Where(n => n.部門編號 == Citys).OrderBy(o => o.部門編號);
            }

            List<ECRS_API.Models.PMDS.系統_部門表> data = await result.ToListAsync();

            return Ok(data);
        }

        [HttpPost("PMDS_機構_縣市匹配")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ECRS_API.Models.PMDS.PMDS_機構_縣市匹配>>> PMDS_機構_縣市匹配([FromBody] string Citys)
        {
            IQueryable<ECRS_API.Models.PMDS.PMDS_機構_縣市匹配> result = from n in _PMDSdb.PMDS_機構_縣市匹配s
                                                                   where n.縣市代碼 != "0"
                                                                   select n;

            if (!Citys.Contains("D") && !string.IsNullOrEmpty(Citys))
            {
                result = result.Where(n => n.縣市代碼 == Citys).OrderBy(o => o.縣市代碼);
            }

            List<ECRS_API.Models.PMDS.PMDS_機構_縣市匹配> data = await result.ToListAsync();

            return Ok(data);
        }

        [HttpPost("鄉鎮代碼表")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ECRS_API.Models.PMDS.鄉鎮代碼表>>> 鄉鎮代碼表([FromBody] string Citys)
        {
            IQueryable<ECRS_API.Models.PMDS.鄉鎮代碼表> result = from n in _PMDSdb.鄉鎮代碼表s
                                                            where n.縣市編號 != "0"
                                                            select n;

            if (!string.IsNullOrEmpty(Citys))
            {
                result = result.Where(n => n.縣市編號 == Citys).OrderBy(o => o.鄉鎮編號);
            }

            List<ECRS_API.Models.PMDS.鄉鎮代碼表> data = await result.ToListAsync();

            return Ok(data);
        }

        [HttpPost("業別次分類表")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ECRS_API.Models.PMDS.業別次分類表>>> 業別次分類表([FromBody] string kind)
        {
            IQueryable<ECRS_API.Models.PMDS.業別次分類表> result = from n in _PMDSdb.業別次分類表s
                                                             where n.業別主分類主鍵 != "0"
                                                             select n;

            if (!string.IsNullOrEmpty(kind))
            {
                result = result.Where(n => n.業別主分類主鍵 == kind).OrderBy(o => o.主鍵);
            }

            List<ECRS_API.Models.PMDS.業別次分類表> data = await result.ToListAsync();

            return Ok(data);
        }

        [HttpPost("專案名稱代碼表")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<AddProject_Result>>> 專案名稱代碼表(
            ECRS_ECRS_API.DTOs.FormManageDTO.FormEditer.QueryCondiction queryCondiction)

        {
            queryCondiction.CreateDepartment ??= string.Empty;
            queryCondiction.ProjectName ??= string.Empty;
            queryCondiction.FormStatus ??= string.Empty;
            queryCondiction.ProjectDeadlineStart ??= string.Empty;
            queryCondiction.ProjectDeadlineEnd ??= string.Empty;

            IQueryable<AddProject_Result> result = from n in _ECRSdb.專案名稱代碼表s
                                                   join d in _ECRSdb.專案名稱_稽查項目附表s on n.專案名稱代碼表主鍵 equals d.專案名稱代碼主鍵 into gj
                                                   where (queryCondiction.CreateDepartment == "" || n.建立部門 == queryCondiction.CreateDepartment)
                                                   && (queryCondiction.ProjectName == "" || (n.專案名稱 ?? string.Empty).Contains(queryCondiction.ProjectName))
                                                   && (queryCondiction.ProjectDeadlineStart == "" || (n.專案截止日期 != null && n.專案截止日期.Length == 7 && string.Compare(n.專案截止日期, queryCondiction.ProjectDeadlineStart) >= 0))
                                                   && (queryCondiction.ProjectDeadlineEnd == "" || (n.專案截止日期 != null && n.專案截止日期.Length == 7 && string.Compare(n.專案截止日期, queryCondiction.ProjectDeadlineEnd) <= 0))
                                                   //&& (queryCondiction.FormStatus == "" || n.是否啟用 == queryCondiction.FormStatus)
                                                   select new AddProject_Result
                                                   {
                                                       專案主鍵 = n.專案名稱代碼表主鍵,
                                                       專案名稱 = n.專案名稱 ?? string.Empty,
                                                       稽查項目 = gj.Select(x => x.稽查項目).FirstOrDefault() ?? string.Empty,
                                                       修改日期 = n.異動時間 ?? default,
                                                       異動人員 = n.異動人員主鍵 ?? string.Empty,
                                                       狀態 = n.是否啟用 ?? string.Empty
                                                   };

            List<AddProject_Result> data = await result.ToListAsync();
            return Ok(data);
        }

        [HttpPost("專案名稱代碼表_PMDS")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<AddProject_Result>>> 專案名稱代碼表_PMDS(
            ECRS_ECRS_API.DTOs.FormManageDTO.FormEditer.QueryCondiction queryCondiction)
        {
            queryCondiction.CreateDepartment ??= string.Empty;
            queryCondiction.ProjectName ??= string.Empty;
            queryCondiction.FormStatus ??= string.Empty;
            queryCondiction.ProjectDeadlineStart ??= string.Empty;
            queryCondiction.ProjectDeadlineEnd ??= string.Empty;

            IQueryable<AddProject_Result> result = from n in _PMDSdb.專案名稱代碼表s
                                                   where (queryCondiction.CreateDepartment == "" || n.建立部門 == queryCondiction.CreateDepartment)
                                                   && (queryCondiction.ProjectName == "" || (n.專案名稱 ?? string.Empty).Contains(queryCondiction.ProjectName))
                                                   && (queryCondiction.ProjectDeadlineStart == "" || (n.專案截止日期 != null && n.專案截止日期.Length == 7 && string.Compare(n.專案截止日期, queryCondiction.ProjectDeadlineStart) >= 0))
                                                   && (queryCondiction.ProjectDeadlineEnd == "" || (n.專案截止日期 != null && n.專案截止日期.Length == 7 && string.Compare(n.專案截止日期, queryCondiction.ProjectDeadlineEnd) <= 0))
                                                   select new AddProject_Result
                                                   {
                                                       專案主鍵 = n.專案名稱代碼表主鍵,
                                                       專案名稱 = n.專案名稱 ?? string.Empty,
                                                       稽查項目 = string.Empty,
                                                       修改日期 = n.異動時間 ?? default,
                                                       異動人員 = n.異動人員主鍵 ?? string.Empty,
                                                       狀態 = n.是否啟用 ?? string.Empty
                                                   };

            List<AddProject_Result> data = await result.ToListAsync();
            return Ok(data);
        }

        #endregion

        [HttpPost("儲存PMDS專案名稱代碼")]
        [AllowAnonymous]
        public async Task<ActionResult> 儲存PMDS專案名稱代碼([FromBody] ProjectCopy projectCopy)
        {
            if (projectCopy is null || projectCopy.ProjectIds.Count == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "請先選擇專案"
                });
            }

            var projectIds = projectCopy.ProjectIds.Distinct().ToList();
            var projectId = projectIds.First();


            await using var tx = await _ECRSdb.Database.BeginTransactionAsync();

            try
            {
                //var existingPMDSProjectIds = await _ECRSdb.專案名稱代碼表s
                //    .Where(project => project.專案名稱代碼表主鍵_PMDS.HasValue && projectIds.Contains(project.專案名稱代碼表主鍵_PMDS.Value))
                //    .Select(project => project.專案名稱代碼表主鍵_PMDS!.Value)
                //    .ToListAsync();
                var existingPMDSProjectIds = await _ECRSdb.專案名稱代碼表s
                        .Where(project =>
                            project.專案名稱代碼表主鍵_PMDS.HasValue &&
                            project.專案名稱代碼表主鍵_PMDS.Value == projectId)
                        .Select(project => project.專案名稱代碼表主鍵_PMDS!.Value)
                        .ToListAsync();

                var projects = await _PMDSdb.專案名稱代碼表s
                    .Where(project => projectIds.Contains(project.專案名稱代碼表主鍵) && !existingPMDSProjectIds.Contains(project.專案名稱代碼表主鍵))
                    .ToListAsync();

                var projects2 = await _PMDSdb.專案名稱代碼表s
                     .Where(project =>
                         projectIds.Contains(project.專案名稱代碼表主鍵) &&
                         !existingPMDSProjectIds.Contains(project.專案名稱代碼表主鍵))
                     .ToListAsync();

                var allItemId = await _ECRSdb.專案名稱_稽查項目代碼表s
                    .Select(x => new
                    {
                        稽查項目代碼 = x.稽查項目代碼,
                        稽查項目 = x.稽查項目,
                        專案名稱代碼表_稽查欄位名稱 = x.專案名稱代碼表_稽查欄位名稱
                    })
                    .Distinct()
                    .ToListAsync();

                foreach (var project in projects)
                {
                    var inspectionSelectedCodes = new List<string>();
                    var inspectionSelectedColumns = new List<string>();

                    foreach (var item in allItemId)
                    {
                        var columnName = item.專案名稱代碼表_稽查欄位名稱;

                        if (string.IsNullOrWhiteSpace(columnName))
                        {
                            continue;
                        }

                        var propertyInfo = project.GetType().GetProperty(columnName);

                        if (propertyInfo == null)
                        {
                            continue;
                        }

                        var value = propertyInfo.GetValue(project)?.ToString();

                        if (value == "Y")
                        {
                            inspectionSelectedCodes.Add(item.稽查項目代碼.ToString());
                            inspectionSelectedColumns.Add(item.稽查項目);
                        }
                    }

                    var inspectionSelectedCodesText = string.Join(",", inspectionSelectedCodes);
                    var inspectionSelectedColumnsText = string.Join(",", inspectionSelectedColumns);

                    var insertData = new 專案名稱_稽查項目附表
                    {
                        專案名稱代碼主鍵 = project.專案名稱代碼表主鍵,
                        稽查項目代碼 = inspectionSelectedCodesText,
                        稽查項目 = inspectionSelectedColumnsText
                    };

                    _ECRSdb.專案名稱_稽查項目附表s.Add(insertData);
                }

                //存回ECRS的專案名稱代碼表
                foreach (var project in projects)
                {
                    _ECRSdb.專案名稱代碼表s.Add(new ECRS_API.Models.ECRS.專案名稱代碼表
                    {
                        專案名稱代碼表主鍵_PMDS = project.專案名稱代碼表主鍵,
                        建立部門 = project.建立部門,
                        專案截止日期 = project.專案截止日期,
                        專案名稱 = project.專案名稱,
                        GHP專案 = project.GHP專案,
                        標示專案 = project.標示專案,
                        抽驗專案 = project.抽驗專案,
                        熟食專案 = project.熟食專案,
                        建立時間 = project.建立時間 ?? DateTime.Now,
                        建立人員主鍵 = project.建立人員主鍵,
                        保留欄位用 = project.保留欄位用,
                        油炸油專案 = project.油炸油專案,
                        HACCP專案 = project.HACCP專案,
                        查驗登記工廠專案_健康食品工廠 = project.查驗登記工廠專案_健康食品工廠,
                        查驗登記產品專案_健康食品 = project.查驗登記產品專案_健康食品,
                        異動時間 = project.異動時間,
                        衛生局稽查系統主鍵 = project.衛生局稽查系統主鍵,
                        異動人員主鍵 = project.異動人員主鍵,
                        GHP專案_體檢情形 = project.GHP專案_體檢情形,
                        查驗登記工廠專案_食品添加物工廠 = project.查驗登記工廠專案_食品添加物工廠,
                        查驗登記工廠專案_國產維生素錠狀膠囊狀工廠 = project.查驗登記工廠專案_國產維生素錠狀膠囊狀工廠,
                        查驗登記產品專案_食品添加物 = project.查驗登記產品專案_食品添加物,
                        查驗登記產品專案_國產維生素錠狀膠囊狀食品 = project.查驗登記產品專案_國產維生素錠狀膠囊狀食品,
                        查驗登記產品專案_輸入錠狀膠囊狀食品 = project.查驗登記產品專案_輸入錠狀膠囊狀食品,
                        查驗登記產品專案_病人用特殊營養食品 = project.查驗登記產品專案_病人用特殊營養食品,
                        查驗登記產品專案_嬰兒配方食品及較大嬰兒配方輔助食品 = project.查驗登記產品專案_嬰兒配方食品及較大嬰兒配方輔助食品,
                        是否啟用 = project.是否啟用,
                        真空包裝專案 = project.真空包裝專案,
                        GHP專案_標示符合性 = project.GHP專案_標示符合性,
                        專案承辦人員主鍵 = project.專案承辦人員主鍵,
                        是否有專案目標數 = project.是否有專案目標數,
                        是否有設定浮動欄位 = project.是否有設定浮動欄位,
                        瘦身美容業稽專案 = project.瘦身美容業稽專案,
                        化粧品工廠專案 = project.化粧品工廠專案,
                        化粧品專案 = project.化粧品專案,
                        藥商藥局醫療機構專案 = project.藥商藥局醫療機構專案,
                        藥品專案 = project.藥品專案,
                        藥廠自用原料查核專案 = project.藥廠自用原料查核專案,
                        藥品回收專案 = project.藥品回收專案,
                        醫療器材專案 = project.醫療器材專案,
                        稽查事由及內容 = project.稽查事由及內容,
                        派案 = project.派案,
                        是否派案 = project.是否派案,
                        登錄查核專案 = project.登錄查核專案,
                        追溯追蹤專案 = project.追溯追蹤專案,
                        一級品管專案 = project.一級品管專案,
                        食品郵購買賣定型化契約 = project.食品郵購買賣定型化契約,
                        餐飲禮券定型化契約 = project.餐飲禮券定型化契約,
                        訂席_外燴定型化契約 = project.訂席_外燴定型化契約,
                        藥品回收後續處理結果 = project.藥品回收後續處理結果,
                        醫療器材回收後續處理結果 = project.醫療器材回收後續處理結果,
                        化粧品回收後續處理結果 = project.化粧品回收後續處理結果,
                        化粧品業者專案 = project.化粧品業者專案,
                        產品責任保險 = project.產品責任保險,
                        來源文件保存 = project.來源文件保存,
                        逾期食品保存 = project.逾期食品保存,
                        衛生管理人員 = project.衛生管理人員,
                        專門職業或技術證照人員 = project.專門職業或技術證照人員,
                    });

                }

                await _ECRSdb.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new
                {
                    success = true,
                    id = projects.Count,
                    message = $"儲存成功，共新增 {projects.Count} 筆專案。"
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();

                return BadRequest(new
                {
                    success = false,
                    message = "儲存 PMDS 專案名稱代碼失敗",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        #region DAta動作（新增、修改、刪除）

        [HttpPost("新增專案名稱代碼")]
        [AllowAnonymous]
        public async Task<ActionResult> 新增專案名稱代碼([FromBody] AddProject_Form _addform)
        {
            if (_addform is null)
            {
                return BadRequest("未收到新增資料");
            }

            _addform.FormName ??= string.Empty;
            _addform.ProjectDeadline ??= string.Empty;
            _addform.Status ??= string.Empty;
            _addform.InspectionItems ??= string.Empty;
            _addform.InspectionItemsValue ??= string.Empty;

            await using var tx = await _ECRSdb.Database.BeginTransactionAsync();

            try
            {
                var 專案名稱代碼_新增資料 = new ECRS_API.Models.ECRS.專案名稱代碼表
                {
                    專案名稱 = _addform.FormName,
                    專案截止日期 = _addform.ProjectDeadline.Replace(@"/", ""),
                    是否啟用 = _addform.Status,
                    建立時間 = DateTime.Now
                };

                _ECRSdb.專案名稱代碼表s.Add(專案名稱代碼_新增資料);
                await _ECRSdb.SaveChangesAsync();

                var newProjectId = 專案名稱代碼_新增資料.專案名稱代碼表主鍵;

                var 專案名稱_稽查項目附表_新增資料 = new ECRS_API.Models.ECRS.專案名稱_稽查項目附表
                {
                    專案名稱代碼主鍵 = newProjectId,
                    稽查項目 = _addform.InspectionItems,
                    稽查項目代碼 = _addform.InspectionItemsValue
                };

                _ECRSdb.專案名稱_稽查項目附表s.Add(專案名稱_稽查項目附表_新增資料);
                await _ECRSdb.SaveChangesAsync();

                //回填專案名稱代碼表上各個稽查項目的值
                var inspectionItemCodes = (專案名稱_稽查項目附表_新增資料.稽查項目代碼 ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => int.TryParse(x, out _))
                    .Select(int.Parse)
                    .ToList();

                var selectedColumnNames = _ECRSdb.專案名稱_稽查項目代碼表s
                    .Where(x => inspectionItemCodes.Contains(x.稽查項目代碼))
                    .Select(x => x.專案名稱代碼表_稽查欄位名稱)
                    .Where(x => x != null && x != "")
                    .Distinct()
                    .ToList();

                var allColumnNames = _ECRSdb.專案名稱_稽查項目代碼表s
                    .Select(x => x.專案名稱代碼表_稽查欄位名稱)
                    .Where(x => x != null && x != "")
                    .Distinct()
                    .ToList();

                //先全部設成 N
                foreach (var columnName in allColumnNames)
                {
                    var prop = 專案名稱代碼_新增資料.GetType().GetProperty(columnName);

                    if (prop != null && prop.CanWrite)
                    {
                        prop.SetValue(專案名稱代碼_新增資料, "N");
                    }
                }

                //被選到的欄位設成 Y
                foreach (var columnName in selectedColumnNames)
                {
                    var prop = 專案名稱代碼_新增資料.GetType().GetProperty(columnName);

                    if (prop != null && prop.CanWrite)
                    {
                        prop.SetValue(專案名稱代碼_新增資料, "Y");
                    }
                }

                await _ECRSdb.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new
                {
                    success = true,
                    id = newProjectId
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();

                return BadRequest(new
                {
                    success = false,
                    message = "新增專案名稱代碼失敗",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }

        }

        [HttpPost("新增專案名稱浮動欄位")]
        [AllowAnonymous]
        public async Task<ActionResult> 新增專案名稱浮動欄位([FromBody] AddProject_FloatColumn _FloatColumn)
        {
            if (_FloatColumn is null)
            {
                return BadRequest("未收到新增資料");
            }

            _FloatColumn.columnname ??= string.Empty;
            _FloatColumn.type ??= string.Empty;
            _FloatColumn.note ??= string.Empty;

            await using var tx = await _ECRSdb.Database.BeginTransactionAsync();

            try
            {
                var 專案浮動欄位設定表 = new ECRS_API.Models.ECRS.專案浮動欄位設定表
                {
                    專案名稱代碼表主鍵 = _FloatColumn.專案名稱代碼主鍵,
                    欄位排序 = _FloatColumn.Sort,
                    欄位名稱 = _FloatColumn.columnname,
                    欄位說明 = _FloatColumn.note,
                    欄位類型 = _FloatColumn.type,
                    欄位是否必填 = _FloatColumn.ismusttobe
                };

                _ECRSdb.專案浮動欄位設定表s.Add(專案浮動欄位設定表);
                await _ECRSdb.SaveChangesAsync();

                var newDataId = 專案浮動欄位設定表.專案浮動欄位設定表主鍵;

                await _ECRSdb.SaveChangesAsync();

                await tx.CommitAsync();

                return Ok(new
                {
                    success = true,
                    id = newDataId
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();

                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                    error = ex.StackTrace,
                    innerError = ex.InnerException?.Message
                });
            }

        }

        #endregion
    }
}
