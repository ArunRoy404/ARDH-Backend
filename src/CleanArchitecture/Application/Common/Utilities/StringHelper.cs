using System.Security.Cryptography;

namespace CleanArchitecture.Application.Common.Utilities;

public static class StringHelper
{
    public static string Hash(this string inputString)
        => BCrypt.Net.BCrypt.HashPassword(inputString);

    public static bool Verify(string pass, string oldPass)
        => BCrypt.Net.BCrypt.Verify(pass, oldPass);

    public static int GenerateRandom(int min, int max) => RandomNumberGenerator.GetInt32(min, max);
}
