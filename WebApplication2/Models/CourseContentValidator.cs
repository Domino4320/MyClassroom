using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;

namespace WebApplication2.Models
{
    public static class CourseContentValidator
    {
        public static bool IsValidVideoUrl(string? videoUrl)
        {
            if (string.IsNullOrWhiteSpace(videoUrl))
                return false;

            return Uri.TryCreate(videoUrl.Trim(), UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        public static async Task<string?> ValidateLessonAsync(
            LessonModel lesson,
            string? lessonTitle,
            List<StepUpdateDto> stepDtos,
            ApplicationDBContext db)
        {
            if (string.IsNullOrWhiteSpace(lessonTitle))
                return "Укажите заголовок урока.";

            var steps = lesson.Steps?.OrderBy(s => s.Order).ThenBy(s => s.Id).ToList() ?? new List<StepModel>();
            if (steps.Count == 0)
                return "Добавьте хотя бы один шаг в урок.";

            if (stepDtos == null || stepDtos.Count != steps.Count)
                return "Не все шаги урока переданы. Перезагрузите страницу и попробуйте снова.";

            foreach (var step in steps)
            {
                var dto = stepDtos.FirstOrDefault(s => s.Id == step.Id);
                if (dto == null)
                    return "Не все шаги урока переданы. Перезагрузите страницу и попробуйте снова.";

                var error = await ValidateStepAsync(step, dto, db);
                if (error != null)
                    return error;
            }

            return null;
        }

        public static async Task<string?> ValidateStepAsync(StepModel step, StepUpdateDto dto, ApplicationDBContext db)
        {
            var stepLabel = string.IsNullOrWhiteSpace(dto.Title)
                ? $"шаг #{step.Order + 1}"
                : $"«{dto.Title.Trim()}»";

            if (string.IsNullOrWhiteSpace(dto.Title))
                return "У каждого шага должен быть заголовок.";

            switch (step.Type)
            {
                case StepType.Text:
                case StepType.Code:
                    if (string.IsNullOrWhiteSpace(dto.TextContent) ||
                        string.Equals(dto.TextContent.Trim(), "Введите текст...", StringComparison.Ordinal))
                        return $"Заполните текстовый контент шага {stepLabel}.";
                    break;

                case StepType.Video:
                    if (!IsValidVideoUrl(dto.VideoUrl))
                        return $"Укажите корректную ссылку на видео для шага {stepLabel}.";
                    break;

                case StepType.Interactive:
                    if (!InteractiveStepHelper.TryValidateConfig(dto.TextContent, out var interactiveError))
                        return $"{stepLabel}: {interactiveError ?? "интерактивное задание заполнено не полностью."}";
                    break;

                case StepType.Quiz:
                    if (dto.MaxPoints < 1)
                        return $"Укажите количество баллов (минимум 1) для теста {stepLabel}.";

                    var quizError = await ValidateQuizStepAsync(step, dto, db, stepLabel);
                    if (quizError != null)
                        return quizError;
                    break;
            }

            return null;
        }

        public static async Task<string?> ValidateCourseForPublishAsync(CourseModel course, ApplicationDBContext db)
        {
            var modules = course.Modules?.OrderBy(m => m.Order).ToList() ?? new List<ModuleModel>();
            if (modules.Count == 0)
                return "Добавьте хотя бы один модуль в курс.";

            var lessons = modules.SelectMany(m => m.Lessons ?? new List<LessonModel>()).OrderBy(l => l.Order).ToList();
            if (lessons.Count == 0)
                return "В курсе должен быть хотя бы один урок.";

            foreach (var lesson in lessons)
            {
                if (string.IsNullOrWhiteSpace(lesson.Title))
                    return "У каждого урока должен быть заголовок.";

                var steps = lesson.Steps?.OrderBy(s => s.Order).ThenBy(s => s.Id).ToList() ?? new List<StepModel>();
                if (steps.Count == 0)
                    return $"Урок «{lesson.Title}» не содержит шагов.";

                foreach (var step in steps)
                {
                    var dto = new StepUpdateDto
                    {
                        Id = step.Id,
                        Title = step.Title,
                        TextContent = step.TextContent,
                        VideoUrl = step.VideoUrl,
                        IsManualCheck = step.IsManualCheck,
                        IsMultipleChoice = step.IsMultipleChoice,
                        CorrectTextAnswer = step.CorrectTextAnswer,
                        MaxPoints = step.MaxPoints
                    };

                    var error = await ValidateStepAsync(step, dto, db);
                    if (error != null)
                        return error;
                }
            }

            return null;
        }

        private static async Task<string?> ValidateQuizStepAsync(
            StepModel step,
            StepUpdateDto dto,
            ApplicationDBContext db,
            string stepLabel)
        {
            if (dto.IsManualCheck)
            {
                if (string.IsNullOrWhiteSpace(dto.CorrectTextAnswer))
                    return $"Введите эталонный ответ для теста {stepLabel}.";
                return null;
            }

            var hasTextAutoAnswer = !string.IsNullOrWhiteSpace(dto.CorrectTextAnswer);
            var options = await db.QuizOptions.Where(o => o.StepId == step.Id).ToListAsync();

            if (hasTextAutoAnswer)
            {
                if (options.Count > 0 && options.Any(o => string.IsNullOrWhiteSpace(o.Text)))
                    return $"Заполните текст всех вариантов в тесте {stepLabel}.";
                return null;
            }

            if (options.Count < 2)
                return $"Добавьте минимум 2 варианта ответа в тесте {stepLabel}.";

            if (options.Any(o => string.IsNullOrWhiteSpace(o.Text)))
                return $"Заполните текст всех вариантов в тесте {stepLabel}.";

            if (!options.Any(o => o.IsCorrect))
                return $"Отметьте хотя бы один правильный вариант в тесте {stepLabel}.";

            return null;
        }
    }
}
