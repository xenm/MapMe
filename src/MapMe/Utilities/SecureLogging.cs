using System.Text.RegularExpressions;

namespace MapMe.Utilities;

/// <summary>
/// Provides secure logging utilities to prevent log forging attacks by sanitizing user-provided input.
/// Implements OWASP recommendations for secure logging practices.
/// </summary>
public static class SecureLogging
{
    private static readonly Regex ControlCharacterRegex = new(@"[\x00-\x1F\x7F-\x9F]", RegexOptions.Compiled);
    private static readonly Regex HtmlTagRegex = new(@"<[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Sanitizes a string value for safe logging by removing control characters, HTML tags, 
    /// and limiting length to prevent log forging attacks.
    /// </summary>
    /// <param name="value">The value to sanitize</param>
    /// <param name="maxLength">Maximum allowed length (default: 200)</param>
    /// <param name="placeholder">Placeholder for null/empty values (default: "[empty]")</param>
    /// <returns>Sanitized string safe for logging</returns>
    public static string SanitizeForLog(string? value, int maxLength = 200, string placeholder = "[empty]")
    {
        if (string.IsNullOrEmpty(value))
            return placeholder;

        // Remove all control characters (including newlines, carriage returns, tabs, etc.)
        var sanitized = ControlCharacterRegex.Replace(value, "");

        // Remove HTML tags to prevent HTML injection in web-based log viewers
        sanitized = HtmlTagRegex.Replace(sanitized, "");

        // Normalize whitespace - replace multiple spaces with single space
        sanitized = Regex.Replace(sanitized, @"\s+", " ");

        // Trim whitespace
        sanitized = sanitized.Trim();

        // Truncate if too long and add indicator
        if (sanitized.Length > maxLength)
        {
            sanitized = sanitized.Substring(0, maxLength - 3) + "...";
        }

        // Return placeholder if sanitization resulted in empty string
        return string.IsNullOrEmpty(sanitized) ? placeholder : sanitized;
    }

    /// <summary>
    /// Creates a safe preview of a JWT token for logging purposes.
    /// Shows only first few characters to aid in debugging while protecting the token.
    /// </summary>
    /// <param name="token">The JWT token to preview</param>
    /// <param name="previewLength">Number of characters to show (default: 20)</param>
    /// <returns>Safe token preview</returns>
    public static string ToTokenPreview(string? token, int previewLength = 20)
    {
        if (string.IsNullOrEmpty(token))
            return "[empty-token]";

        var sanitized = SanitizeForLog(token, maxLength: previewLength + 10);

        if (sanitized.Length <= 10)
            return "[short-token]";

        return sanitized.Length > previewLength
            ? sanitized.Substring(0, previewLength) + "..."
            : sanitized + "...";
    }

    /// <summary>
    /// Sanitizes an email address for logging, preserving format while removing potential injection.
    /// </summary>
    /// <param name="email">Email address to sanitize</param>
    /// <returns>Sanitized email safe for logging</returns>
    public static string SanitizeEmailForLog(string? email)
    {
        if (string.IsNullOrEmpty(email))
            return "[no-email]";

        var sanitized = SanitizeForLog(email, maxLength: 100);

        // Additional validation - ensure it looks like an email
        if (!sanitized.Contains('@') || sanitized.Length < 3)
            return "[invalid-email]";

        return sanitized;
    }

    /// <summary>
    /// Sanitizes HTTP header values for secure logging.
    /// </summary>
    /// <param name="headerValue">Header value to sanitize</param>
    /// <param name="headerName">Name of the header (for context)</param>
    /// <returns>Sanitized header value</returns>
    public static string SanitizeHeaderForLog(string? headerValue, string headerName = "header")
    {
        if (string.IsNullOrEmpty(headerValue))
            return $"[no-{headerName.ToLowerInvariant()}]";

        // For Authorization headers, show only the scheme
        if (headerName.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
        {
            var parts = headerValue.Split(' ', 2);
            if (parts.Length >= 1)
            {
                var scheme = SanitizeForLog(parts[0], maxLength: 20);
                return parts.Length == 2 ? $"{scheme} [token-hidden]" : scheme;
            }
        }

        return SanitizeForLog(headerValue, maxLength: 50);
    }

    /// <summary>
    /// Sanitizes user ID values for logging, ensuring they don't contain injection attempts.
    /// </summary>
    /// <param name="userId">User ID to sanitize</param>
    /// <returns>Sanitized user ID</returns>
    public static string SanitizeUserIdForLog(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
            return "[no-user-id]";

        var sanitized = SanitizeForLog(userId, maxLength: 50);

        // User IDs should typically be alphanumeric with limited special chars
        if (Regex.IsMatch(sanitized, @"^[a-zA-Z0-9._@-]+$"))
            return sanitized;

        // If it contains unexpected characters, be more restrictive
        return Regex.Replace(sanitized, @"[^a-zA-Z0-9._@-]", "_");
    }

    /// <summary>
    /// Sanitizes request path for logging.
    /// </summary>
    /// <param name="path">Request path to sanitize</param>
    /// <returns>Sanitized path</returns>
    public static string SanitizePathForLog(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return "[no-path]";

        var sanitized = SanitizeForLog(path, maxLength: 200);

        // Ensure path starts with / if it's supposed to be a URL path
        if (!sanitized.StartsWith('/') && !sanitized.StartsWith('['))
            sanitized = "/" + sanitized;

        return sanitized;
    }

    /// <summary>
    /// Creates a sanitized context object for structured logging with common HTTP request properties.
    /// </summary>
    /// <param name="httpContext">HTTP context</param>
    /// <returns>Sanitized logging context</returns>
    public static object CreateSafeHttpContext(HttpContext? httpContext)
    {
        if (httpContext?.Request == null)
        {
            return new
            {
                Method = "[unknown]",
                Path = "[unknown]",
                ClientIP = "[unknown]",
                UserAgent = "[unknown]"
            };
        }

        var request = httpContext.Request;

        return new
        {
            Method = SanitizeForLog(request.Method, maxLength: 10),
            Path = SanitizePathForLog(request.Path.Value),
            ClientIP = SanitizeForLog(httpContext.Connection.RemoteIpAddress?.ToString(), maxLength: 45,
                placeholder: "[unknown-ip]"),
            UserAgent = SanitizeForLog(request.Headers["User-Agent"].ToString(), maxLength: 100,
                placeholder: "[no-user-agent]")
        };
    }
}