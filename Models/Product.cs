using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace InventorySystem.Models
{
    /// <summary>
    /// IT envanterindeki bir ürünü temsil eder
    /// Barkod üzerinden erişilecek
    /// </summary>
    public class Product
    {
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Ürün Adı")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Barkod")]
        public string Barcode { get; set; } = String.Empty;

        [Required]
        [Display(Name = "Stok Miktarı")]
        public required string Quantity { get; set; }
    }
}
