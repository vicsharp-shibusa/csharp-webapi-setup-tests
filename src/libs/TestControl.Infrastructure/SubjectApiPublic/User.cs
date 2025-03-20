namespace TestControl.Infrastructure.SubjectApiPublic;

/// <summary>
/// Represents a user of our fictional system.
/// </summary>
public record User
{
    public Guid UserId { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public required string Email { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTime.UtcNow;
    public Organization Organization { get; init; }
    public string Role { get; init; }

    public User CloneForOrgAndRole(Organization org, string role)
    {
        return new User()
        {
            UserId = UserId,
            Name = Name,
            Email = $"{TestDataCreationService.GetUniqueString(8)}@test.org",
            CreatedAt = DateTime.Now,
            Organization = org,
            Role = role
        };
    }
}