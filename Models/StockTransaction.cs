// Dosya: Models/StockTransaction.cs
// Amaç: IN/OUT hareket logu. TransactionType enum, Quantity=1 (tekil hareket).
// TransactionDate DB’de GETDATE() default (DbContext’te HasDefaultValueSql).

using System;
using System.ComponentModel.DataAnnotations;

namespace InventorySystem.Models
{
    public class StockTransaction
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Barkod zorunludur.")]
        [StringLength(7, MinimumLength = 6, ErrorMessage = "Barkod 6 ile 7 karakter arasında olmalıdır.")]
        public string Barcode { get; set; } = string.Empty;

        [Required(ErrorMessage ="Type zorunludur")]
        public TransactionType Type { get; set; }    // Entry / Exit

        // Tekil modelde hep 1 yazıyoruz (controller set ediyor).
        [Required(ErrorMessage = "Miktar girilmelidir.")]
        [Range(1, int.MaxValue, ErrorMessage = "Miktar en az 1 olmalıdır.")]
        public int Quantity { get; set; }

        // DB default: GETDATE() (DbContext → .HasDefaultValueSql("GETDATE()"))
        public DateTime TransactionDate { get; set; }

        // 🔁 NEW: target location of the move (Depo/Ofis/Stok dışı)
        [Required, StringLength(50, ErrorMessage = "Lokasyon seçilmelidir.")]
        public string Location { get; set; } = "Depo";

        [StringLength(200)]
        public string? DeliveredBy { get; set; }

        [StringLength(50)]
        public string? Note { get; set; }
    }

    public enum TransactionType
    {
        Entry = 1,
        Exit = 2
    }
}
