namespace ECRS_API.DTOs.FormMangeDTO.FormEditer
{
    public class AddProject_Form
    {
        public required string FormName
        {
            get; set;
        }
        public required string ProjectDeadline
        {
            get; set;
        }
        public required string Status
        {
            get; set;
        }
        public required string InspectionItems
        {
            get; set;
        }
        public required string InspectionItemsValue
        {
            get; set;
        }
        public string? 建立人員主鍵
        {
            get; set;
        }
        public string? 異動人員主鍵
        {
            get; set;
        }
    }

    public class UpdateProject_Form : AddProject_Form
    {
        public required int ProjectId
        {
            get; set;
        }
    }

    public class ApiAddProjectResult
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

    public class AddProject_Inspections
    {

    }
}
