using System.ComponentModel.DataAnnotations;
namespace WebApplication2.Models
{
    public class UserModel
    {
        [Key]
        public string Login {  get; set; }
        [Required, MinLength(8)]
        public string Username { get; set; }
        [Required, MinLength(8)]
        public string Password { get; set; }

        public string? Avatar { get; set; }

    }
}
