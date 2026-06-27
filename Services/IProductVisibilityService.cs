using StokTakip.Models;
using StokTakip.Security;

namespace StokTakip.Services
{
    public interface IProductVisibilityService
    {
        IQueryable<Product> Apply(IQueryable<Product> query);
    }

    public sealed class ProductVisibilityService : IProductVisibilityService
    {
        private readonly ITenantContext _tenantContext;

        public ProductVisibilityService(
            ITenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public IQueryable<Product> Apply(IQueryable<Product> query)
        {
            int companyId =
                _tenantContext.CompanyId ??
                throw new InvalidOperationException(
                    "Şirket oturumu bulunamadı.");

            query = query.Where(x =>
                x.CompanyId == companyId &&
                x.IsActive);

            if (_tenantContext.Role == RoleNames.Personel)
            {
                int userId =
                    _tenantContext.UserId ??
                    throw new InvalidOperationException(
                        "Kullanıcı oturumu bulunamadı.");

                query = query.Where(x =>
                    x.CreatedByUserId == userId ||
                    x.UserId == userId);
            }

            return query;
        }
    }
}