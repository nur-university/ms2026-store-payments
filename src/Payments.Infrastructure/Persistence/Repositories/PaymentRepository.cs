using Microsoft.EntityFrameworkCore;
using Payments.Domain.Payments;
using Payments.Infrastructure.Persistence.DomainModel;

namespace Payments.Infrastructure.Persistence.Repositories;

internal class PaymentRepository(DomainDbContext context) : IPaymentRepository
{
    private readonly DomainDbContext _context = context;
    public async Task AddAsync(Payment entity)
    {
        await _context.Payments.AddAsync(entity);
    }

    public Task<Payment?> GetByIdAsync(Guid id, bool readOnly = false)
    {
        if (readOnly)
        {
            return _context.Payments.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        }
        else
        {
            return _context.Payments.FirstOrDefaultAsync(o => o.Id == id);
        }
    }
}
