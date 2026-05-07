namespace ECRS_API.DTOs.FormMangeDTO.FormEditer
{
    public class AddProject_FloatColumn
    {
        public required int 專案名稱代碼主鍵
        {
            get; set;
        }
        public required int Sort
        {
            get; set;
        }
        public required string columnname
        {
            get; set;
        }
        public required string type
        {
            get; set;
        }
        public required string ismusttobe
        {
            get; set;
        }
        public required string note
        {
            get; set;
        }
    }
}
