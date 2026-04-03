using System.Security.Cryptography;
using System.Text;

namespace CommonLibExtended.Generator;

public static class MongoIdGenerator
{
    public static string Generate()
    {
        var bytes = new byte[12];

        // 4 bytes timestamp
        var timestamp = BitConverter.GetBytes((int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(timestamp);
        }
        Array.Copy(timestamp, 0, bytes, 0, 4);

        // remaining 8 bytes random
        RandomNumberGenerator.Fill(bytes.AsSpan(4));

        var sb = new StringBuilder(24);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }
}