namespace ECRS_API.DTOs.FormMangeDTO.FormEditer
{
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
