namespace ECRS_API.DTOs.InspectionDTO.PReview
{
    //稽查單表頭
    public class CheckRecM
    {
        public int? 稽查單號
        {
            get; set;
        }
        public string? 稽查人員
        {
            get; set;
        }
        public string? 機構代碼
        {
            get; set;
        }
        public DateTime? 稽查日期
        {
            get; set;
        }
        public string? 業者名稱
        {
            get; set;
        }
        public string? 業者電子信箱
        {
            get; set;
        }
        public string? 負責人
        {
            get; set;
        }
        public string? 業者電話
        {
            get; set;
        }
        public string? 業者地址_縣市主鍵
        {
            get; set;
        }
        public string? 業者地址_鄉鎮區主鍵
        {
            get; set;
        }
        public string? 業者地址
        {
            get; set;
        }
    }
}
