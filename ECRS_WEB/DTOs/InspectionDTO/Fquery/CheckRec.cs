using System.ComponentModel.DataAnnotations;

namespace ECRS_WEB.DTOs.InspectionDTO.Fquery
{
    //稽查紀錄總覽-稽查紀錄
    public class CheckRec
    {
        public int? 稽查單號
        {
            get; set;
        }
        public string? 稽查表單
        {
            get; set;
        }
        public string? 稽查人員
        {
            get; set;
        }
        public string? 稽查進度
        {
            get; set;
        }
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime? 稽查日期
        {
            get; set;
        }
        public string? 稽查結果
        {
            get; set;
        }
        public string? 執行狀態
        {
            get; set;
        }
    }
}
