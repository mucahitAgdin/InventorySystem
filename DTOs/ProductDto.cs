namespace InventorySystem.Dtos
{
    // Liste için hafif DTO
    public class ProductListItemDto
    {
        public int Id { get; set; }
        public string Barcode { get; set; } = "";
        public string Name { get; set; } = "";
        public string? ProductType { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }
        public string? Location { get; set; }
        public string? CurrentHolder { get; set; }
        public bool IsInStock { get; set; }
    }

    // Detay için geniş DTO
    public class ProductDetailDto : ProductListItemDto
    {
        public string? Description { get; set; }
        public DateTime? DateTime { get; set; }
    }

    public class StockTransactionDto
    {
        public int Id { get; set; }
        public string Barcode { get; set; } = "";
        public string Type { get; set; } = ""; // "Entry"/"Exit"
        public int Quantity { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? DeliveredTo { get; set; }
        public string? DeliveredBy { get; set; }
        public string? Note { get; set; }
    }
}
