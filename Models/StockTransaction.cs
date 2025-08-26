// Dosya: Models/StockTransaction.cs
// Amaç: IN/OUT hareket logu. i18n: DataAnnotations mesajlarını resource anahtarlarıyla kullan.
// Not: AddDataAnnotationsLocalization() açık olmalı. Anahtarlar Resources/Models.StockTransaction.{lang}.resx’ten gelir.

using System;
using System.ComponentModel.DataAnnotations;

namespace InventorySystem.Models
{
    public class StockTransaction
    {
        public int Id { get; set; }

        // Display(Name) -> alan etiketi; ErrorMessage -> resource key (gerçek metin .resx’te)
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
        public DateTime TransactionDate { get; set; }

        // 🔁 NEW: target location of the move (Depo/Ofis/Stok dışı)
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
