// Models/Product.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace InventorySystem.Models
{
    /// <summary>
    /// Her fiziksel ürün = 1 satır.
    /// Barcode zorunlu ve benzersiz; SerialNumber varsa benzersiz.
    /// Quantity alanını "tekil" modelde kullanmıyoruz (daima 1 gibi düşün).
    /// </summary>
    public class Product
    {
        public int Id { get; set; }

        [StringLength(200)]
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = string.Empty;

        // 🔒 Her ürün için tekil barkod
        [StringLength(200)]
        [Required(ErrorMessage = "Barcode is required.")]
        public string Barcode { get; set; } = string.Empty;

        // ❗ Tekil modelde adet kullanılmıyor; varsa da UI'da göstermeyeceğiz.
        // public int Quantity { get; set; } = 1;

        public bool IsInStock { get; set; } = true; // Depo/dışarı durumunu UI'da göstermek için

        [StringLength(200)]
        public string? CurrentHolder { get; set; }

        [StringLength(200)]
        public string? Location { get; set; } = "Depo";

        [StringLength(100)]
        public string? ProductType { get; set; }

        [StringLength(100)]
        public string? Brand { get; set; }

        [StringLength(150)]
        public string? Model { get; set; }

        public string? Description { get; set; }

        // 🔒 SerialNumber da benzersiz olacak (nullable unique)
        [StringLength(150)]
        public string? SerialNumber { get; set; }

        public DateTime? DateTime { get; set; }
    }
}
