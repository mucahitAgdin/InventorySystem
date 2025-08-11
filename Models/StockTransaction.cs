// Dosya: Models/StockTransaction.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace InventorySystem.Models
{
    /// <summary>Giriş/çıkış hareketleri logu.</summary>
    public class StockTransaction
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Barkod zorunludur.")]
        [StringLength(100)]
        public string Barcode { get; set; } = string.Empty;

        /// <summary>Giriş (Entry) veya Çıkış (Exit).</summary>
        [Required]
        public TransactionType Type { get; set; }

        [Required(ErrorMessage = "Miktar girilmelidir.")]
        [Range(1, int.MaxValue, ErrorMessage = "Miktar en az 1 olmalıdır.")]
        public int Quantity { get; set; }

        /// <summary>İşlem zamanı. DB'de GETDATE() default da veriyoruz.</summary>
        public DateTime TransactionDate { get; set; }

        /// <summary>Çıkışta: kime teslim edildi?</summary>
        [Required(ErrorMessage = "Teslim alan kişi girilmelidir.")]
        [StringLength(200)]
        public string? DeliveredTo { get; set; }

        /// <summary>Ürünü teslim eden kişi.</summary>
        [StringLength(200)]
        public string? DeliveredBy { get; set; }

        /// <summary>Not/açıklama.</summary>
        [StringLength(500)]
        public string? Note { get; set; }
    }

    /// <summary>Entry = 1, Exit = 2</summary>
    public enum TransactionType
    {
        Entry = 1,
        Exit = 2
    }
}
