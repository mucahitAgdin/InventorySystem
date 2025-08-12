// Dosya: ViewModels/StockViewModels.cs  (Yeni klasör/namespace)
using System.ComponentModel.DataAnnotations;
using InventorySystem.Models;

namespace InventorySystem.ViewModels
{
    public abstract class StockVmBase
    {
        [Required]
        [StringLength(100)]
        public string Barcode { get; set; } = string.Empty;

        [StringLength(200)]
        public string? DeliveredBy { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }
    }

    public class StockInVm : StockVmBase
    {
        public TransactionType Type => TransactionType.Entry;
        // Entity'de DeliveredTo Required olduğu için IN tarafında controller "Depo" yazar (aşağıda).
    }

    public class StockOutVm : StockVmBase
    {
        [Required(ErrorMessage = "Teslim alan kişi girilmelidir.")]
        [StringLength(200)]
        public string DeliveredTo { get; set; } = string.Empty;

        public TransactionType Type => TransactionType.Exit;
    }
}
