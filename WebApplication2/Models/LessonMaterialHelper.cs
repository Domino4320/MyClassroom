namespace WebApplication2.Models
{
    public static class LessonMaterialHelper
    {
        public const long MaxFileBytes = 100L * 1024 * 1024;
        public const int MaxFilesPerLesson = 5;
        public const int MaxLinksPerLesson = 10;

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pptx", ".docx", ".pdf", ".xlsx", ".txt"
        };

        public static bool IsAllowedExtension(string fileName)
        {
            var ext = Path.GetExtension(fileName ?? "");
            return !string.IsNullOrEmpty(ext) && AllowedExtensions.Contains(ext);
        }

        public static bool IsValidHttpUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        public static string SanitizeFileName(string fileName)
        {
            var name = Path.GetFileName(fileName ?? "file");
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return string.IsNullOrWhiteSpace(name) ? "file" : name;
        }

        public static string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
            return ext switch
            {
                ".txt" => "text/plain; charset=utf-8",
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
        }
    }
}
