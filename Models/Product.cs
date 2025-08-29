// File: Models/Product.cs
// Purpose: Each physical product = 1 row. Barcode required/unique, SerialNumber unique if present.
// Quantity unused in single-item model. IsInStock computed by DB.
// i18n: Display + Validation messages are resource keys in Resources/Models.Product.{culture}.resx

using System;
using System.ComponentModel.DataAnnotations;

namespace InventorySystem.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Display(Name = "Name")]
        [StringLength(200, ErrorMessage = "NameLength")]
        [Required(ErrorMessage = "NameRequired")]
        public string Name { get; set; } = string.Empty;

        // 🔒 Unique barcode per product (DbContext AlternateKey/Unique supports it)
        [Display(Name = "Barcode")]
        [StringLength(7, MinimumLength = 6, ErrorMessage = "BarcodeLength")]
        [Required(ErrorMessage = "BarcodeRequired")]
        public string Barcode { get; set; } = string.Empty;

        public bool IsInStock { get; set; } = true;

        [Display(Name = "CurrentHolder")]
        [StringLength(200, ErrorMessage = "CurrentHolderLength")]
        public string? CurrentHolder { get; set; }

        [Display(Name = "Location")]
        [StringLength(200, ErrorMessage = "LocationLength")]
        public string? Location { get; set; } = "Depo";

        [Display(Name = "ProductType")]
        [StringLength(100, ErrorMessage = "ProductTypeLength")]
        public string? ProductType { get; set; }

        [Display(Name = "Brand")]
        [StringLength(100, ErrorMessage = "BrandLength")]
        public string? Brand { get; set; }

        [Display(Name = "Model")]
        [StringLength(150, ErrorMessage = "ModelLength")]
        public string? Model { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        // 🔒 Nullable unique (DbContext HasIndex + filtered unique)
        [Display(Name = "SerialNumber")]
        [StringLength(150, ErrorMessage = "SerialNumberLength")]
        [Required(ErrorMessage = "SerialNumberRequired")]
        public string? SerialNumber { get; set; }

        [Display(Name = "DateTime")]
        public DateTime? DateTime { get; set; }
    }
}
