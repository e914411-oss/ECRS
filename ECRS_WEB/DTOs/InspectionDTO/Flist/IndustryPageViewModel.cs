using ECRS_WEB.Models.ECRS;
using static CoreWebApp.Controllers.InspectionController;

namespace ECRS_WEB.DTOs.InspectionDTO.Flist
{
    public class IndustryPageViewModel
    {
        //公司基本資料
        public 業者資料表? Company
        {
            get; set;
        }

        //稽查紀錄
        public List<稽查事件_主表>? CheckRecs
        {
            get; set;
        }
    }
}
