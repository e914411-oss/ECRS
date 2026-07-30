namespace ECRS_API.DTOs.InspectionDTO.InspectionForms
{
    public class SourceDocumentInspectionSaveRequest
    {
        public int EventId { get; set; }

        public string? SourceDocumentSaved { get; set; }

        public string? ReceiveDateOrBatchNoSpecified { get; set; }

        public string? MaterialOrProductInfoSpecified { get; set; }

        public string? SupplierInfoSpecified { get; set; }

        public string? InspectionDescription { get; set; }

        public string? InspectionUserId { get; set; }
    }
}
