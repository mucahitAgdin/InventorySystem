using System.ComponentModel.DataAnnotations;

namespace InventorySystem.Models
{
    /// <summary>
    /// Sadece admin için kullanılacak kullanıcı modeli.
    /// Şifre hash'leme ve kontrol ileride eklenecek.
    /// </summary>
    public class Admin
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [MaxLength(50)]
        public required string Username { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [DataType(DataType.Password)]
        public required string Password { get; set; }
    }
}
