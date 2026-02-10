using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class TeacherProfile
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string UserLogin { get; set; }

    [Required]
    public string SpecializationCategory { get; set; }

    [Required]
    public int Experience { get; set; }

    // Новое поле: Текущая должность
    [Required]
    public string CurrentJob { get; set; }

    // Новое поле: Теги (храним как строку через запятую)
    public string? TeacherTags { get; set; }

    public string? PortfolioUrl { get; set; }

    [Required]
    public string About { get; set; }

    public string? ExtraInfo { get; set; }
}