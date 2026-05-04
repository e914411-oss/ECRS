using System;
using ECRS_API.Data;
using ECRS_API.DTOs;
using ECRS_API.DTOs.FormMangeDTO;
using ECRS_API.DTOs.FormMangeDTO.FormEditer;
using ECRS_API.DTOs.Security;
using ECRS_API.Models;
/*using ECRS_API.Models.PMDS;*/
using ECRS_ECRS_API.DTOs.FormMangeDTO.FormEditer;
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
        public async Task<ActionResult<IEnumerable<ECRS_API.Models.ECRS.專案名稱代碼表>>> 專案名稱代碼表(QueryCondiction queryCondiction)
        {
            queryCondiction.CreateDepartment ??= string.Empty;
            queryCondiction.ProjectName ??= string.Empty;
            queryCondiction.FormStatus ??= string.Empty;
            queryCondiction.ProjectDeadlineStart ??= string.Empty;
            queryCondiction.ProjectDeadlineEnd ??= string.Empty;

            IQueryable<ECRS_API.Models.ECRS.專案名稱代碼表> result = from n in _ECRSdb.專案名稱代碼表s
                                                              where (queryCondiction.CreateDepartment == "" || n.建立部門 == queryCondiction.CreateDepartment)
                                                              && (queryCondiction.ProjectName == "" || (n.專案名稱 ?? string.Empty).Contains(queryCondiction.ProjectName))
                                                              && (queryCondiction.ProjectDeadlineStart == "" || (n.專案截止日期 != null && n.專案截止日期.Length == 7 && string.Compare(n.專案截止日期, queryCondiction.ProjectDeadlineStart) >= 0))
                                                              && (queryCondiction.ProjectDeadlineEnd == "" || (n.專案截止日期 != null && n.專案截止日期.Length == 7 && string.Compare(n.專案截止日期, queryCondiction.ProjectDeadlineEnd) <= 0))
                                                              && (queryCondiction.FormStatus == "" || n.是否啟用 == queryCondiction.FormStatus)
                                                              select n;

            List<ECRS_API.Models.ECRS.專案名稱代碼表> data = await result.ToListAsync();
            return Ok(data);
        }

        #endregion

    }
}
