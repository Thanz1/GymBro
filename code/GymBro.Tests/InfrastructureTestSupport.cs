using GymBro.Application.Payments;
using GymBro.Core;
using GymBro.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GymBro.Tests;

internal sealed class SqliteTestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<GymBroDbContext> _options;

    private SqliteTestDatabase(
        SqliteConnection connection,
        DbContextOptions<GymBroDbContext> options)
    {
        _connection = connection;
        _options = options;
    }

    public static async Task<SqliteTestDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<GymBroDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new GymBroDbContext(options);
        await context.Database.EnsureCreatedAsync();

        return new SqliteTestDatabase(connection, options);
    }

    public GymBroDbContext CreateContext()
    {
        return new GymBroDbContext(_options);
    }

    public static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VietQr:BankId"] = "MB",
                ["VietQr:AccountNo"] = "0000123456789",
                ["VietQr:AccountName"] = "GYMBRO",
                ["VietQr:Template"] = "compact"
            })
            .Build();
    }

    public ValueTask DisposeAsync()
    {
        return _connection.DisposeAsync();
    }
}

internal sealed class TestPaymentAuditService : IPaymentAuditService
{
    private readonly List<PaymentAuditLogEntry> _entries = [];
    private int _nextId = 1;

    public IReadOnlyList<PaymentAuditLogEntry> Entries => _entries;

    public Task LogAsync(
        Payment payment,
        string actionType,
        string actorName,
        string message,
        DateTime? createdDate = null)
    {
        _entries.Add(new PaymentAuditLogEntry
        {
            PaymentAuditLogId = _nextId++,
            PaymentId = payment.Id,
            OrderId = payment.OrderId,
            ActionType = actionType,
            ActorName = actorName,
            Message = message,
            CreatedDate = createdDate ?? DateTime.Now
        });

        return Task.CompletedTask;
    }

    public Task EnsureInitialLogAsync(Payment payment)
    {
        if (_entries.Any(entry =>
            entry.PaymentId == payment.Id
            && entry.ActionType == PaymentAuditAction.StatusSnapshot))
        {
            return Task.CompletedTask;
        }

        _entries.Add(new PaymentAuditLogEntry
        {
            PaymentAuditLogId = _nextId++,
            PaymentId = payment.Id,
            OrderId = payment.OrderId,
            ActionType = PaymentAuditAction.StatusSnapshot,
            ActorName = "System",
            Message = payment.Status,
            CreatedDate = payment.PaymentDate,
            IsSynthetic = true
        });

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PaymentAuditLogEntry>> GetTimelineAsync(Payment payment)
    {
        var timeline = _entries
            .Where(entry => entry.PaymentId == payment.Id)
            .OrderBy(entry => entry.CreatedDate)
            .ToList();

        return Task.FromResult<IReadOnlyList<PaymentAuditLogEntry>>(timeline);
    }
}
