namespace ECRS_API.DTOs.FormMangeDTO.FormEditer
{
    public class AddProject_Result
    {
        public int 專案主鍵
        {
            get; set;
        }
        public int? 專案主鍵_PMDS
        {
            get; set;
        }
        public string? 專案名稱
        {
            get; set;
        }
        public string? 專案截止日期
        {
            get; set;
        }
        public string? 稽查項目
        {
            get; set;
        }
        public DateTime 修改日期
        {
            get; set;
        }
        public string? 異動人員
        {
            get; set;
        }
        public string? 狀態
        {
            get; set;
        }
    }
}
