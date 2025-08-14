using InventorySystem.Dtos;
using InventorySystem.Models;

namespace InventorySystem.Mapping
{
    public static class DtoMappings
    {
        public static ProductListItemDto ToListItemDto(this Product p) => new()
        {
            Id = p.Id,
            Barcode = p.Barcode,
            Name = p.Name,
            ProductType = p.ProductType,
            Brand = p.Brand,
            Model = p.Model,
            SerialNumber = p.SerialNumber,
            Location = p.Location,
            CurrentHolder = p.CurrentHolder,
            IsInStock = p.IsInStock
        };

        public static ProductDetailDto ToDetailDto(this Product p)
        {
            var dto = (ProductDetailDto)p.ToListItemDto();
            dto.Description = p.Description;
            dto.DateTime = p.DateTime;
            return dto;
        }

        public static StockTransactionDto ToDto(this StockTransaction t) => new()
        {
            Id = t.Id,
            Barcode = t.Barcode,
            Type = t.Type.ToString(),
            Quantity = t.Quantity,
            TransactionDate = t.TransactionDate,
            DeliveredTo = t.DeliveredTo,
            DeliveredBy = t.DeliveredBy,
            Note = t.Note
        };
    }
}
