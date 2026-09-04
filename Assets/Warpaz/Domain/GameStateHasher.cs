using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Warpaz.Domain
{
    /// <summary>
    /// SHA-256 (lowercase hex) of the canonical UTF-8 <see cref="GameState"/> document.
    /// </summary>
    public static class GameStateHasher
    {
        public static string Hash(GameState state)
        {
            var bytes = CanonicalGameStateSerializer.ToUtf8Bytes(state);
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(bytes);
                var builder = new StringBuilder(hash.Length * 2);
                for (var i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                }

                return builder.ToString();
            }
        }

        public static string CanonicalDocument(GameState state)
        {
            return CanonicalGameStateSerializer.ToString(state);
        }
    }
}
