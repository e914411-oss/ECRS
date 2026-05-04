namespace ECRS_WEB.DTOs.InspectionDTO.Fquery
{
    //業者稽查查詢結果-業者清單
    public class SupplierPageViewModel
    {
        public List<Supplier> Suppliers
        {
            get; set;
        }

        public int CurrentPage
        {
            get; set;
        }

        public int TotalPages
        {
            get; set;
        }

        public int TotalCount
        {
            get; set;
        }
    }
}
