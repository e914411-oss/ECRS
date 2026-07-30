namespace ECRS_API.DTOs.InspectionDTO.InspectionForms
{
    public class ExpiredFoodInspectionSaveRequest
    {
        public int EventId { get; set; }

        public string? HasExpiredFood { get; set; }

        public string? InspectionDescription { get; set; }

        public string? InspectionUserId { get; set; }
    }
}
