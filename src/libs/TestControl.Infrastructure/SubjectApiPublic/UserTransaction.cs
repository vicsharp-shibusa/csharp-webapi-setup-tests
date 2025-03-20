namespace TestControl.Infrastructure.SubjectApiPublic;

/// <summary>
/// Represents a fictional unit of user work.
/// </summary>
public record UserTransaction
{
    public Guid TransactionId { get; init; } = Guid.NewGuid();
    public User User { get; init; }

    public Organization Organization { get; init; }
    public string TransactionType { get; init; } = "Testing";
    public string Account { get; init; }
    public decimal Amount { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
    public string Status { get; set; }
}