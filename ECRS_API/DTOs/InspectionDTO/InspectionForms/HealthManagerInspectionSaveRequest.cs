namespace ECRS_API.DTOs.InspectionDTO.InspectionForms
{
    public class HealthManagerInspectionSaveRequest
    {
        public int EventId { get; set; }

        public string? HasHealthManager { get; set; }

        public string? ApprovalNo { get; set; }

        public bool? NoApprovalNo { get; set; }

        public bool? NoFactoryRegistrationNotApplicable { get; set; }

        public bool? SmallScaleManufacturerNotApplicable { get; set; }

        public string? InspectionDescription { get; set; }

        public string? InspectionUserId { get; set; }
    }
}
