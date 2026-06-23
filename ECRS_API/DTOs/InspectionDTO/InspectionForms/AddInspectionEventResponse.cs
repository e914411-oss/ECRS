namespace ECRS_API.DTOs.InspectionDTO.InspectionForms
{
    public class AddInspectionEventResponse
    {
        public bool Success
        {
            get; set;
        }
        public int EventId
        {
            get; set;
        }
        public string Message
        {
            get; set;
        } = default!;
        public string error
        {
            get; set;
        } = default!;
        public string innerError
        {
            get; set;
        } = default!;
    }
}
