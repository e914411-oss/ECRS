using System.ComponentModel.DataAnnotations;

namespace ECRS_WEB.DTOs.InspectionDTO.Fquery
{
    //業者清單包含的欄位
    public class Supplier
    {
        public int? Id
        {
            get; set;
        }
        public string? 業者編號
        {
            get; set;
        }
        public string? 業者名稱
        {
            get; set;
        }
        public string? 食品登錄字號
        {
            get; set;
        }
        public string? 統一編號
        {
            get; set;
        }
        public string? 電話號碼
        {
            get; set;
        }
        public string? 業者地址
        {
            get; set;
        }
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime? 案件建立日期
        {
            get; set;
        }
    }
}
