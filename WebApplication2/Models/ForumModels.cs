using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class ForumDiscussionModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(120, MinimumLength = 3)]
        public string Title { get; set; }

        [Required]
        public string AuthorLogin { get; set; }

        [ForeignKey(nameof(AuthorLogin))]
        public UserModel? Author { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<ForumMessageModel> Messages { get; set; } = new();
    }

    public class ForumMessageModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DiscussionId { get; set; }

        [ForeignKey(nameof(DiscussionId))]
        public ForumDiscussionModel Discussion { get; set; }

        [Required]
        public string UserLogin { get; set; }

        [ForeignKey(nameof(UserLogin))]
        public UserModel? User { get; set; }

        [Required]
        [StringLength(3000, MinimumLength = 1)]
        public string Text { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? ParentMessageId { get; set; }

        [ForeignKey(nameof(ParentMessageId))]
        public ForumMessageModel? ParentMessage { get; set; }
    }
}

