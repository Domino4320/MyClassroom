using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class CourseModel
    {

        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название курса")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Название должно быть от 3 до 100 символов")]
        public string Title { get; set; }

        [StringLength(500, ErrorMessage = "Описание не может превышать 500 символов")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Выберите категорию")]
        public string Category { get; set; }

        // Путь к файлу изображения на сервере
        public string? CoverImagePath { get; set; }

        // Логин автора курса
        [Required]
        public string? AuthorLogin { get; set; }

        // Дата создания (удобно для сортировки)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
