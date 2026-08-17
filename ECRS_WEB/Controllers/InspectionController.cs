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
using ECRS_WEB.DTOs.InspectionDTO.InspectionForms;
using ECRS_WEB.DTOs.InspectionDTO.InspectionQry;
using ECRS_WEB.DTOs.InspectionDTO.Flist;
using ECRS_WEB.Helpers;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Razor;
using System.IO.Compression;

namespace CoreWebApp.Controllers
{
    [Authorize]
    public class InspectionController : Controller
    {
        private readonly ReadPMDSDTApiClient _apiPMDS;
        private readonly ReadECRSDTApiClient _apiECRS;
        private readonly ILogger<InspectionController> _logger;
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly IWebHostEnvironment _environment;
        private const long MaxUploadFileSizeBytes = 4 * 1024 * 1024;

        public InspectionController(ReadPMDSDTApiClient apiPMDS, ReadECRSDTApiClient apiECRS, ILogger<InspectionController> logger, IRazorViewEngine razorViewEngine, IWebHostEnvironment environment)
        {
            _apiPMDS = apiPMDS;
            _apiECRS = apiECRS;
            _logger = logger;
            _razorViewEngine = razorViewEngine;
            _environment = environment;
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
        public async Task<IActionResult> InspectionForms(string? companyId, int projectId, string? eventId)
        {
            companyId = QueryStringSecurityHelper.UrlDecode(companyId);
            eventId = QueryStringSecurityHelper.UrlDecode(eventId);

            if (string.IsNullOrWhiteSpace(companyId) || projectId <= 0)
            {
                return RedirectToAction("Fquery", "Inspection");
            }

            var vm = new InspectionFormsViewModel
            {
                CompanyId = companyId ?? string.Empty,
                ProjectId = projectId,
                eventId = eventId ?? string.Empty,
                InspectorName = HttpContext.Session.GetString("DisplayName") ?? string.Empty
            };

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
                    InspectorName = HttpContext.Session.GetString("DisplayName") ?? string.Empty
                };

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

        private static string? NormalizeYesNoOrNull(string? value)
        {
            var normalized = value?.Trim().ToUpperInvariant();
            return normalized == "Y" || normalized == "N" ? normalized : null;
        }

        public async Task<IActionResult> InspectionFormContent(string? InspectionId, string? inspectionItemName, string? encodedEventId)
        {
            InspectionId = QueryStringSecurityHelper.UrlDecode(InspectionId);
            inspectionItemName = QueryStringSecurityHelper.UrlDecode(inspectionItemName);
            encodedEventId = QueryStringSecurityHelper.UrlDecode(encodedEventId);

            var hasInspectionId = !string.IsNullOrWhiteSpace(InspectionId);
            ViewBag.InspectionItemName = inspectionItemName?.Trim() ?? string.Empty;
            ViewBag.HasInspectionId = hasInspectionId;
            ViewBag.EventId = encodedEventId ?? string.Empty;
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

            var partialViewNames = ViewBag.PartialViewNames as IEnumerable<string> ?? Enumerable.Empty<string>();
            var shouldLoadExpiredFoodInspection =
                string.Equals(ViewBag.InspectionItemName as string, "逾期食品", StringComparison.OrdinalIgnoreCase)
                || partialViewNames.Any(x => x.Contains("_逾期食品Partial", StringComparison.OrdinalIgnoreCase));

            if (shouldLoadExpiredFoodInspection && int.TryParse(ViewBag.EventId as string, out var expiredFoodEventId))
            {
                try
                {
                    ViewBag.ExpiredFoodInspection = await _apiECRS.GetExpiredFoodInspection(expiredFoodEventId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "InspectionFormContent 查詢逾期食品稽查資料失敗，eventId={EventId}", expiredFoodEventId);
                    ViewBag.ExpiredFoodInspection = null;
                }
            }

            var shouldLoadSourceDocumentInspection =
                string.Equals(ViewBag.InspectionItemName as string, "保存來源文件", StringComparison.OrdinalIgnoreCase)
                || partialViewNames.Any(x => x.Contains("_保存來源文件Partial", StringComparison.OrdinalIgnoreCase));

            if (shouldLoadSourceDocumentInspection && int.TryParse(ViewBag.EventId as string, out var sourceDocumentEventId))
            {
                try
                {
                    ViewBag.SourceDocumentInspection = await _apiECRS.GetSourceDocumentInspection(sourceDocumentEventId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "InspectionFormContent 查詢保存來源文件稽查資料失敗，eventId={EventId}", sourceDocumentEventId);
                    ViewBag.SourceDocumentInspection = null;
                }
            }

            var shouldLoadHealthManagerInspection =
                string.Equals(ViewBag.InspectionItemName as string, "衛生管理人員", StringComparison.OrdinalIgnoreCase)
                || partialViewNames.Any(x => x.Contains("_衛生管理人員Partial", StringComparison.OrdinalIgnoreCase));

            if (shouldLoadHealthManagerInspection && int.TryParse(ViewBag.EventId as string, out var healthManagerEventId))
            {
                try
                {
                    ViewBag.HealthManagerInspection = await _apiECRS.GetHealthManagerInspection(healthManagerEventId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "InspectionFormContent 查詢衛生管理人員稽查資料失敗，eventId={EventId}", healthManagerEventId);
                    ViewBag.HealthManagerInspection = null;
                }
            }

            var shouldLoadProfessionalLicenseInspection =
                string.Equals(ViewBag.InspectionItemName as string, "專門職業或技術證照人員查核", StringComparison.OrdinalIgnoreCase)
                || partialViewNames.Any(x => x.Contains("_專門職業或技術證照人員查核Partial", StringComparison.OrdinalIgnoreCase));

            if (shouldLoadProfessionalLicenseInspection && int.TryParse(ViewBag.EventId as string, out var professionalLicenseEventId))
            {
                try
                {
                    ViewBag.ProfessionalLicenseInspection = await _apiECRS.GetProfessionalLicenseInspection(professionalLicenseEventId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "InspectionFormContent 讀取專門職業或技術證照人員查核稽查資料失敗，EventId={EventId}", professionalLicenseEventId);
                    ViewBag.ProfessionalLicenseInspection = null;
                }
            }

            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                return PartialView("InspectionFormContent");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveExpiredFoodInspection([FromBody] ExpiredFoodInspectionSaveRequest request)
        {
            if (request.EventId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "稽查事件主鍵錯誤"
                });
            }

            var hasExpiredFood = request.HasExpiredFood?.Trim().ToUpperInvariant();
            if (hasExpiredFood != "Y" && hasExpiredFood != "N")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "請選擇有無貯存逾期食品或原料"
                });
            }

            request.HasExpiredFood = hasExpiredFood;
            request.InspectionUserId = HttpContext.Session.GetString("InspectionId");

            var result = await _apiECRS.SaveExpiredFoodInspection(request);
            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = string.IsNullOrWhiteSpace(result.Message) ? "儲存失敗" : result.Message
                });
            }

            return Ok(new
            {
                success = true
            });
        }

        [HttpPost]
        public async Task<IActionResult> UploadExpiredFoodPhoto([FromForm] string encodedEventId, [FromForm] IFormFile photo)
        {
            if (string.IsNullOrWhiteSpace(encodedEventId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "稽查事件主鍵不可為空"
                });
            }

            if (photo == null || photo.Length == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "上傳照片失敗"
                });
            }

            if (photo.Length > MaxUploadFileSizeBytes)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "上傳圖檔大小不得超過4MB"
                });
            }

            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (extension != ".jpg" && extension != ".png")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "請上傳副檔名為 jpg 或 png 的圖檔。"
                });
            }

            var savedZip = (ZipPath: string.Empty, ZipFileName: string.Empty);

            AddInspectionEventResponse result;
            try
            {
                savedZip = await SaveExpiredFoodPhotoZip(encodedEventId, photo);
                result = await _apiECRS.UploadExpiredFoodInspectionPhoto(encodedEventId, photo, savedZip.ZipFileName);
            }
            catch (Exception ex)
            {
                DeleteFileIfExists(savedZip.ZipPath);
                _logger.LogError(ex, "UploadExpiredFoodPhoto failed. encodedEventId={EncodedEventId}, fileName={FileName}", encodedEventId, photo.FileName);
                var message = ex is InvalidOperationException && ex.Message == "上傳圖檔大小不得超過4MB"
                    ? ex.Message
                    : "上傳照片失敗";

                return BadRequest(new
                {
                    success = false,
                    message
                });
            }

            if (!result.Success)
            {
                DeleteFileIfExists(savedZip.ZipPath);
                return BadRequest(new
                {
                    success = false,
                    message = string.IsNullOrWhiteSpace(result.Message) ? "上傳照片失敗" : result.Message
                });
            }

            return Ok(new
            {
                success = true,
                message = "上傳照片已成功",
                zipFileName = savedZip.ZipFileName
            });
        }

        [HttpPost]
        public async Task<IActionResult> UploadExpiredFoodAttachment([FromForm] string encodedEventId, [FromForm] List<IFormFile> attachments)
        {
            if (string.IsNullOrWhiteSpace(encodedEventId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "稽查事件主鍵不可為空"
                });
            }

            if (attachments == null || attachments.Count == 0 || attachments.Any(x => x == null || x.Length == 0))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "上傳附件失敗"
                });
            }

            var imageFile = attachments.FirstOrDefault(IsImageUploadFile);
            if (imageFile != null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "請選擇非圖片檔案上傳"
                });
            }

            var oversizedFile = attachments.FirstOrDefault(x => x.Length > MaxUploadFileSizeBytes);
            if (oversizedFile != null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"上傳檔案大小不得超過4MB：{Path.GetFileName(oversizedFile.FileName)}"
                });
            }

            var uploadedCount = 0;
            foreach (var attachment in attachments)
            {
                var savedZip = (ZipPath: string.Empty, ZipFileName: string.Empty);

                AddInspectionEventResponse result;
                try
                {
                    savedZip = await SaveExpiredFoodAttachmentZip(encodedEventId, attachment);
                    result = await _apiECRS.UploadExpiredFoodInspectionAttachment(encodedEventId, attachment, savedZip.ZipFileName);
                }
                catch (Exception ex)
                {
                    DeleteFileIfExists(savedZip.ZipPath);
                    _logger.LogError(ex, "UploadExpiredFoodAttachment failed. encodedEventId={EncodedEventId}, fileName={FileName}", encodedEventId, attachment.FileName);
                    var message = ex is InvalidOperationException && ex.Message == "上傳檔案大小不得超過4MB"
                        ? ex.Message
                        : "上傳附件失敗";

                    return BadRequest(new
                    {
                        success = false,
                        message
                    });
                }

                if (!result.Success)
                {
                    DeleteFileIfExists(savedZip.ZipPath);
                    return BadRequest(new
                    {
                        success = false,
                        message = string.IsNullOrWhiteSpace(result.Message) ? "上傳附件失敗" : result.Message
                    });
                }

                uploadedCount++;
            }

            return Ok(new
            {
                success = true,
                message = $"上傳附件已成功，共 {uploadedCount} 個"
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetExpiredFoodPhotos(string encodedEventId)
        {
            if (string.IsNullOrWhiteSpace(encodedEventId))
            {
                return Ok(Array.Empty<InspectionUploadFileResult>());
            }

            var files = await _apiECRS.GetExpiredFoodInspectionPhotos(encodedEventId);
            return Ok(files);
        }

        [HttpGet]
        public async Task<IActionResult> GetExpiredFoodAttachments(string encodedEventId)
        {
            if (string.IsNullOrWhiteSpace(encodedEventId))
            {
                return Ok(Array.Empty<InspectionUploadFileResult>());
            }

            var files = await _apiECRS.GetExpiredFoodInspectionAttachments(encodedEventId);
            return Ok(files);
        }

        [HttpGet]
        public IActionResult PreviewExpiredFoodPhoto(string zipFileName)
        {
            if (string.IsNullOrWhiteSpace(zipFileName))
            {
                return BadRequest();
            }

            var safeZipFileName = Path.GetFileName(zipFileName);
            if (!safeZipFileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest();
            }

            var photoDirectory = Path.Combine(_environment.ContentRootPath, "Files", "Inspection", "Photo");
            var zipPath = Path.GetFullPath(Path.Combine(photoDirectory, safeZipFileName));
            var photoRoot = Path.GetFullPath(photoDirectory);
            if (!photoRoot.EndsWith(Path.DirectorySeparatorChar))
            {
                photoRoot += Path.DirectorySeparatorChar;
            }

            if (!zipPath.StartsWith(photoRoot, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(zipPath))
            {
                return NotFound();
            }

            using var archive = ZipFile.OpenRead(zipPath);
            var entry = archive.Entries.FirstOrDefault(x =>
            {
                var extension = Path.GetExtension(x.Name).ToLowerInvariant();
                return extension == ".jpg" || extension == ".png";
            });

            if (entry == null)
            {
                return NotFound();
            }

            using var entryStream = entry.Open();
            using var imageStream = new MemoryStream();
            entryStream.CopyTo(imageStream);
            var imageBytes = imageStream.ToArray();
            var contentType = Path.GetExtension(entry.Name).Equals(".png", StringComparison.OrdinalIgnoreCase)
                ? "image/png"
                : "image/jpeg";

            return File(imageBytes, contentType);
        }

        [HttpGet]
        public IActionResult PreviewExpiredFoodAttachment(string zipFileName)
        {
            if (string.IsNullOrWhiteSpace(zipFileName))
            {
                return BadRequest();
            }

            var safeZipFileName = Path.GetFileName(zipFileName);
            if (!safeZipFileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest();
            }

            var attachmentDirectory = Path.Combine(_environment.ContentRootPath, "Files", "Inspection", "Attachment");
            var zipPath = Path.GetFullPath(Path.Combine(attachmentDirectory, safeZipFileName));
            var attachmentRoot = Path.GetFullPath(attachmentDirectory);
            if (!attachmentRoot.EndsWith(Path.DirectorySeparatorChar))
            {
                attachmentRoot += Path.DirectorySeparatorChar;
            }

            if (!zipPath.StartsWith(attachmentRoot, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(zipPath))
            {
                return NotFound();
            }

            using var archive = ZipFile.OpenRead(zipPath);
            var entry = archive.Entries.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Name));
            if (entry == null)
            {
                return NotFound();
            }

            using var entryStream = entry.Open();
            using var fileStream = new MemoryStream();
            entryStream.CopyTo(fileStream);

            return File(fileStream.ToArray(), GetContentTypeByExtension(Path.GetExtension(entry.Name)), Path.GetFileName(entry.Name));
        }

        private IActionResult PreviewInspectionZip(string zipFileName, string directoryName, bool onlyImages)
        {
            if (string.IsNullOrWhiteSpace(zipFileName))
            {
                return BadRequest();
            }

            var safeZipFileName = Path.GetFileName(zipFileName);
            if (!safeZipFileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest();
            }

            var uploadDirectory = Path.Combine(_environment.ContentRootPath, "Files", "Inspection", directoryName);
            var zipPath = Path.GetFullPath(Path.Combine(uploadDirectory, safeZipFileName));
            var uploadRoot = Path.GetFullPath(uploadDirectory);
            if (!uploadRoot.EndsWith(Path.DirectorySeparatorChar))
            {
                uploadRoot += Path.DirectorySeparatorChar;
            }

            if (!zipPath.StartsWith(uploadRoot, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(zipPath))
            {
                return NotFound();
            }

            using var archive = ZipFile.OpenRead(zipPath);
            var entry = archive.Entries.FirstOrDefault(x =>
            {
                if (string.IsNullOrWhiteSpace(x.Name))
                {
                    return false;
                }

                if (!onlyImages)
                {
                    return true;
                }

                var extension = Path.GetExtension(x.Name).ToLowerInvariant();
                return extension == ".jpg" || extension == ".png";
            });

            if (entry == null)
            {
                return NotFound();
            }

            using var entryStream = entry.Open();
            using var fileStream = new MemoryStream();
            entryStream.CopyTo(fileStream);

            var extensionName = Path.GetExtension(entry.Name);
            var contentType = GetContentTypeByExtension(extensionName);
            return onlyImages
                ? File(fileStream.ToArray(), contentType)
                : File(fileStream.ToArray(), contentType, Path.GetFileName(entry.Name));
        }

        private async Task<(string ZipPath, string ZipFileName)> SaveExpiredFoodPhotoZip(string encodedEventId, IFormFile photo)
        {
            var originalFileName = Path.GetFileName(photo.FileName);
            var safeEventId = SanitizeFileNamePart(encodedEventId);
            var zipFileName = $"{safeEventId}_Photo_{DateTime.Now:yyyyMMddHHmmssfff}.zip";

            await using var zipStream = new MemoryStream();
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = archive.CreateEntry(originalFileName, CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                await using var uploadedStream = photo.OpenReadStream();
                await uploadedStream.CopyToAsync(entryStream);
            }

            if (zipStream.Length > MaxUploadFileSizeBytes)
            {
                throw new InvalidOperationException("上傳圖檔大小不得超過4MB");
            }

            var photoDirectory = Path.Combine(_environment.ContentRootPath, "Files", "Inspection", "Photo");
            Directory.CreateDirectory(photoDirectory);

            var zipPath = Path.Combine(photoDirectory, zipFileName);
            await System.IO.File.WriteAllBytesAsync(zipPath, zipStream.ToArray());
            return (zipPath, zipFileName);
        }

        private async Task<(string ZipPath, string ZipFileName)> SaveExpiredFoodAttachmentZip(string encodedEventId, IFormFile attachment)
        {
            var originalFileName = Path.GetFileName(attachment.FileName);
            var safeEventId = SanitizeFileNamePart(encodedEventId);
            var zipFileName = $"{safeEventId}_Attachment_{DateTime.Now:yyyyMMddHHmmssfff}.zip";

            await using var zipStream = new MemoryStream();
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = archive.CreateEntry(originalFileName, CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                await using var uploadedStream = attachment.OpenReadStream();
                await uploadedStream.CopyToAsync(entryStream);
            }

            if (zipStream.Length > MaxUploadFileSizeBytes)
            {
                throw new InvalidOperationException("上傳檔案大小不得超過4MB");
            }

            var attachmentDirectory = Path.Combine(_environment.ContentRootPath, "Files", "Inspection", "Attachment");
            Directory.CreateDirectory(attachmentDirectory);

            var zipPath = Path.Combine(attachmentDirectory, zipFileName);
            await System.IO.File.WriteAllBytesAsync(zipPath, zipStream.ToArray());
            return (zipPath, zipFileName);
        }

        private Task<(string ZipPath, string ZipFileName)> SaveHealthManagerPhotoZip(string encodedEventId, IFormFile photo)
        {
            return SaveInspectionZip(encodedEventId, photo, "HealthManagerPhoto", "Photo", "上傳檔案大小不能超過4MB");
        }

        private Task<(string ZipPath, string ZipFileName)> SaveHealthManagerAttachmentZip(string encodedEventId, IFormFile attachment)
        {
            return SaveInspectionZip(encodedEventId, attachment, "HealthManagerAttachment", "Attachment", "上傳檔案大小不能超過4MB");
        }

        private Task<(string ZipPath, string ZipFileName)> SaveProfessionalLicenseAttachmentZip(string encodedEventId, IFormFile attachment)
        {
            return SaveInspectionZip(encodedEventId, attachment, "ProfessionalLicenseAttachment", "Attachment", "上傳檔案大小不能超過4MB");
        }

        private Task<(string ZipPath, string ZipFileName)> SaveSourceDocumentAttachmentZip(string encodedEventId, IFormFile attachment)
        {
            return SaveInspectionZip(encodedEventId, attachment, "SourceDocumentAttachment", "Attachment", "上傳檔案大小不能超過4MB");
        }

        private Task<(string ZipPath, string ZipFileName)> SaveSourceDocumentPhotoZip(string encodedEventId, IFormFile photo)
        {
            return SaveInspectionZip(encodedEventId, photo, "SourceDocumentPhoto", "Photo", "上傳檔案大小不能超過4MB");
        }

        private Task<(string ZipPath, string ZipFileName)> SaveProfessionalLicensePhotoZip(string encodedEventId, IFormFile photo)
        {
            return SaveInspectionZip(encodedEventId, photo, "ProfessionalLicensePhoto", "Photo", "上傳檔案大小不能超過4MB");
        }

        private async Task<(string ZipPath, string ZipFileName)> SaveInspectionZip(
            string encodedEventId,
            IFormFile uploadFile,
            string zipKind,
            string directoryName,
            string oversizedMessage)
        {
            var originalFileName = Path.GetFileName(uploadFile.FileName);
            var safeEventId = SanitizeFileNamePart(encodedEventId);
            var zipFileName = $"{safeEventId}_{zipKind}_{DateTime.Now:yyyyMMddHHmmssfff}.zip";

            await using var zipStream = new MemoryStream();
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = archive.CreateEntry(originalFileName, CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                await using var uploadedStream = uploadFile.OpenReadStream();
                await uploadedStream.CopyToAsync(entryStream);
            }

            if (zipStream.Length > MaxUploadFileSizeBytes)
            {
                throw new InvalidOperationException(oversizedMessage);
            }

            var uploadDirectory = Path.Combine(_environment.ContentRootPath, "Files", "Inspection", directoryName);
            Directory.CreateDirectory(uploadDirectory);

            var zipPath = Path.Combine(uploadDirectory, zipFileName);
            await System.IO.File.WriteAllBytesAsync(zipPath, zipStream.ToArray());
            return (zipPath, zipFileName);
        }

        private static string GetContentTypeByExtension(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".csv" => "text/csv",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".zip" => "application/zip",
                _ => "application/octet-stream"
            };
        }

        private static string SanitizeFileNamePart(string value)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var chars = value
                .Trim()
                .Select(ch => invalidChars.Contains(ch) ? '_' : ch)
                .ToArray();

            var sanitized = new string(chars);
            return string.IsNullOrWhiteSpace(sanitized) ? "Event" : sanitized;
        }

        private static void DeleteFileIfExists(string? path)
        {
            if (!string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveHealthManagerInspection([FromBody] HealthManagerInspectionSaveRequest request)
        {
            if (request.EventId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "稽查事件主鍵錯誤"
                });
            }

            var hasHealthManager = request.HasHealthManager?.Trim().ToUpperInvariant();
            if (hasHealthManager != "1" && hasHealthManager != "2" && hasHealthManager != "3")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "請選擇是否有衛生管理人員"
                });
            }

            request.HasHealthManager = hasHealthManager;
            request.InspectionUserId = HttpContext.Session.GetString("InspectionId");

            var result = await _apiECRS.SaveHealthManagerInspection(request);
            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = string.IsNullOrWhiteSpace(result.Message) ? "儲存失敗" : result.Message
                });
            }

            return Ok(new
            {
                success = true
            });
        }

        [HttpPost]
        public async Task<IActionResult> UploadHealthManagerPhoto([FromForm] string encodedEventId, [FromForm] IFormFile photo)
        {
            if (string.IsNullOrWhiteSpace(encodedEventId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "稽查表單編號錯誤，無法上傳"
                });
            }

            if (photo == null || photo.Length == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "上傳照片失敗"
                });
            }

            if (photo.Length > MaxUploadFileSizeBytes)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "上傳檔案大小不能超過4MB"
                });
            }

            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (extension != ".jpg" && extension != ".png")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "照片格式不符，僅允許 jpg 或 png"
                });
            }

            var savedZip = (ZipPath: string.Empty, ZipFileName: string.Empty);
            AddInspectionEventResponse result;

            try
            {
                savedZip = await SaveHealthManagerPhotoZip(encodedEventId, photo);
                result = await _apiECRS.UploadHealthManagerInspectionPhoto(encodedEventId, photo, savedZip.ZipFileName);
            }
            catch (Exception ex)
            {
                DeleteFileIfExists(savedZip.ZipPath);
                _logger.LogError(ex, "UploadHealthManagerPhoto failed. encodedEventId={EncodedEventId}, fileName={FileName}", encodedEventId, photo.FileName);

                return BadRequest(new
                {
                    success = false,
                    message = ex is InvalidOperationException ? ex.Message : "上傳照片失敗"
                });
            }

            if (!result.Success)
            {
                DeleteFileIfExists(savedZip.ZipPath);
                return BadRequest(new
                {
                    success = false,
                    message = string.IsNullOrWhiteSpace(result.Message) ? "上傳照片失敗" : result.Message
                });
            }

            return Ok(new
            {
                success = true,
                message = "上傳照片已成功",
                zipFileName = savedZip.ZipFileName
            });
        }

        [HttpPost]
        public async Task<IActionResult> UploadHealthManagerAttachment([FromForm] string encodedEventId, [FromForm] List<IFormFile> attachments)
        {
            if (string.IsNullOrWhiteSpace(encodedEventId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "稽查表單編號錯誤，無法上傳"
                });
            }

            if (attachments == null || attachments.Count == 0 || attachments.Any(x => x == null || x.Length == 0))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "上傳附件失敗"
                });
            }

            var imageFile = attachments.FirstOrDefault(IsImageUploadFile);
            if (imageFile != null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"請至照片頁籤上傳圖檔：{Path.GetFileName(imageFile.FileName)}"
                });
            }

            var oversizedFile = attachments.FirstOrDefault(x => x.Length > MaxUploadFileSizeBytes);
            if (oversizedFile != null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"上傳檔案大小不能超過4MB：{Path.GetFileName(oversizedFile.FileName)}"
                });
            }

            var uploadedCount = 0;
            foreach (var attachment in attachments)
            {
                var savedZip = (ZipPath: string.Empty, ZipFileName: string.Empty);
                AddInspectionEventResponse result;

                try
                {
                    savedZip = await SaveHealthManagerAttachmentZip(encodedEventId, attachment);
                    result = await _apiECRS.UploadHealthManagerInspectionAttachment(encodedEventId, attachment, savedZip.ZipFileName);
                }
                catch (Exception ex)
                {
                    DeleteFileIfExists(savedZip.ZipPath);
                    _logger.LogError(ex, "UploadHealthManagerAttachment failed. encodedEventId={EncodedEventId}, fileName={FileName}", encodedEventId, attachment.FileName);

                    return BadRequest(new
                    {
                        success = false,
                        message = ex is InvalidOperationException ? ex.Message : "上傳附件失敗"
                    });
                }

                if (!result.Success)
                {
                    DeleteFileIfExists(savedZip.ZipPath);
                    return BadRequest(new
                    {
                        success = false,
                        message = string.IsNullOrWhiteSpace(result.Message) ? "上傳附件失敗" : result.Message
                    });
                }

                uploadedCount++;
            }

            return Ok(new
            {
                success = true,
                message = $"上傳附件已成功，共 {uploadedCount} 個"
            });
        }

        private static bool IsImageUploadFile(IFormFile file)
        {
            var contentType = file.ContentType?.Trim().ToLowerInvariant() ?? string.Empty;
            if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".tif" or ".tiff" or ".svg" or ".heic" or ".heif";
        }

        [HttpGet]
        public async Task<IActionResult> GetHealthManagerPhotos(string encodedEventId)
        {
            if (string.IsNullOrWhiteSpace(encodedEventId))
            {
                return Ok(Array.Empty<InspectionUploadFileResult>());
            }

            var files = await _apiECRS.GetHealthManagerInspectionPhotos(encodedEventId);
            return Ok(files);
        }

        [HttpGet]
        public async Task<IActionResult> GetHealthManagerAttachments(string encodedEventId)
        {
            if (string.IsNullOrWhiteSpace(encodedEventId))
            {
                return Ok(Array.Empty<InspectionUploadFileResult>());
            }

            var files = await _apiECRS.GetHealthManagerInspectionAttachments(encodedEventId);
            return Ok(files);
        }

        [HttpGet]
        public IActionResult PreviewHealthManagerPhoto(string zipFileName)
        {
            return PreviewInspectionZip(zipFileName, "Photo", onlyImages: true);
        }

        [HttpGet]
        public IActionResult PreviewHealthManagerAttachment(string zipFileName)
        {
            return PreviewInspectionZip(zipFileName, "Attachment", onlyImages: false);
        }

        [HttpPost]
        public async Task<IActionResult> UploadProfessionalLicensePhoto([FromForm] string encodedEventId, [FromForm] IFormFile photo)
        {
            if (string.IsNullOrWhiteSpace(encodedEventId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "稽查事件主鍵錯誤，無法上傳"
                });
            }

            if (photo == null || photo.Length == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "上傳照片失敗"
                });
            }

            if (photo.Length > MaxUploadFileSizeBytes)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "上傳檔案大小不能超過4MB"
                });
            }

            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (extension != ".jpg" && extension != ".png")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "照片格式不符，僅允許 jpg 或 png"
                });
            }

            var savedZip = (ZipPath: string.Empty, ZipFileName: string.Empty);
            AddInspectionEventResponse result;

            try
            {
                savedZip = await SaveProfessionalLicensePhotoZip(encodedEventId, photo);
                result = await _apiECRS.UploadProfessionalLicenseInspectionPhoto(encodedEventId, photo, savedZip.ZipFileName);
            }
            catch (Exception ex)
            {
                DeleteFileIfExists(savedZip.ZipPath);
                _logger.LogError(ex, "UploadProfessionalLicensePhoto failed. encodedEventId={EncodedEventId}, fileName={FileName}", encodedEventId, photo.FileName);

                return BadRequest(new
                {
                    success = false,
                    message = ex is InvalidOperationException ? ex.Message : "上傳照片失敗"
                });
            }

            if (!result.Success)
            {
                DeleteFileIfExists(savedZip.ZipPath);
                return BadRequest(new
                {
                    success = false,
                    message = string.IsNullOrWhiteSpace(result.Message) ? "上傳照片失敗" : result.Message
                });
            }

            return Ok(new
            {
                success = true,
                message = "上傳照片已成功",
                zipFileName = savedZip.ZipFileName
            });
        }

        [HttpPost]
        public async Task<IActionResult> UploadProfessionalLicenseAttachment([FromForm] string encodedEventId, [FromForm] List<IFormFile> attachments)
        {
            if (string.IsNullOrWhiteSpace(encodedEventId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "稽查事件主鍵錯誤，無法上傳"
                });
            }

            if (attachments == null || attachments.Count == 0 || attachments.Any(x => x == null || x.Length == 0))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "上傳附件失敗"
                });
            }

            var imageFile = attachments.FirstOrDefault(IsImageUploadFile);
            if (imageFile != null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "請使用照片頁籤上傳圖片檔"
                });
            }

            var oversizedFile = attachments.FirstOrDefault(x => x.Length > MaxUploadFileSizeBytes);
            if (oversizedFile != null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"上傳檔案大小不能超過4MB：{Path.GetFileName(oversizedFile.FileName)}"
                });
            }

            var uploadedCount = 0;
            foreach (var attachment in attachments)
            {
                var savedZip = (ZipPath: string.Empty, ZipFileName: string.Empty);
                AddInspectionEventResponse result;

                try
                {
                    savedZip = await SaveProfessionalLicenseAttachmentZip(encodedEventId, attachment);
                    result = await _apiECRS.UploadProfessionalLicenseInspectionAttachment(encodedEventId, attachment, savedZip.ZipFileName);
                }
                catch (Exception ex)
                {
                    DeleteFileIfExists(savedZip.ZipPath);
                    _logger.LogError(ex, "UploadProfessionalLicenseAttachment failed. encodedEventId={EncodedEventId}, fileName={FileName}", encodedEventId, attachment.FileName);

                    return BadRequest(new
                    {
                        success = false,
                        message = ex is InvalidOperationException ? ex.Message : "上傳附件失敗"
                    });
                }

                if (!result.Success)
                {
                    DeleteFileIfExists(savedZip.ZipPath);
                    return BadRequest(new
                    {
                        success = false,
                        message = string.IsNullOrWhiteSpace(result.Message) ? "上傳附件失敗" : result.Message
                    });
                }

                uploadedCount++;
            }

            return Ok(new
            {
                success = true,
                message = $"上傳附件已成功，共 {uploadedCount} 個"
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetProfessionalLicensePhotos(string encodedEventId)
        {
            if (string.IsNullOrWhiteSpace(encodedEventId))
            {
                return Ok(Array.Empty<InspectionUploadFileResult>());
            }

            var files = await _apiECRS.GetProfessionalLicenseInspectionPhotos(encodedEventId);
            return Ok(files);
        }

        [HttpGet]
        public async Task<IActionResult> GetProfessionalLicenseAttachments(string encodedEventId)
        {
            if (string.IsNullOrWhiteSpace(encodedEventId))
            {
                return Ok(Array.Empty<InspectionUploadFileResult>());
            }

            var files = await _apiECRS.GetProfessionalLicenseInspectionAttachments(encodedEventId);
            return Ok(files);
        }

        [HttpGet]
        public IActionResult PreviewProfessionalLicensePhoto(string zipFileName)
        {
            return PreviewInspectionZip(zipFileName, "Photo", onlyImages: true);
        }

        [HttpGet]
        public IActionResult PreviewProfessionalLicenseAttachment(string zipFileName)
        {
            return PreviewInspectionZip(zipFileName, "Attachment", onlyImages: false);
        }

        [HttpPost]
        public async Task<IActionResult> SaveSourceDocumentInspection([FromBody] SourceDocumentInspectionSaveRequest request)
        {
            if (request.EventId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "稽查事件主鍵不可為空"
                });
            }

            var sourceDocumentSaved = request.SourceDocumentSaved?.Trim().ToUpperInvariant();
            if (sourceDocumentSaved != "Y" && sourceDocumentSaved != "N")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "請選擇是否保存來源文件"
                });
            }

            request.SourceDocumentSaved = sourceDocumentSaved;
            request.ReceiveDateOrBatchNoSpecified = NormalizeYesNoOrNull(request.ReceiveDateOrBatchNoSpecified);
            request.MaterialOrProductInfoSpecified = NormalizeYesNoOrNull(request.MaterialOrProductInfoSpecified);
            request.SupplierInfoSpecified = NormalizeYesNoOrNull(request.SupplierInfoSpecified);
            request.InspectionUserId = HttpContext.Session.GetString("InspectionId");

            var result = await _apiECRS.SaveSourceDocumentInspection(request);
            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = string.IsNullOrWhiteSpace(result.Message) ? "儲存失敗" : result.Message
                });
            }

            return Ok(new
            {
                success = true,
                message = "儲存成功"
            });
        }

        [HttpPost]
        public async Task<IActionResult> UploadSourceDocumentAttachment([FromForm] string encodedEventId, [FromForm] List<IFormFile> attachments)
        {
            if (string.IsNullOrWhiteSpace(encodedEventId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "稽查事件主鍵不可為空"
                });
            }

            if (attachments == null || attachments.Count == 0 || attachments.Any(x => x == null || x.Length == 0))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "上傳附件失敗"
                });
            }

            var imageFile = attachments.FirstOrDefault(IsImageUploadFile);
            if (imageFile != null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"請至照片頁籤上傳圖檔：{Path.GetFileName(imageFile.FileName)}"
                });
            }

            var oversizedFile = attachments.FirstOrDefault(x => x.Length > MaxUploadFileSizeBytes);
            if (oversizedFile != null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"上傳檔案大小不得超過4MB：{Path.GetFileName(oversizedFile.FileName)}"
                });
            }

            var uploadedCount = 0;
            foreach (var attachment in attachments)
            {
                var savedZip = (ZipPath: string.Empty, ZipFileName: string.Empty);

                AddInspectionEventResponse result;
                try
                {
                    savedZip = await SaveSourceDocumentAttachmentZip(encodedEventId, attachment);
                    result = await _apiECRS.UploadSourceDocumentInspectionAttachment(encodedEventId, attachment, savedZip.ZipFileName);
                }
                catch (Exception ex)
                {
                    DeleteFileIfExists(savedZip.ZipPath);
                    _logger.LogError(ex, "UploadSourceDocumentAttachment failed. encodedEventId={EncodedEventId}, fileName={FileName}", encodedEventId, attachment.FileName);
                    var message = ex is InvalidOperationException && ex.Message == "上傳檔案大小不能超過4MB"
                        ? ex.Message
                        : "上傳附件失敗";

                    return BadRequest(new
                    {
                        success = false,
                        message
                    });
                }

                if (!result.Success)
                {
                    DeleteFileIfExists(savedZip.ZipPath);
                    return BadRequest(new
                    {
                        success = false,
                        message = string.IsNullOrWhiteSpace(result.Message) ? "上傳附件失敗" : result.Message
                    });
                }

                uploadedCount++;
            }

            return Ok(new
            {
                success = true,
                message = $"上傳附件已成功，共 {uploadedCount} 筆"
            });
        }

        [HttpPost]
        public async Task<IActionResult> UploadSourceDocumentPhoto([FromForm] string encodedEventId, [FromForm] IFormFile photo)
        {
            if (string.IsNullOrWhiteSpace(encodedEventId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "稽查事件主鍵不可為空"
                });
            }

            if (photo == null || photo.Length == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "上傳照片失敗"
                });
            }

            if (photo.Length > MaxUploadFileSizeBytes)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "上傳圖檔大小不得超過4MB"
                });
            }

            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (extension != ".jpg" && extension != ".png")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "照片格式不符，僅允許 jpg 或 png"
                });
            }

            var savedZip = (ZipPath: string.Empty, ZipFileName: string.Empty);

            AddInspectionEventResponse result;
            try
            {
                savedZip = await SaveSourceDocumentPhotoZip(encodedEventId, photo);
                result = await _apiECRS.UploadSourceDocumentInspectionPhoto(encodedEventId, photo, savedZip.ZipFileName);
            }
            catch (Exception ex)
            {
                DeleteFileIfExists(savedZip.ZipPath);
                _logger.LogError(ex, "UploadSourceDocumentPhoto failed. encodedEventId={EncodedEventId}, fileName={FileName}", encodedEventId, photo.FileName);
                var message = ex is InvalidOperationException && ex.Message == "上傳檔案大小不能超過4MB"
                    ? "上傳圖檔大小不得超過4MB"
                    : "上傳照片失敗";

                return BadRequest(new
                {
                    success = false,
                    message
                });
            }

            if (!result.Success)
            {
                DeleteFileIfExists(savedZip.ZipPath);
                return BadRequest(new
                {
                    success = false,
                    message = string.IsNullOrWhiteSpace(result.Message) ? "上傳照片失敗" : result.Message
                });
            }

            return Ok(new
            {
                success = true,
                message = "上傳照片已成功",
                zipFileName = savedZip.ZipFileName
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetSourceDocumentAttachments(string encodedEventId)
        {
            if (string.IsNullOrWhiteSpace(encodedEventId))
            {
                return Ok(Array.Empty<InspectionUploadFileResult>());
            }

            var files = await _apiECRS.GetSourceDocumentInspectionAttachments(encodedEventId);
            return Ok(files);
        }

        [HttpGet]
        public async Task<IActionResult> GetSourceDocumentPhotos(string encodedEventId)
        {
            if (string.IsNullOrWhiteSpace(encodedEventId))
            {
                return Ok(Array.Empty<InspectionUploadFileResult>());
            }

            var files = await _apiECRS.GetSourceDocumentInspectionPhotos(encodedEventId);
            return Ok(files);
        }

        [HttpGet]
        public IActionResult PreviewSourceDocumentAttachment(string zipFileName)
        {
            return PreviewInspectionZip(zipFileName, "Attachment", onlyImages: false);
        }

        [HttpGet]
        public IActionResult PreviewSourceDocumentPhoto(string zipFileName)
        {
            return PreviewInspectionZip(zipFileName, "Photo", onlyImages: true);
        }

        [HttpPost]
        public async Task<IActionResult> SaveProfessionalLicenseInspection([FromBody] ProfessionalLicenseInspectionSaveRequest request)
        {
            if (request.EventId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "稽查事件主鍵不可為空"
                });
            }

            var professionalLicenseStatus = request.ProfessionalLicenseStatus?.Trim().ToUpperInvariant();
            if (professionalLicenseStatus != "1" && professionalLicenseStatus != "2" && professionalLicenseStatus != "3")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "請選擇聘用食品業者專門職業或技術證照人員查核結果"
                });
            }

            var trainingStatus = request.TrainingStatus?.Trim().ToUpperInvariant();
            if (professionalLicenseStatus == "1" && trainingStatus != "Y" && trainingStatus != "N")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "請選擇登錄各該人員資料及衛生講習或訓練時數查核結果"
                });
            }

            request.ProfessionalLicenseStatus = professionalLicenseStatus;
            request.TrainingStatus = professionalLicenseStatus == "1" ? trainingStatus : null;
            request.NoFactoryRegistrationNotApplicable = professionalLicenseStatus == "3" && request.NoFactoryRegistrationNotApplicable == true;
            request.CapitalUnderThirtyMillionNotApplicable = professionalLicenseStatus == "3" && request.CapitalUnderThirtyMillionNotApplicable == true;
            request.FoodWorkersUnderTwentyNotApplicable = professionalLicenseStatus == "3" && request.FoodWorkersUnderTwentyNotApplicable == true;
            request.NonRegulatedBusinessNotApplicable = professionalLicenseStatus == "3" && request.NonRegulatedBusinessNotApplicable == true;
            request.InspectionUserId = HttpContext.Session.GetString("InspectionId");

            var result = await _apiECRS.SaveProfessionalLicenseInspection(request);
            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = string.IsNullOrWhiteSpace(result.Message) ? "儲存失敗" : result.Message
                });
            }

            return Ok(new
            {
                success = true,
                message = string.IsNullOrWhiteSpace(result.Message) ? "儲存成功" : result.Message
            });
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
