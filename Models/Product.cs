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

        [StringLength(100)]
        public string? Barcode { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative.")]
        public int Quantity { get; set; } = 0;

        public bool IsInStock { get; set; } = true;

        [StringLength(200)]
        public string? CurrentHolder { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        [StringLength(100)]
        public string? ProductType { get; set; }

        [StringLength(100)]
        public string? Brand { get; set; }

        [StringLength(150)]
        public string? Model { get; set; }

        public string? Description { get; set; }

        [StringLength(150)]
        public string? SerialNumber { get; set; }

        public DateTime? DateTime { get; set; }
    }
}
