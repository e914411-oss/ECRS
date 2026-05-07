namespace ECRS_WEB.DTOs.FormManageDTO.FormEditer
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

    public class ApiAddProject_FloatColumn
    {
        public bool Success
        {
            get; set;
        }
        public int Id
        {
            get; set;
        }
        public string? Message
        {
            get; set;
        }
    }
}
