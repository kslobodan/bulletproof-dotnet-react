using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Common.Interfaces;

public interface ITenantRepository : IRepository<Tenant>
{
    Task<Tenant?> GetByEmailAsync(string email);
}
