namespace ECRS_WEB.DTOs.InspectionDTO.InspectionForms
{
    public class HealthManagerInspectionResult
    {
        public int EventId { get; set; }

        public string? HasHealthManager { get; set; }

        public string? NoFactoryRegistrationNotApplicable { get; set; }

        public string? SmallScaleManufacturerNotApplicable { get; set; }

        public string? InspectionDescription { get; set; }
    }
}
