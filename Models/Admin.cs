using System.ComponentModel.DataAnnotations;

namespace InventorySystem.Models
{
    /// <summary>
    /// Admin-only user model.
    /// Password hashing/verification will be added in a later patch.
    /// </summary>
    public class Admin
    {
        public int Id { get; set; }

        // Leave messages to i18n (resx). We only specify the rules here.
        [Required]                  // message will come from localization resources
        [MaxLength(50)]             // message will come from localization resources
        public required string Username { get; set; }

        [Required]                  // message will come from localization resources
        [DataType(DataType.Password)]
        public required string Password { get; set; }
    }
}
