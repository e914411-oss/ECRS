namespace ECRS_API.DTOs.InspectionDTO.PReview
{
    //稽查單-稽查紀錄
    public class CheckRecD
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
        public DateTime? 稽查日期
        {
            get; set;
        }
        public DateTime? 限改日期
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
