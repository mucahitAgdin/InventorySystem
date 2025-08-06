using System.ComponentModel.DataAnnotations;

namespace InventorySystem.Models
{
    /// <summary>
    /// Bu sınıf, sistemdeki mevcut ürünlerin anlık durumunu temsil eder.
    /// Örneğin: Şu an depoda mı, dışarıda mı? Kimde, ne kadar var?
    /// </summary>
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ürün adı zorunludur.")]
        public string Name { get; set; } = string.Empty;

        
        [Required(ErrorMessage = "Barkod zorunludur.")]
        public string Barcode { get; set; } = string.Empty;

        
        [Required(ErrorMessage = "Stok miktarı zorunludur.")]
        [Range(0, int.MaxValue, ErrorMessage = "Stok 0 veya daha fazla olmalıdır.")]
        
        public int Quantity { get; set; }

        /// <summary>
        /// Ürün şu anda depoda mı? true = depoda, false = dışarıda.
        /// </summary>
        public bool IsInStock { get; set; } = true;

        /// <summary>
        /// Eğer ürün dışarıdaysa: şu anda kimde?
        /// Örnek: "Ali Yılmaz"
        /// </summary>
        public string? CurrentHolder { get; set; }

        /// <summary>
        /// Ürünün fiziksel konumu. Örn: Depo, Servis, Ofis, Atölye, vs.
        /// </summary>
        public string Location { get; set; } = "Depo";


        /// <summary>
        /// Ürünün türünü türünü belirtir. Örn: Klavye, Mouse, Monitör, vs.
        /// </summary>7

        [Required(ErrorMessage ="Ürün tipi zorunludur.")]
        public string ProductType { get; set; } = string.Empty;


        [Required(ErrorMessage = "Marka zorunludur.")]
        public string Brand { get; set; } = string.Empty;


        /// <summary>
        /// Ürün seri numarası, açıklaması modeli ve eklenme tarihi
        /// </summary>
        public string? Model {  get; set; } // opsiyonel
        
        public string? Description {  get; set; } // teknik detaylar, arızalı mı değil mi?

        public string? SerialNumber { get; set; } // opsiyonel

        public DateTime? DateTime { get; set; } // opsiyonel



    }
}
