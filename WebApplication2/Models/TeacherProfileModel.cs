namespace WebApplication2.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class TeacherProfile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // Ключ не создается базой, мы передаем его сами
        public string UserLogin { get; set; }

        [Required]
        public string SpecializationCategory { get; set; }

        [Required]
        public int Experience { get; set; }

        public string? PortfolioUrl { get; set; }

        [Required]
        public string About { get; set; }

        public string? ExtraInfo { get; set; }

    }
}
