using StokTakip.Models;

namespace StokTakip.Services
{
    public interface IAuditLogService
    {
        void Add(string message);
    }

    public sealed class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Security.ITenantContext _tenantContext;

        public AuditLogService(
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor,
            Security.ITenantContext tenantContext)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _tenantContext = tenantContext;
        }

        public void Add(string message)
        {
            HttpContext? httpContext = _httpContextAccessor.HttpContext;

            _context.Logs.Add(new Log
            {
                Islem = message,
                Tarih = DateTime.Now,
                CompanyId = _tenantContext.CompanyId ?? 0,
                UserId = _tenantContext.UserId,
                KullaniciAdi =
                    httpContext?.Session.GetString("Username") ??
                    httpContext?.User.Identity?.Name,
                Rol =
                    _tenantContext.Role ??
                    httpContext?.Session.GetString("Role"),
                IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString()
            });
        }
    }
}
