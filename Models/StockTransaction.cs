// File: Models/StockTransaction.cs
// Purpose: IN/OUT transaction log.
// i18n: Display/Validation messages are resource keys. Actual texts come from
//       Resources/Models.StockTransaction.{culture}.resx via AddDataAnnotationsLocalization().

using System;
using System.ComponentModel.DataAnnotations;

namespace InventorySystem.Models
{
    public class StockTransaction
    {
        public int Id { get; set; }

        // Display(Name) is a resource key; ErrorMessage values are also resource keys.
        [Display(Name = "Barcode")]
        [Required(ErrorMessage = "BarcodeRequired")]
        [StringLength(7, MinimumLength = 6, ErrorMessage = "BarcodeLength")]
        public string Barcode { get; set; } = string.Empty;

        [Display(Name = "Type")]
        [Required(ErrorMessage = "TypeRequired")]
        public TransactionType Type { get; set; }    // Entry / Exit

        [Display(Name = "Quantity")]
        [Required(ErrorMessage = "QuantityRequired")]
        [Range(1, int.MaxValue, ErrorMessage = "QuantityMin")]
        public int Quantity { get; set; }

        [Display(Name = "TransactionDate")]
        public DateTime TransactionDate { get; set; }  // Db default via HasDefaultValueSql("GETDATE()")

        // Target location (Depot/Office/Out-of-Stock). We keep the persisted text as TR domain value for now.
        [Display(Name = "Location")]
        [Required(ErrorMessage = "LocationRequired")]
        [StringLength(50, ErrorMessage = "LocationRequired")]
        public string Location { get; set; } = "Depo";

        [Display(Name = "DeliveredBy")]
        [StringLength(200, ErrorMessage = "DeliveredByLength")]
        public string? DeliveredBy { get; set; }

        [Display(Name = "Note")]
        [StringLength(50, ErrorMessage = "NoteLength")]
        public string? Note { get; set; }
    }

    public enum TransactionType
    {
        Entry = 1,
        Exit = 2
    }
}
