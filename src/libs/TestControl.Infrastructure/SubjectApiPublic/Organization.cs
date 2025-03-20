namespace TestControl.Infrastructure.SubjectApiPublic;

/// <summary>
/// Represents an organization in the (business) test domain.
/// </summary>
public record Organization
{
    public Guid OrganizationId { get; init; } = Guid.NewGuid();
    public string Name { get; init; }
    public Organization ParentOrganization { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTime.Now;
}