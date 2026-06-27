using System.Security.Claims;

namespace StokTakip.Security
{
    public interface ITenantContext
    {
        int? CompanyId { get; }

        int? UserId { get; }

        string? Role { get; }
    }

    public sealed class TenantContext : ITenantContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? CompanyId
        {
            get
            {
                HttpContext? context = _httpContextAccessor.HttpContext;
                int? sessionCompanyId = context?.Session.GetInt32("CompanyId");

                if (sessionCompanyId.HasValue && sessionCompanyId.Value > 0)
                {
                    return sessionCompanyId.Value;
                }

                string? companyClaim = context?.User.FindFirstValue("CompanyId");

                return int.TryParse(companyClaim, out int companyId)
                    ? companyId
                    : null;
            }
        }

        public int? UserId
        {
            get
            {
                HttpContext? context = _httpContextAccessor.HttpContext;
                int? sessionUserId = context?.Session.GetInt32("UserId");

                if (sessionUserId.HasValue && sessionUserId.Value > 0)
                {
                    return sessionUserId.Value;
                }

                string? userClaim = context?.User.FindFirstValue(ClaimTypes.NameIdentifier);

                return int.TryParse(userClaim, out int userId)
                    ? userId
                    : null;
            }
        }

        public string? Role =>
            _httpContextAccessor.HttpContext?.Session.GetString("Role") ??
            _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
    }
}
