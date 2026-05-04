namespace ECRS_WEB.DTOs.InspectionDTO.PReview
{
    //待審核案件-稽查單
    public class CheckRecMDViewModel
    {
        public CheckRecM? Mast
        {
            get; set;
        }
        public List<CheckRecD>? CheckRecs
        {
            get; set;
        }

    }
}
