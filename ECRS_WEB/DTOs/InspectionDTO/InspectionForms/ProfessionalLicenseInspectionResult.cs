namespace ECRS_WEB.DTOs.InspectionDTO.InspectionForms
{
    public class ProfessionalLicenseInspectionResult
    {
        public int EventId { get; set; }

        public string? ProfessionalLicenseStatus { get; set; }

        public string? TrainingStatus { get; set; }

        public string? NoFactoryRegistrationNotApplicable { get; set; }

        public string? CapitalUnderThirtyMillionNotApplicable { get; set; }

        public string? FoodWorkersUnderTwentyNotApplicable { get; set; }

        public string? InspectionDescription { get; set; }
    }
}
