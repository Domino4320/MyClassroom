// Модель для отслеживания прогресса (какие шаги пройдены)
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using WebApplication2.Models;

public class UserProgressModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserLogin { get; set; } // Кто проходит

    [Required]
    public int StepId { get; set; } // Какой шаг пройден

    [ForeignKey("StepId")]
    public StepModel Step { get; set; }

    public bool IsCompleted { get; set; } = true;
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}