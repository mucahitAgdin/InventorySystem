// Dosya: Models/Product.cs
// Amaç: Her fiziksel ürün = 1 satır. Barcode zorunlu/benzersiz, SerialNumber varsa benzersiz.
// Quantity alanı tekil modelde kullanılmıyor. IsInStock DB tarafında computed olabilir.

using System;
using System.ComponentModel.DataAnnotations;

namespace InventorySystem.Models
{
    public class Product
    {
        public int Id { get; set; }

        [StringLength(200)]
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = string.Empty;

        // 🔒 Tekil ürün için benzersiz barkod (DbContext’te AlternateKey/Unique ile destekli)
        [StringLength(7, MinimumLength = 6, ErrorMessage ="Barcode must be between 6 and 7 characters.")]
        [Required(ErrorMessage = "Barcode is required.")]
        public string Barcode { get; set; } = string.Empty;

        // Tekil modelde adet yok — UI’da göstermiyoruz.
        // public int Quantity { get; set; } = 1;

        // DB’de computed (öneri: Location == 'Depo') olabilir; burada set etmiyoruz.
        public bool IsInStock { get; set; } = true;

        [StringLength(200)]
        public string? CurrentHolder { get; set; }   // dışarıdaysa kimde?

        [StringLength(200)]
        public string? Location { get; set; } = "Depo"; // 'Depo' / 'Dışarıda' vb.

        [StringLength(100)]
        public string? ProductType { get; set; }

        [StringLength(100)]
        public string? Brand { get; set; }

        [StringLength(150)]
        public string? Model { get; set; }

        public string? Description { get; set; }

        // 🔒 Nullable unique (DbContext’te HasIndex(...).IsUnique().HasFilter("[SerialNumber] IS NOT NULL"))
        [StringLength(150)]
        [Required(ErrorMessage = "SerialNumber is required.")]
        public string? SerialNumber { get; set; }

        public DateTime? DateTime { get; set; }      // eklenme/güncellenme tarihi (opsiyonel)
    }
}
