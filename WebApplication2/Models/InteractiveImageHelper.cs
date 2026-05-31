using Microsoft.AspNetCore.Hosting;

namespace WebApplication2.Models
{
    public static class InteractiveImageHelper
    {
        public const long MaxFileBytes = 5L * 1024 * 1024;
        public const int MaxImagesPerAssignment = 6;

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"
        };

        public static bool IsAllowedExtension(string fileName)
        {
            var ext = Path.GetExtension(fileName ?? "");
            return !string.IsNullOrEmpty(ext) && AllowedExtensions.Contains(ext);
        }

        public static bool IsStoredInteractivePath(int stepId, string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            var expectedPrefix = $"/uploads/interactive/{stepId}/";
            return path.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase);
        }

        public static string? TryGetPhysicalPath(IWebHostEnvironment env, string? webPath)
        {
            if (string.IsNullOrWhiteSpace(webPath) || !webPath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
                return null;

            var relative = webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(env.WebRootPath, relative);
        }
    }
}
