using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace InventorySystem.Models
{

    /// <summary>
    /// Ürünlere ait tüm giriş ve çıkış hareketlerini kayıt altına alır.
    /// Bu log'lar geçmişe dönük izleme sağlar.
    /// </summary>
    public class StockTransaction
    {
        public int Id {  get; set; }

        [Required(ErrorMessage = "Barkod zorunludur.")]
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// giriş veya çıkış işlemi
        /// </summary>
        [Required]
        public TransactionType Type { get; set; }

        [Required(ErrorMessage ="Miktar girilmelidir.")]
        [Range(1, int.MaxValue, ErrorMessage ="Miktar en az 1 olmalıdır.")]
        public int Quantity { get; set; }

        /// <summary>
        /// işlem ne zaman yapıldı
        /// </summary>
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        /// <summary>
        /// eğer ürün çıkışı yapıldıysa > kime teslim edildi?
        /// </summary>
        public string? DeliveredTo { get; set; }

        /// <summary>
        /// Ürünü teslim eden kişi 
        /// </summary>
        public string? DeliveredBy { get; set; }

        /// <summary>
        /// Notlar, açıklamalar, neden verildiği gibi serbest bilgi alanı
        /// </summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// Giriş = 1, Çıkış = 2 > enum olarak kullanılır
    /// </summary>
    public enum TransactionType
    {
        Entry = 1,
        Exit = 2
    }
}

