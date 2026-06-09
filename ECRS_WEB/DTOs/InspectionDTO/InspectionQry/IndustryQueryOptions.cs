namespace ECRS_WEB.DTOs.InspectionDTO.InspectionQry
{
    public class IndustryQueryOptions
    {
        //主鍵
        public int PrimaryKey
        {
            get; set;
        }

        //市招名稱
        public string RecruitmentName
        {
            get; set;
        } = string.Empty;

        //統一編號
        public string UniformNumber
        {
            get; set;
        } = string.Empty;
    }
}
