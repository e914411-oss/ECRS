namespace ECRS_API.DTOs.InspectionDTO.InspectionForms
{
    public class InspectionUploadFileResult
    {
        public int PKey { get; set; }

        public string EventId { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public string Extension { get; set; } = string.Empty;

        public string ZipFileName { get; set; } = string.Empty;
    }
}
