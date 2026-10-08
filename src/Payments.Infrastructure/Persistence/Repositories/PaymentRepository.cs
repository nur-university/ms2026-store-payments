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

    public async Task<(IReadOnlyList<Payment> Payments, int Total)> GetPendingAsync(int skip, int take)
    {
        IQueryable<Payment> pending = _context.Payments.AsNoTracking().Where(p => p.Status == PaymentStatus.Pending);

        int total = await pending.CountAsync();
        List<Payment> page = await pending.OrderBy(p => p.CreatedOn).Skip(skip).Take(take).ToListAsync();

        return (page, total);
    }

    public Task<Payment?> GetBySourceAsync(Guid sourceId, string sourceType)
    {
        return _context.Payments.FirstOrDefaultAsync(p => p.SourceId == sourceId && p.SourceType == sourceType);
    }
}
