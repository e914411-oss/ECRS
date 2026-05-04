using ECRS_WEB.Models.PMDS;
using static CoreWebApp.Controllers.InspectionController;

namespace ECRS_WEB.DTOs.InspectionDTO.Fquery
{
    //稽查紀錄總覽
    public class CompanyPageViewModel
    {
        //公司基本資料
        public 業者資料表? Company
        {
            get; set;
        }

        //稽查紀錄
        public List<CheckRec>? CheckRecs
        {
            get; set;
        }

    }
}
