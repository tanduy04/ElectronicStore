namespace ElectronicStore.Api.Dto
{
    public class ImportDto
    {
        public int SupplierID { get; set; }
        public List<ImportDetailDto> ImportDetails { get; set; }
        public string Note { get; set; }
    }
    public class ImportDetailDto
    {
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        
    }
}
