namespace ECRS_WEB.DTOs.InspectionDTO.InspectionForms
{
    public class ExpiredFoodInspectionResult
    {
        public int EventId { get; set; }

        public string? HasExpiredFood { get; set; }

        public string? InspectionDescription { get; set; }
    }
}
