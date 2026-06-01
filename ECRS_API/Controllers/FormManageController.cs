using System;
using ECRS_API.Data;
using ECRS_API.DTOs;
using ECRS_API.DTOs.FormMangeDTO.FormEditer;
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
        public async Task<ActionResult<IEnumerable<ECRS_API.Models.ECRS.專案名稱代碼表>>> 專案名稱代碼表(ECRS_ECRS_API.DTOs.FormManageDTO.FormEditer.QueryCondiction queryCondiction)
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
        public async Task<ActionResult<IEnumerable<ECRS_API.Models.PMDS.專案名稱代碼表>>> 專案名稱代碼表_PMDS(ECRS_ECRS_API.DTOs.FormManageDTO.FormEditer.QueryCondiction queryCondiction)
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

        #endregion

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
