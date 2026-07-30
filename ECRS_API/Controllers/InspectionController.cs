using CoreAPI.Controllers;
using ECRS_API.Data;
using ECRS_API.DTOs;
using ECRS_API.DTOs.InspectionDTO.Fquery;
using ECRS_API.DTOs.InspectionDTO.PReview;
using ECRS_API.DTOs.InspectionDTO.InspectionForms;
using ECRS_API.DTOs.Security;
using ECRS_API.Models;
using ECRS_API.Models.ECRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;

namespace ECRS_API.Controllers
{
    [ApiController]
    [Route("Api/[controller]")]
    [Authorize]
    public class InspectionController : Controller
    {
        private readonly ECRSDbContext _ECRSdb;
        private readonly PMDSDbContext _PMDSdb;
        private readonly JwtTokenService _jwt;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;

        public InspectionController(ECRSDbContext ECRSdb, PMDSDbContext PMDSdb, JwtTokenService jwt, IConfiguration config, ILogger<AuthController> logger)
        {
            _ECRSdb = ECRSdb;
            _PMDSdb = PMDSdb;
            _jwt = jwt;
            _config = config;
            _logger = logger;
        }

        private static string? NormalizeYesNoOrNull(string? value)
        {
            var normalized = value?.Trim().ToUpperInvariant();
            return normalized == "Y" || normalized == "N" ? normalized : null;
        }

