using System;

namespace BigRedCloud.Api.Samples.Components
{
    public static class Utils
    {
        private static readonly Random RandomGenerator = new Random();

        public static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var stringChars = new char[length];

            for (var i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[RandomGenerator.Next(chars.Length)];
            }

            return new String(stringChars);
        }
    }
}
