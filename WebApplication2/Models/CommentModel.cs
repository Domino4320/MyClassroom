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

    // --- НОВОЕ СВОЙСТВО СВЯЗИ ---
    [ForeignKey("UserLogin")]
    public virtual UserModel User { get; set; }
    // ----------------------------

    public int StepId { get; set; }
    [ForeignKey("StepId")]
    public StepModel Step { get; set; }

    public int? ParentCommentId { get; set; }
    [ForeignKey("ParentCommentId")]
    public CommentModel? ParentComment { get; set; }
}