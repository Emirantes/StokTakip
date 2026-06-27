namespace StokTakip.Security
{
    public sealed class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _environment;

        public SecurityHeadersMiddleware(
            RequestDelegate next,
            IWebHostEnvironment environment)
        {
            _next = next;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            IHeaderDictionary headers = context.Response.Headers;

            headers["X-Frame-Options"] = "DENY";
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-XSS-Protection"] = "0";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] =
                "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            headers["Cross-Origin-Resource-Policy"] = "same-origin";
            headers["X-Permitted-Cross-Domain-Policies"] = "none";

            if (!_environment.IsDevelopment())
            {
                headers["Strict-Transport-Security"] =
                    "max-age=31536000; includeSubDomains; preload";
            }

            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "base-uri 'self'; " +
                "object-src 'none'; " +
                "frame-ancestors 'none'; " +
                "form-action 'self'; " +
                "img-src 'self' data:; " +
                "font-src 'self' https://cdn.jsdelivr.net data:; " +
                "style-src 'self' https://cdn.jsdelivr.net 'unsafe-inline'; " +
                "script-src 'self' https://cdn.jsdelivr.net 'unsafe-inline'; " +
                "connect-src 'self'; " +
                "upgrade-insecure-requests";

            await _next(context);
        }
    }
}
