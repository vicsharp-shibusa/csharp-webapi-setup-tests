namespace TestControl.Infrastructure.SubjectApiPublic;

public record User
{
    public Guid UserId { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public required string Email { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public Organization Organization { get; init; }
    public string Role { get; init; }

    public User CloneForOrgAndRole(Organization org, string role)
    {
        return new User()
        {
            UserId = UserId,
            Name = Name,
            Email = $"{TestDataCreationService.GetUniqueString(8)}@test.org",
            CreatedAt = DateTime.UtcNow,
            Organization = org,
            Role = role
        };
    }
}