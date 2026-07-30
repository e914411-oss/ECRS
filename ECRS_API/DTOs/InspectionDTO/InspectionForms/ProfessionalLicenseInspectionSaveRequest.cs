namespace ECRS_API.DTOs.InspectionDTO.InspectionForms
{
    public class ProfessionalLicenseInspectionSaveRequest
    {
        public int EventId { get; set; }

        public string? ProfessionalLicenseStatus { get; set; }

        public string? TrainingStatus { get; set; }

        public bool? NoFactoryRegistrationNotApplicable { get; set; }

        public bool? CapitalUnderThirtyMillionNotApplicable { get; set; }

        public bool? FoodWorkersUnderTwentyNotApplicable { get; set; }

        public bool? NonRegulatedBusinessNotApplicable { get; set; }

        public string? InspectionDescription { get; set; }

        public string? InspectionUserId { get; set; }
    }
}
