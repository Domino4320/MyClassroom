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
        }
    }
}
