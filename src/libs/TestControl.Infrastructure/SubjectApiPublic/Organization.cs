namespace TestControl.Infrastructure.SubjectApiPublic;

public record Organization
{
    public Guid OrganizationId { get; init; } = Guid.NewGuid();
    public string Name { get; init; }
    public Organization ParentOrganization { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}