// File: ViewModels/StockViewModels.cs
// Purpose: Single-card stock move VM. DeliveredTo removed; Location required.
// i18n: Validation messages mapped to Resources/ViewModels.StockMoveVm.{culture}.resx

using System.ComponentModel.DataAnnotations;

namespace InventorySystem.ViewModels
{
    public enum MoveLocation
    {
        [Display(Name = "Storage")]
        Depo,

        [Display(Name = "Office")]
        Ofis,

        [Display(Name = "OutOfStock")]
        StokDisi
    }

    public class StockMoveVm
    {
        [Display(Name = "Barcode")]
        [Required(ErrorMessage = "BarcodeRequired")]
        [StringLength(200, ErrorMessage = "BarcodeLength")]
        public string Barcode { get; set; } = string.Empty;

        [Display(Name = "Location")]
        [Required(ErrorMessage = "LocationRequired")]
        public MoveLocation Location { get; set; }

        [Display(Name = "DeliveredBy")]
        [StringLength(200, ErrorMessage = "DeliveredByLength")]
        public string? DeliveredBy { get; set; }

        [Display(Name = "Note")]
        [StringLength(500, ErrorMessage = "NoteLength")]
        public string? Note { get; set; }
    }
}
