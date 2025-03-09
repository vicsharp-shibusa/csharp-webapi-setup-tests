namespace TestControl.Infrastructure.SubjectApiPublic;

public record UserTransaction
{
    public Guid TransactionId { get; init; } = Guid.NewGuid();
    public User User { get; init; }

    public Organization Organization { get; init; }
    public string TransactionType { get; init; } = "Testing";
    public string Account { get; init; }
    public decimal Amount { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; init; }
    public string Status { get; set; }
}