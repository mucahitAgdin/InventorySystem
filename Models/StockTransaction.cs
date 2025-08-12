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
        [StringLength(200)]                 // ⬅️ DB ile hizalı (NVARCHAR(200))
        public string Barcode { get; set; } = string.Empty;

        [Required]
        public TransactionType Type { get; set; }    // Entry / Exit

        // Tekil modelde hep 1 yazıyoruz (controller set ediyor).
        [Required(ErrorMessage = "Miktar girilmelidir.")]
        [Range(1, int.MaxValue, ErrorMessage = "Miktar en az 1 olmalıdır.")]
        public int Quantity { get; set; }

        // DB default: GETDATE() (DbContext → .HasDefaultValueSql("GETDATE()"))
        public DateTime TransactionDate { get; set; }

        // Çıkışta zorunlu; girişte controller "Depo" placeholder yazar.
        [Required(ErrorMessage = "Teslim alan kişi girilmelidir.")]
        [StringLength(200)]
        public string? DeliveredTo { get; set; }

        [StringLength(200)]
        public string? DeliveredBy { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }
    }

    public enum TransactionType
    {
        Entry = 1,
        Exit = 2
    }
}
