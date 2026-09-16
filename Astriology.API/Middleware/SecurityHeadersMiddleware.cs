namespace Astriology.API.Middleware;

/// <summary>
/// Adds the response headers that cost nothing and close off whole classes of
/// browser-side attacks.
/// </summary>
/// <remarks>
/// Not in the original roadmap; added during Faz 2 on the security checklist. A full
/// Content-Security-Policy is not set here because this process serves JSON, not
/// pages - the SPA's own host is where CSP belongs.
/// </remarks>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var headers = context.Response.Headers;

        // Stops the browser guessing a content type other than the one declared.
        headers["X-Content-Type-Options"] = "nosniff";

        // No page of this API should ever be framed.
        headers["X-Frame-Options"] = "DENY";

        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        return _next(context);
    }
}