        [HttpPost("Suppliers")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Supplier>>> Suppliers([FromBody] SupplierQ supplierQ)
        {
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

            var categoryQuery = from a1 in _PMDSdb.業者業別對應表s
                                join b1 in _PMDSdb.業別主分類表s
                                    on a1.業別主分類主鍵 equals b1.主鍵 into b1g
                                from b1 in b1g.DefaultIfEmpty()
                                join c1 in _PMDSdb.業別次分類表s
                                    on new
                                    {
                                        次分類主鍵 = a1.業別次分類主鍵,
                                        主分類主鍵 = a1.業別主分類主鍵
                                    }
                                    equals new
                                    {
                                        次分類主鍵 = c1.主鍵,
                                        主分類主鍵 = c1.業別主分類主鍵
                                    }
                                    into c1g
                                from c1 in c1g.DefaultIfEmpty()
                                select new
                                {
                                    業者主鍵 = a1.業者主鍵,
                                    業別主分類主鍵 = a1.業別主分類主鍵,
                                    業別次分類主鍵 = a1.業別次分類主鍵,
                                    業別主分類名稱 = b1 != null ? b1.業別主分類名稱 : null,
                                    業別次分類名稱 = c1 != null ? c1.業別次分類名稱 : null
                                };

            var query = (from n in _PMDSdb.業者資料表s
                         join d in _PMDSdb.PMDS_機構_縣市匹配s
                             on new
                             {
                                 鄉鎮區 = n.營業地址_鄉鎮區主鍵,
                                 縣市 = n.營業地址_縣市主鍵
                             }
                             equals new
                             {
                                 鄉鎮區 = d.鄉鎮區代碼,
                                 縣市 = d.縣市代碼
                             }
                         join c in categoryQuery
                             on n.主鍵 equals c.業者主鍵
                         where 1 == 1
                             && (supplierQ.商業登記名稱 == "" || (n.商業登記名稱 ?? "").Contains(supplierQ.商業登記名稱 ?? ""))
                             && (supplierQ.負責人 == "" || (n.負責人姓名 ?? "").Contains(supplierQ.負責人 ?? ""))
                             && (supplierQ.市招名稱 == "" || (n.市招名稱 ?? "").Contains(supplierQ.市招名稱 ?? ""))
                             && (supplierQ.業者地址 == "" || (((d.機構縣市鄉鎮市區 ?? "") + (n.營業地址_街道 ?? "")).Contains(supplierQ.業者地址 ?? "")))
                             && (supplierQ.縣市代碼 == "" || d.縣市代碼 == supplierQ.縣市代碼)
                             && (supplierQ.鄉鎮區代碼 == "" || d.鄉鎮區代碼 == supplierQ.鄉鎮區代碼)
                             && (supplierQ.業者業別主類 == "" || c.業別主分類主鍵 == supplierQ.業者業別主類)
                             && (supplierQ.業者業別次類 == "" || c.業別次分類主鍵 == supplierQ.業者業別次類)
                             && (supplierQ.食品登錄字號 == "" || (n.食品業者登錄字號 ?? "").Contains(supplierQ.食品登錄字號 ?? ""))
                             && (supplierQ.統一編號 == "" || (n.統一編號 ?? "").Contains(supplierQ.統一編號 ?? ""))
                             && (supplierQ.電話號碼 == "" || (n.電話 ?? "").Contains(supplierQ.電話號碼 ?? ""))
                         select new
                         {
                             業者編號 = n.主鍵,
                             食品登錄字號 = n.食品業者登錄字號,
                             統一編號 = n.統一編號,
                             電話號碼 = n.電話,
                             業者地址 = (d.機構縣市鄉鎮市區 ?? "") + (n.營業地址_街道 ?? ""),
                             業者名稱 = n.商業登記名稱,
                             案件建立日期 = n.建檔時間
                         }
                        )
                        .Distinct();

            IQueryable<Supplier> result = query.Select(
                x => new Supplier
                {
                    業者編號 = x.業者編號.ToString(),
                    食品登錄字號 = x.食品登錄字號,
                    統一編號 = x.統一編號,
                    電話號碼 = x.電話號碼,
                    業者地址 = x.業者地址,
                    業者名稱 = x.業者名稱,
                    案件建立日期 = x.案件建立日期
                });


            //IQueryable<Supplier> result = from n in _db.業者資料表s
            //                              join d in _db.PMDS_機構_縣市匹配s
            //                                on new
            //                                {
            //                                    鄉鎮區 = n.營業地址_鄉鎮區主鍵,
            //                                    縣市 = n.營業地址_縣市主鍵
            //                                }
            //                                equals new
            //                                {
            //                                    鄉鎮區 = d.鄉鎮區代碼,
            //                                    縣市 = d.縣市代碼
            //                                }
            //                              join i0 in _db.業別主分類表s
            //                                on n.非登匯入業別 equals i0.業別主分類名稱 into ig
            //                              from i in ig.DefaultIfEmpty()
            //                              where 1 == 1
            //                                && (supplierQ.商業登記名稱 == "" || (n.商業登記名稱 ?? "").Contains(supplierQ.商業登記名稱 ?? ""))
            //                                && (supplierQ.負責人 == "" || (n.負責人姓名 ?? "").Contains(supplierQ.負責人 ?? ""))
            //                                && (supplierQ.市招名稱 == "" || (n.市招名稱 ?? "").Contains(supplierQ.市招名稱 ?? ""))
            //                                && (supplierQ.業者地址 == "" || ((d.機構縣市鄉鎮市區 ?? "") + (n.營業地址_街道 ?? "")).Contains(supplierQ.業者地址 ?? ""))
            //                                && (supplierQ.縣市代碼 == "" || d.縣市代碼 == supplierQ.縣市代碼)
            //                                && (supplierQ.鄉鎮區代碼 == "" || d.鄉鎮區代碼 == supplierQ.鄉鎮區代碼)
            //                                && (supplierQ.業者業別主類 == "" || (i != null && i.業別主分類名稱 == supplierQ.業者業別主類))
            //                                && (supplierQ.食品登錄字號 == "" || (n.食品業者登錄字號 ?? "").Contains(supplierQ.食品登錄字號 ?? ""))
            //                                && (supplierQ.統一編號 == "" || (n.統一編號 ?? "").Contains(supplierQ.統一編號))
            //                                && (supplierQ.電話號碼 == "" || (n.電話 ?? "").Contains(supplierQ.電話號碼))
            //                              select new Supplier()
            //                              {
            //                                  業者編號 = n.主鍵.ToString(),
            //                                  食品登錄字號 = n.食品業者登錄字號,
            //                                  統一編號 = n.統一編號,
            //                                  電話號碼 = n.電話,
            //                                  業者地址 = d.機構縣市鄉鎮市區 + n.營業地址_街道,
            //                                  業者名稱 = n.商業登記名稱,
            //                                  案件建立日期 = n.建檔時間
            //                              };


            List<Supplier> res = await result.ToListAsync();
            List<Supplier> data = res;

            return Ok(data);
        }

        [HttpPost("業者資料表")]
        [AllowAnonymous]
        public async Task<ActionResult<ECRS_API.Models.PMDS.業者資料表>> 業者資料表([FromBody] Supplier supplierQ)
        {
            IQueryable<ECRS_API.Models.PMDS.業者資料表> result = from n in _PMDSdb.業者資料表s
                                                            where n.商業登記名稱 != "0"
                                                            select n;

            if (!string.IsNullOrEmpty(supplierQ.業者編號))
            {
                result = result.Where(n => n.主鍵 == int.Parse(supplierQ.業者編號));
            }

            List<ECRS_API.Models.PMDS.業者資料表> datas = await result.ToListAsync();
            ECRS_API.Models.PMDS.業者資料表? data = datas.FirstOrDefault();

            return Ok(data);
        }

        /// <summary>
        /// 業別主分類表（從共用服務取得）
        /// </summary>
        /// <returns></returns>
        [HttpPost("業別主分類表")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<業別主分類表>>> 業別主分類表([FromBody] string _key)
        {
            //ECRS_API.Services.GetCommonService 必須要連同Name Space一起輸入，不然會有兩個物件發生模擬兩可的情況
            var data = await new ECRS_API.Services.GetCommonService(_PMDSdb, _config, _logger).業別主分類表(_key);
            return Ok(data);
        }

        /// <summary>
        /// 業別次分類表（從共用服務取得）
        /// </summary>
        /// <returns></returns>
        [HttpPost("業別次分類表")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<業別次分類表>>> 業別次分類表([FromBody] string _key)
        {
            //ECRS_API.Services.GetCommonService 必須要連同Name Space一起輸入，不然會有兩個物件發生模擬兩可的情況
            var data = await new ECRS_API.Services.GetCommonService(_PMDSdb, _config, _logger).業別次分類表(_key);
            return Ok(data);
        }

        [HttpPost("新增稽查事件")]
        public async Task<ActionResult<AddInspectionEventResponse>> 新增稽查事件([FromBody] 稽查事件_主表 _InsertValues)
        {
            var 稽查事件_主表新增资料 = new 稽查事件_主表
            {
                稽查縣市編號 = _InsertValues.稽查縣市編號,
                業者編號 = _InsertValues.業者編號,
                專案名稱編號 = _InsertValues.專案名稱編號,
                專案名稱 = _InsertValues.專案名稱,
                稽查日期 = _InsertValues.稽查日期,
                國曆稽查日期 = _InsertValues.國曆稽查日期,
                為複查案件 = _InsertValues.為複查案件,
                結案狀態 = _InsertValues.結案狀態,
                稽查人員編號 = _InsertValues.稽查人員編號,
                稽查人員姓名 = _InsertValues.稽查人員姓名,
                建立時間 = _InsertValues.建立時間,
                異動時間 = _InsertValues.異動時間
            };

            await using var tx = await _ECRSdb.Database.BeginTransactionAsync();

            try
            {
                _ECRSdb.稽查事件_主表s.Add(稽查事件_主表新增资料);
                int a = await _ECRSdb.SaveChangesAsync();
                await tx.CommitAsync();
                return Ok(new AddInspectionEventResponse
                {
                    Success = true,
                    EventId = 稽查事件_主表新增资料.稽查事件編號
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return BadRequest(new AddInspectionEventResponse
                {
                    Success = false,
                    Message = "新增稽查事件失敗",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message ?? string.Empty
                });
            }
        }

        [HttpPost("稽查資料")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<稽查事件_主表>>> 稽查資料([FromBody] string companyId)
        {
            if (int.TryParse(companyId, out int CompanyId))
            {
                IQueryable<稽查事件_主表> result = from a in _ECRSdb.稽查事件_主表s
                                                   where a.業者編號 == CompanyId
                                                   select a;
                //select new CheckRecD()
                //{
                //    稽查單號 = a.物品稽查編號,
                //    稽查表單 = a.專案名稱,
                //    稽查人員 = d.稽查人員姓名,
                //    稽查日期 = d.稽查日期,
                //    限改日期 = null,
                //    稽查結果 = "",
                //    執行狀態 = d.稽查事件備註
                //};

                List<稽查事件_主表> res = await result.ToListAsync();
                List<稽查事件_主表> data = res;

                return Ok(data);
            }
            else
            {
                return BadRequest("Invalid companyId");
            }
        }

        [HttpPost("待審核資料")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<CheckRecM>>> 待審核資料([FromBody] string companyId)
        {
            //int CompanyId = int.Parse(companyId);
            //inner join 物品稽查明細表 s on s.稽查主檔編號 = d.稽查事件編號
            //join s in _PMDSdb.物品稽查明細表s
            //on s.稽查主檔編號 equals d.稽查事件編號
            IQueryable<CheckRecM> result = (from a in _PMDSdb.業者資料表s
                                            join d in _PMDSdb.稽查事件_主表s
                                                on a.主鍵 equals d.業者編號
                                            join s in _PMDSdb.物品稽查明細表s on d.稽查事件編號 equals s.稽查主檔編號
                                            where d.審查人員編號 == null //&& d.業者編號 == CompanyId
                                            && d.建立時間 >= DateTime.Now.AddDays(-1)
                                            select new CheckRecM()
                                            {
                                                稽查單號 = d.稽查事件編號,
                                                稽查人員 = d.稽查人員姓名,
                                                機構代碼 = "",
                                                稽查日期 = d.稽查日期,
                                                業者名稱 = a.市招名稱,
                                                業者電子信箱 = a.負責人電子郵件,
                                                負責人 = a.負責人姓名,
                                                業者電話 = a.電話,
                                                業者地址_縣市主鍵 = a.營業地址_縣市主鍵,
                                                業者地址_鄉鎮區主鍵 = a.營業地址_鄉鎮區主鍵,
                                                業者地址 = a.營業地址_街道
                                            }).Distinct();


            var res = await result.ToListAsync();
            var data = res;

            return Ok(data);
        }

        [HttpPost("Upd待審核資料M1")]
        [AllowAnonymous]
        public async Task<ActionResult> Upd待審核資料M1([FromBody] BackNote eventI)
        {
            IQueryable<ECRS_API.Models.PMDS.稽查事件_主表> result = from d in _PMDSdb.稽查事件_主表s
                                                              where d.稽查事件編號 == eventI.eventId
                                                              select d;

            ECRS_API.Models.PMDS.稽查事件_主表? data = (await result.ToListAsync()).FirstOrDefault();
            if (data != null)
            {
                data.稽查事件備註 = eventI.note;
                //var entry = _db.Entry(data);
                //var st = entry.State;
                //var a = _db.SaveChanges();

                var res = _PMDSdb.稽查事件_主表s
                       .Where(x => x.稽查事件編號 == eventI.eventId)
                       .ExecuteUpdate(s => s.SetProperty(x => x.稽查事件備註, eventI.note));
                if (res > 0)
                {
                    ECRS_API.Models.PMDS.紀錄_系統紀錄檔 entity = new ECRS_API.Models.PMDS.紀錄_系統紀錄檔
                    {
                        系統名稱 = "待審核案件-審核作業",
                        系統登入者 = "SYSTEM",
                        資料類型 = "LOG",
                        訊息 = "稽查事件編號:" + eventI.eventId.ToString() + " 退回案件",
                        內容 = "說明:" + eventI.note,
                        建立日期 = DateTime.Now
                    };

                    Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<ECRS_API.Models.PMDS.紀錄_系統紀錄檔> d = _PMDSdb.紀錄_系統紀錄檔s.Add(entity);
                    int a = _PMDSdb.SaveChanges();
                    return Ok(new
                    {
                        success = true
                    });
                }
            }
            return Ok(new
            {
                success = false
            });
            //return Ok(Json(new { success = true }));
            //return Ok(data);
        }

        [HttpPost("待審核資料M1")]
        [AllowAnonymous]
        public async Task<ActionResult<CheckRecM>> 待審核資料M1([FromBody] int eventId)
        {
            IQueryable<CheckRecM> result = from a in _PMDSdb.業者資料表s
                                           join d in _PMDSdb.稽查事件_主表s
                                               on a.主鍵 equals d.業者編號
                                           where d.稽查事件編號 == eventId
                                           select new CheckRecM()
                                           {
                                               稽查單號 = d.稽查事件編號,
                                               稽查人員 = d.稽查人員姓名,
                                               機構代碼 = "",
                                               稽查日期 = d.稽查日期,
                                               業者名稱 = a.市招名稱,
                                               業者電子信箱 = a.負責人電子郵件,
                                               負責人 = a.負責人姓名,
                                               業者電話 = a.電話,
                                               業者地址_縣市主鍵 = a.營業地址_縣市主鍵,
                                               業者地址_鄉鎮區主鍵 = a.營業地址_鄉鎮區主鍵,
                                               業者地址 = a.營業地址_街道
                                           };

            var res = await result.ToListAsync();
            var datas = res;
            var data = datas.FirstOrDefault();

            return Ok(data);
        }

        [HttpPost("待審核資料D")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<CheckRecD>>> 待審核資料D([FromBody] int eventId)
        {
            IQueryable<CheckRecD> result = from a in _PMDSdb.物品稽查明細表s
                                           join d in _PMDSdb.稽查事件_主表s
                                               on a.稽查主檔編號 equals d.稽查事件編號
                                           where d.稽查事件編號 == eventId
                                           select new CheckRecD()
                                           {
                                               稽查單號 = a.物品稽查編號,
                                               稽查表單 = a.專案名稱,
                                               稽查人員 = d.稽查人員姓名,
                                               稽查日期 = d.稽查日期,
                                               限改日期 = null,
                                               稽查結果 = "",
                                               執行狀態 = d.稽查事件備註
                                           };

            var res = await result.ToListAsync();

            var data = res;

            return Ok(data);
        }

        [HttpPost("ExpiredFoodInspection")]
        public async Task<ActionResult<AddInspectionEventResponse>> SaveExpiredFoodInspection([FromBody] ExpiredFoodInspectionSaveRequest request)
        {
            if (request.EventId <= 0)
            {
                return BadRequest(new AddInspectionEventResponse
                {
                    Success = false,
                    Message = "稽查事件主鍵錯誤"
                });
            }

            var hasExpiredFood = request.HasExpiredFood?.Trim().ToUpperInvariant();
            if (hasExpiredFood is not null && hasExpiredFood != "Y" && hasExpiredFood != "N")
            {
                return BadRequest(new AddInspectionEventResponse
                {
                    Success = false,
                    Message = "有無貯存逾期食品或原料欄位值錯誤"
                });
            }

            var now = DateTime.Now;
            await using var tx = await _ECRSdb.Database.BeginTransactionAsync();

            try
            {
                var entity = await _ECRSdb.逾期食品稽查_主表s
                    .FirstOrDefaultAsync(x => x.稽查事件主鍵 == request.EventId);

                if (entity == null)
                {
                    entity = new 逾期食品稽查_主表
                    {
                        稽查事件主鍵 = request.EventId,
                        稽查結果代碼 = "1",
                        作業負責人員主鍵 = request.InspectionUserId,
                        稽查人員主鍵 = request.InspectionUserId,
                        承辦人員主鍵 = request.InspectionUserId,
                        異動人員主鍵 = request.InspectionUserId,
                        結案狀態 = "N",
                        異動時間 = now,
                        建立時間 = now
                    };

                    _ECRSdb.逾期食品稽查_主表s.Add(entity);
                }

                entity.有無貯存逾期食品或原料 = hasExpiredFood;
                entity.現場稽查描述 = request.InspectionDescription;
                entity.異動人員主鍵 = request.InspectionUserId;
                entity.異動時間 = now;

                await _ECRSdb.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new AddInspectionEventResponse
                {
                    Success = true,
                    EventId = request.EventId
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return BadRequest(new AddInspectionEventResponse
                {
                    Success = false,
                    Message = "儲存逾期食品稽查資料失敗",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message ?? string.Empty
                });
            }
        }

        [HttpPost("SourceDocumentInspection")]
        public async Task<ActionResult<AddInspectionEventResponse>> SaveSourceDocumentInspection([FromBody] SourceDocumentInspectionSaveRequest request)
        {
            if (request.EventId <= 0)
            {
                return BadRequest(new AddInspectionEventResponse
                {
                    Success = false,
                    Message = "稽查事件主鍵不可為空"
                });
            }

            var sourceDocumentSaved = NormalizeYesNoOrNull(request.SourceDocumentSaved);
            if (sourceDocumentSaved == null)
            {
                return BadRequest(new AddInspectionEventResponse
                {
                    Success = false,
                    Message = "是否保存來源文件欄位值錯誤"
                });
            }

            var now = DateTime.Now;
            await using var tx = await _ECRSdb.Database.BeginTransactionAsync();

            try
            {
                var entity = await _ECRSdb.來源文件保存稽查_主表s
                    .FirstOrDefaultAsync(x => x.稽查事件主鍵 == request.EventId);

                if (entity == null)
                {
                    entity = new ECRS_API.Models.ECRS.來源文件保存稽查_主表
                    {
                        稽查事件主鍵 = request.EventId,
                        稽查結果代碼 = "1",
                        作業負責人員主鍵 = request.InspectionUserId,
                        稽查人員主鍵 = request.InspectionUserId,
                        承辦人員主鍵 = request.InspectionUserId,
                        異動人員主鍵 = request.InspectionUserId,
                        結案狀態 = "N",
                        異動時間 = now,
                        建立時間 = now
                    };

                    _ECRSdb.來源文件保存稽查_主表s.Add(entity);
                }

                entity.以書面或電子化保存來源文件 = sourceDocumentSaved;
                entity.載明收貨日期或批號 = NormalizeYesNoOrNull(request.ReceiveDateOrBatchNoSpecified);
                entity.載明材料或成品之資訊 = NormalizeYesNoOrNull(request.MaterialOrProductInfoSpecified);
                entity.載明供應商資訊 = NormalizeYesNoOrNull(request.SupplierInfoSpecified);
                entity.現場稽查描述 = request.InspectionDescription;
                entity.異動人員主鍵 = request.InspectionUserId;
                entity.異動時間 = now;

                await _ECRSdb.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new AddInspectionEventResponse
                {
                    Success = true,
                    EventId = request.EventId,
                    Message = "儲存成功"
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return BadRequest(new AddInspectionEventResponse
                {
                    Success = false,
                    Message = "儲存來源文件保存稽查資料失敗",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message ?? string.Empty
                });
            }
        }

        [HttpPost("HealthManagerInspection")]
        public async Task<ActionResult<AddInspectionEventResponse>> SaveHealthManagerInspection([FromBody] HealthManagerInspectionSaveRequest request)
        {
            if (request.EventId <= 0)
            {
                return BadRequest(new AddInspectionEventResponse
                {
                    Success = false,
                    Message = "稽查事件主鍵錯誤"
                });
            }

            var hasHealthManager = request.HasHealthManager?.Trim().ToUpperInvariant();
            if (hasHealthManager != "1" && hasHealthManager != "2" && hasHealthManager != "3")
            {
                return BadRequest(new AddInspectionEventResponse
                {
                    Success = false,
                    Message = "是否有衛生管理人員欄位值錯誤"
                });
            }

            var now = DateTime.Now;
            await using var tx = await _ECRSdb.Database.BeginTransactionAsync();

            try
            {
                var entity = await _ECRSdb.衛生管理人員查核_主表s
                    .FirstOrDefaultAsync(x => x.稽查事件主鍵 == request.EventId);

                if (entity == null)
                {
                    entity = new ECRS_API.Models.ECRS.衛生管理人員查核_主表
                    {
                        稽查事件主鍵 = request.EventId,
                        稽查結果代碼 = "1",
                        作業負責人員主鍵 = request.InspectionUserId,
                        稽查人員主鍵 = request.InspectionUserId,
                        承辦人員主鍵 = request.InspectionUserId,
                        異動人員主鍵 = request.InspectionUserId,
                        結案狀態 = "N",
                        異動時間 = now,
                        建立時間 = now
                    };

                    _ECRSdb.衛生管理人員查核_主表s.Add(entity);
                }

                entity.是否有衛生管理人員 = hasHealthManager;
                if (hasHealthManager == "1")
                {
                    if (request.NoApprovalNo == true)
                    {
                        entity.核備字號 = null;
                        entity.核備中尚無字號 = "Y";
                    }
                    else
                    {
                        entity.核備中尚無字號 = null;
                        entity.核備字號 = string.IsNullOrWhiteSpace(request.ApprovalNo)
                            ? null
                            : request.ApprovalNo.Trim();
                    }

                    entity.不適用原因_無工廠登記 = null;
                    entity.不適用原因_未滿5人且未達3千萬 = null;
                }
                else if (hasHealthManager == "2")
                {
                    entity.核備字號 = null;
                    entity.核備中尚無字號 = null;
                    entity.不適用原因_無工廠登記 = null;
                    entity.不適用原因_未滿5人且未達3千萬 = null;
                }

                if (hasHealthManager == "3" && request.NoFactoryRegistrationNotApplicable == true)
                {
                    entity.不適用原因_無工廠登記 = "Y";
                }

                if (hasHealthManager == "3" && request.SmallScaleManufacturerNotApplicable == true)
                {
                    entity.不適用原因_未滿5人且未達3千萬 = "Y";
                }

                entity.現場稽查描述 = request.InspectionDescription;
                entity.異動人員主鍵 = request.InspectionUserId;
                entity.異動時間 = now;

                await _ECRSdb.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new AddInspectionEventResponse
                {
                    Success = true,
                    EventId = request.EventId
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return BadRequest(new AddInspectionEventResponse
                {
                    Success = false,
                    Message = "儲存衛生管理人員查核資料失敗",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message ?? string.Empty
                });
            }
        }

        [HttpPost("ProfessionalLicenseInspection")]
        public async Task<ActionResult<AddInspectionEventResponse>> SaveProfessionalLicenseInspection([FromBody] ProfessionalLicenseInspectionSaveRequest request)
        {
            if (request.EventId <= 0)
            {
                return BadRequest(new AddInspectionEventResponse
                {
                    Success = false,
                    Message = "稽查事件主鍵不可為空"
                });
            }

            var professionalLicenseStatus = request.ProfessionalLicenseStatus?.Trim().ToUpperInvariant();
            if (professionalLicenseStatus != "1" && professionalLicenseStatus != "2" && professionalLicenseStatus != "3")
            {
                return BadRequest(new AddInspectionEventResponse
                {
                    Success = false,
                    Message = "聘用食品業者專門職業或技術證照人員查核結果格式錯誤"
                });
            }

            var trainingStatus = request.TrainingStatus?.Trim().ToUpperInvariant();
            if (professionalLicenseStatus == "1" && trainingStatus != "Y" && trainingStatus != "N")
            {
                return BadRequest(new AddInspectionEventResponse
                {
                    Success = false,
                    Message = "登錄各該人員資料及衛生講習或訓練時數查核結果格式錯誤"
                });
            }

            var now = DateTime.Now;
            await using var tx = await _ECRSdb.Database.BeginTransactionAsync();

            try
            {
                var entity = await _ECRSdb.專門職業或技術證照人員查核_主表s
                    .FirstOrDefaultAsync(x => x.稽查事件主鍵 == request.EventId);

                if (entity == null)
                {
                    entity = new ECRS_API.Models.ECRS.專門職業或技術證照人員查核_主表
                    {
                        稽查事件主鍵 = request.EventId,
                        稽查結果代碼 = "1",
                        作業負責人員主鍵 = request.InspectionUserId,
                        稽查人員主鍵 = request.InspectionUserId,
                        承辦人員主鍵 = request.InspectionUserId,
                        異動人員主鍵 = request.InspectionUserId,
                        結案狀態 = "N",
                        異動時間 = now,
                        建立時間 = now
                    };

                    _ECRSdb.專門職業或技術證照人員查核_主表s.Add(entity);
                }

                entity.聘用食品專職或技術證照人員 = professionalLicenseStatus;
                entity.登錄各該人員資料及衛生講習或訓練時數 = professionalLicenseStatus == "1" ? trainingStatus : null;
                entity.不適用原因_無工廠登記 = professionalLicenseStatus == "3" && request.NoFactoryRegistrationNotApplicable == true ? "Y" : "N";
                entity.不適用原因_資本額未達3千萬 = professionalLicenseStatus == "3" && request.CapitalUnderThirtyMillionNotApplicable == true ? "Y" : "N";
                entity.不適用原因_食品從業人員未滿20人 = professionalLicenseStatus == "3" && request.FoodWorkersUnderTwentyNotApplicable == true ? "Y" : "N";
                entity.不適用原因_非規範業別 = professionalLicenseStatus == "3" && request.NonRegulatedBusinessNotApplicable == true ? "Y" : "N";
                entity.現場稽查描述 = request.InspectionDescription;
                entity.異動人員主鍵 = request.InspectionUserId;
                entity.異動時間 = now;

                await _ECRSdb.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new AddInspectionEventResponse
                {
                    Success = true,
                    EventId = request.EventId,
                    Message = "儲存成功"
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return BadRequest(new AddInspectionEventResponse
                {
                    Success = false,
                    Message = "儲存專門職業或技術證照人員查核資料失敗",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message ?? string.Empty
                });
            }
        }

        [HttpGet("InspectionItemNames")]
        public async Task<ActionResult<IEnumerable<string>>> InspectionItemNames([FromQuery] string? inspectionId)
        {
            inspectionId = WebUtility.UrlDecode(inspectionId ?? string.Empty);

            if (string.IsNullOrWhiteSpace(inspectionId))
            {
                return Ok(Array.Empty<string>());
            }

            var inspectionIds = inspectionId
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => int.TryParse(x, out _))
                .Select(int.Parse)
                .Distinct()
                .ToList();

            if (inspectionIds.Count == 0)
            {
                return BadRequest("InspectionId 格式錯誤");
            }

            var itemNames = await _ECRSdb.專案名稱_稽查項目代碼表s
                .Where(x => inspectionIds.Contains(x.稽查項目代碼))
                .Select(x => x.稽查項目)
                .Where(x => x != null && x != string.Empty)
                .Distinct()
                .ToListAsync();

            return Ok(itemNames);
        }

    }
}
