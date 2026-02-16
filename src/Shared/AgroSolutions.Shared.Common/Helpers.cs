using System.Security.Cryptography;
using System.Text;

namespace AgroSolutions.Shared.Common.Helpers;

/// <summary>
/// Helper para operações com DateTime
/// </summary>
public static class DateTimeHelper
{
    public static DateTime ToUtc(this DateTime dateTime)
    {
        return dateTime.Kind == DateTimeKind.Utc
            ? dateTime
            : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
    }

    public static bool IsWithinLast(this DateTime dateTime, int hours)
    {
        return dateTime >= DateTime.UtcNow.AddHours(-hours);
    }

    public static bool IsToday(this DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        return dateTime.Year == now.Year
            && dateTime.Month == now.Month
            && dateTime.Day == now.Day;
    }

    public static string ToIso8601(this DateTime dateTime)
    {
        return dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
    }
}

/// <summary>
/// Helper para operações com strings
/// </summary>
public static class StringHelper
{
    public static string TruncateWithEllipsis(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value.Substring(0, maxLength - 3) + "...";
    }

    public static string ToSafeFileName(this string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }

    public static bool IsValidEmail(this string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Helper para geração de hash
/// </summary>
public static class HashHelper
{
    public static string GenerateMd5Hash(string input)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);

        var sb = new StringBuilder();
        foreach (var b in hashBytes)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }

    public static string GenerateRandomToken(int length = 32)
    {
        var randomBytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}

/// <summary>
/// Helper para formatação de valores
/// </summary>
public static class FormatHelper
{
    public static string FormatArea(decimal area)
    {
        return $"{area:N2} ha";
    }

    public static string FormatTemperature(decimal temperature)
    {
        return $"{temperature:N1}°C";
    }

    public static string FormatMoisture(decimal moisture)
    {
        return $"{moisture:N1}%";
    }

    public static string FormatPrecipitation(decimal precipitation)
    {
        return $"{precipitation:N1} mm";
    }

    public static string FormatTimeAgo(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime.ToUtc();

        if (timeSpan.TotalMinutes < 1)
            return "agora mesmo";
        if (timeSpan.TotalMinutes < 60)
            return $"há {(int)timeSpan.TotalMinutes} minuto(s)";
        if (timeSpan.TotalHours < 24)
            return $"há {(int)timeSpan.TotalHours} hora(s)";
        if (timeSpan.TotalDays < 7)
            return $"há {(int)timeSpan.TotalDays} dia(s)";
        if (timeSpan.TotalDays < 30)
            return $"há {(int)(timeSpan.TotalDays / 7)} semana(s)";
        if (timeSpan.TotalDays < 365)
            return $"há {(int)(timeSpan.TotalDays / 30)} mês(es)";

        return $"há {(int)(timeSpan.TotalDays / 365)} ano(s)";
    }
}

/// <summary>
/// Helper para paginação
/// </summary>
public class PaginationHelper<T>
{
    public List<T> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;

    public static PaginationHelper<T> Create(IQueryable<T> source, int pageNumber, int pageSize)
    {
        var count = source.Count();
        var items = source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginationHelper<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = count
        };
    }
}