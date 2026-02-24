using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using WebApplication2.Models;

public class CommentModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Text { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string UserLogin { get; set; }

    // К какому шагу относится комментарий
    public int StepId { get; set; }
    [ForeignKey("StepId")]
    public StepModel Step { get; set; }

    // Для реализации ответов (реплик) на комментарии
    public int? ParentCommentId { get; set; }
    [ForeignKey("ParentCommentId")]
    public CommentModel? ParentComment { get; set; }
}