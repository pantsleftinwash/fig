using System.Net;
using System.Security;

namespace Fig.Common.Cryptography;

public static class SecureStringExtensions
{
    public static string Read(this SecureString value)
    {
        return new NetworkCredential(string.Empty, value).Password;
    }
}