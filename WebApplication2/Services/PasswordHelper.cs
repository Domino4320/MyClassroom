namespace WebApplication2.Services
{
    public static class PasswordHelper
    {
        public static string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

        public static bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash))
                return false;

            if (storedHash.StartsWith("$2", StringComparison.Ordinal))
                return BCrypt.Net.BCrypt.Verify(password, storedHash);

            return password == storedHash;
        }

        public static bool NeedsRehash(string storedHash) =>
            !storedHash.StartsWith("$2", StringComparison.Ordinal);
    }
}
