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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TeacherProfile>()
                .HasKey(tp => tp.UserLogin); // Логин — первичный ключ

            modelBuilder.Entity<TeacherProfile>()
                .HasOne<UserModel>()
                .WithOne()
                .HasForeignKey<TeacherProfile>(tp => tp.UserLogin)
                .HasPrincipalKey<UserModel>(u => u.Login);
        }
    }
}
