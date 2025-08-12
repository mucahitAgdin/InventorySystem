// Dosya: ViewModels/StockViewModels.cs
// Amaç: Tekil ürün modeli — IN/OUT işlemleri "1 adet" varsayımıyla yapılır.
// Not: Barcode uzunluğu DB ile aynı olmalı (200). Aksi halde FK/validation sorunları çıkar.

using System.ComponentModel.DataAnnotations;
using InventorySystem.Models;

namespace InventorySystem.ViewModels
{
    public abstract class StockVmBase
    {
        [Required]
        [StringLength(200)]                  // ⬅️ DB: NVARCHAR(200) ile HİZALANDI
        public string Barcode { get; set; } = string.Empty;

        [StringLength(200)]
        public string? DeliveredBy { get; set; }  // ürünü getiren/veren kişi (opsiyonel)

        [StringLength(500)]
        public string? Note { get; set; }         // açıklama (opsiyonel)
    }

    // Stok GİRİŞ (Entry) — Depoya al
    public class StockInVm : StockVmBase
    {
        public TransactionType Type => TransactionType.Entry;
        // Entity'de StockTransaction.DeliveredTo [Required] olduğundan
        // controller tarafında "Depo" placeholder'ı yazıyoruz.
    }

    // Stok ÇIKIŞ (Exit) — Dışarı ver
    public class StockOutVm : StockVmBase
    {
        [Required(ErrorMessage = "Teslim alan kişi girilmelidir.")]
        [StringLength(200)]
        public string DeliveredTo { get; set; } = string.Empty;

        public TransactionType Type => TransactionType.Exit;
    }
}
