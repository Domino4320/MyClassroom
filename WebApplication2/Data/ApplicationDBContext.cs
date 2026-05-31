using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;
namespace WebApplication2.Data
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options) { 

        }
        public DbSet<UserModel> Users { get; set; }

        public DbSet<TeacherProfile> TeacherProfiles { get; set; }
        public DbSet<CourseModel> Courses { get; set; }

        public DbSet<ModuleModel> Modules { get; set; }
        public DbSet<LessonModel> Lessons { get; set; }
        public DbSet<StepModel> Steps { get; set; }
        public DbSet<QuizOptionModel> QuizOptions { get; set; }
        public DbSet<UserProgressModel> UserProgress { get; set; }

        public DbSet<EnrollmentModel> Enrollments { get; set; } // Новое
        public DbSet<CommentModel> Comments { get; set; }
        public DbSet<UserProgressModel> Progress { get; set; }
        public DbSet<CourseReviewModel> CourseReviews { get; set; }
        public DbSet<TeacherReviewModel> TeacherReviews { get; set; }
        public DbSet<StepSubmissionModel> StepSubmissions { get; set; }
        public DbSet<ForumDiscussionModel> ForumDiscussions { get; set; }
        public DbSet<ForumMessageModel> ForumMessages { get; set; }
        public DbSet<CourseBookmarkModel> CourseBookmarks { get; set; }
        public DbSet<NotificationModel> Notifications { get; set; }
        public DbSet<LessonMaterialModel> LessonMaterials { get; set; }
        public DbSet<LessonFeedbackModel> LessonFeedbacks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TeacherProfile>()
                .HasKey(tp => tp.UserLogin); 

            modelBuilder.Entity<TeacherProfile>()
                .HasOne<UserModel>()
                .WithOne()
                .HasForeignKey<TeacherProfile>(tp => tp.UserLogin)
                .HasPrincipalKey<UserModel>(u => u.Login);

            modelBuilder.Entity<ModuleModel>()
                .HasOne(m => m.Course)
                .WithMany(c => c.Modules)
                .HasForeignKey(m => m.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ForumMessageModel>()
                .HasOne(m => m.Discussion)
                .WithMany(d => d.Messages)
                .HasForeignKey(m => m.DiscussionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TeacherReviewModel>()
                .HasOne(r => r.Teacher)
                .WithMany()
                .HasForeignKey(r => r.TeacherLogin)
                .HasPrincipalKey(tp => tp.UserLogin)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TeacherReviewModel>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserLogin)
                .HasPrincipalKey(u => u.Login)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CourseBookmarkModel>()
                .HasIndex(b => new { b.UserLogin, b.CourseId })
                .IsUnique();

            modelBuilder.Entity<CourseBookmarkModel>()
                .HasOne(b => b.Course)
                .WithMany()
                .HasForeignKey(b => b.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CourseBookmarkModel>()
                .HasOne(b => b.Step)
                .WithMany()
                .HasForeignKey(b => b.StepId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NotificationModel>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserLogin)
                .HasPrincipalKey(u => u.Login)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LessonMaterialModel>()
                .HasOne(m => m.Lesson)
                .WithMany(l => l.Materials)
                .HasForeignKey(m => m.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LessonFeedbackModel>()
                .HasIndex(f => new { f.LessonId, f.UserLogin })
                .IsUnique();

            modelBuilder.Entity<LessonFeedbackModel>()
                .HasOne(f => f.Lesson)
                .WithMany()
                .HasForeignKey(f => f.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
