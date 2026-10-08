namespace Payments.Infrastructure.Persistence;

public interface IDatabase : IDisposable
{
    void Migrate();
}
