// ViewModels/StockViewModels.cs
// PURPOSE: Single-card stock move VM. DeliveredTo removed; Location is required (depo/ofis/stok dışı).

using System.ComponentModel.DataAnnotations;

namespace InventorySystem.ViewModels
{
    public enum MoveLocation
    {
        Depo,
        Ofis,
        [Display(Name = "Stok dışı")]
        StokDisi
    }

    public class StockMoveVm
    {
        [Required, StringLength(200)]
        public string Barcode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Konum seçimi zorunludur.")]
        public MoveLocation? Location { get; set; }

        [StringLength(200)]
        public string? DeliveredBy { get; set; }   // opsiyonel: işlemi yapan/teslim eden

        [StringLength(500)]
        public string? Note { get; set; }          // opsiyonel açıklama
    }
}
