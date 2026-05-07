using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreAPI.Controllers;
using ECRS_API.Data;
using ECRS_API.DTOs.FormManageDTO.FormEditer;
using ECRS_API.Models.PMDS;
using Microsoft.EntityFrameworkCore;


namespace ECRS_API.Services
{
    public class GetCommonService
    {
        //private readonly ECRSDbContext _db;
        private readonly PMDSDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;

        public GetCommonService(
            PMDSDbContext db,
            IConfiguration config,
            ILogger<AuthController> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }

        public async Task<List<業別主分類表>> 業別主分類表(string _key)
        {
            var data = await _db.業別主分類表s
                .Where(x => x.主鍵 == _key || string.IsNullOrEmpty(_key ?? ""))
                .OrderBy(x => x.業別主分類名稱)
                .ToListAsync();
            return data;
        }

        public async Task<List<業別次分類表>> 業別次分類表(string _key)
        {
            var data = await _db.業別次分類表s
                .Where(x => x.業別主分類主鍵 == _key || string.IsNullOrEmpty(_key ?? ""))
                .OrderBy(x => x.主鍵)
                .ToListAsync();
            return data;
        }

        public async Task<List<QueryContents>> 稽查單查詢(string QueryString)
        {
            var data = await _db.專案名稱代碼表s
                .GroupJoin(
                    _db.系統_部門表s,
                    a => a.建立部門,
                    b => b.部門編號,
                    (a, bs) => new { a, bs }
                )
                .SelectMany(
                    x => x.bs.DefaultIfEmpty(),
                    (x, b) => new { x.a, b }
                )
                .GroupJoin(
                    _db.系統_使用者資料表s,
                    x => x.a.建立人員主鍵,
                    c => c.使用者編號,
                    (x, cs) => new { x.a, x.b, cs }
                )
                .SelectMany(
                    x => x.cs.DefaultIfEmpty(),
                    (x, c) => new
                    {
                        ProjectId = x.a.專案名稱代碼表主鍵,
                        DptCode = x.a.建立部門,
                        DeptName = x.b != null ? x.b.名稱 : null,
                        ProjectYear = x.a.專案截止日期,
                        ProjectName = x.a.專案名稱,
                        MODDATE = x.a.異動時間,
                        ModUser = x.a.異動人員主鍵,
                        Active = x.a.是否啟用
                    }
                )
                .OrderBy(x => x.ProjectId)
                .ToListAsync();

            var result = data
                .Select((x, index) => new QueryContents
                {
                    ProjectId = x.ProjectId,
                    DptCode = x.DptCode,
                    DeptName = x.DeptName,
                    ProjectYear = x.ProjectYear,
                    ProjectName = x.ProjectName,
                    ChkItems = _db.GetProjectTypeINChinese(x.ProjectId),
                    MODDATE = x.MODDATE,
                    ModUser = x.ModUser,
                    Active = x.Active,
                    ROWID = index + 1
                })
                .ToList();

            return result;
        }
    }
}
