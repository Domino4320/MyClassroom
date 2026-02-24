using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using WebApplication2.Models;

public class EnrollmentModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserLogin { get; set; } // Логин из сессии

    public int CourseId { get; set; }
    [ForeignKey("CourseId")]
    public CourseModel Course { get; set; }

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    // Статус: завершил ли пользователь курс целиком (опционально)
    public bool IsFinished { get; set; } = false;
}