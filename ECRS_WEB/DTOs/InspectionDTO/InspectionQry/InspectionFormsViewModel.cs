using ECRS_WEB.Models.PMDS;

namespace ECRS_WEB.DTOs.InspectionDTO.InspectionQry
{
    public class InspectionFormsViewModel
    {
        public string CompanyId { get; set; } = string.Empty;
        public 業者資料表 Company { get; set; } = new();
        public string InspectionDate { get; set; } = DateTime.Now.ToString("yyyy/MM/dd");
        public List<InspectionProjectItemGroup> ProjectGroups { get; set; } = [];
    }

    public class InspectionProjectItemGroup
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public List<InspectionItemLink> Items { get; set; } = [];
    }

    public class InspectionItemLink
    {
        public int Id { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
    }
}
