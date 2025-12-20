namespace ElectronicStore.Api.Helper;
using System.Security.Cryptography;
using System.Text;
using System.Web;

public static class VNPayHelper
{
    // Build query string
    public static string BuildQuery(Dictionary<string, string> data, bool encode = true)
    {
        var sorted = data.OrderBy(x => x.Key, StringComparer.Ordinal);
        var query = sorted
            .Where(x => !string.IsNullOrEmpty(x.Value))
            .Select(x => $"{x.Key}={(encode ? HttpUtility.UrlEncode(x.Value) : x.Value)}");
        return string.Join("&", query);
    }

    // HMAC SHA512 ký dữ liệu
    public static string HmacSHA512(string key, string data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA512(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);
        return BitConverter.ToString(hash).Replace("-", "").ToUpper();
    }
}




