using TestControl.Infrastructure.SubjectApiPublic;

namespace Beta.Repositories;

public record UserDao
{
    public Guid UserId { get; init; }
    public string Name { get; init; }
    public string Email { get; init; }
    public DateTime CreatedAt { get; init; }

    public UserDao() { }

    public UserDao(User dto)
    {
        UserId = dto.UserId;
        Name = dto.Name;
        Email = dto.Email;
        CreatedAt = dto.CreatedAt.UtcDateTime;
    }

    public User ToDto(Organization organization = null, string role = null) => new()
    {
        UserId = UserId,
        Name = Name,
        Email = Email,
        CreatedAt = CreatedAt,
        Organization = organization,
        Role = role
    };
}

public record OrganizationDao
{
    public Guid OrganizationId { get; init; }
    public string Name { get; init; }
    public Guid? ParentOrganizationId { get; init; }
    public DateTime CreatedAt { get; init; }

    public OrganizationDao() { }

    public OrganizationDao(Organization dto)
    {
        OrganizationId = dto.OrganizationId;
        Name = dto.Name;
        ParentOrganizationId = dto.ParentOrganization?.OrganizationId;
        CreatedAt = dto.CreatedAt.UtcDateTime;
    }

    public Organization ToDto(Organization parentOrg = null) => new()
    {
        OrganizationId = OrganizationId,
        Name = Name,
        ParentOrganization = parentOrg,
    };
}

public record OrganizationUserDao
{
    public Guid OrganizationId { get; init; }
    public Guid UserId { get; init; }
    public string Role { get; init; }
    public DateTime JoinedAt { get; init; }
    public OrganizationUserDao() { }
}

public record UserTransactionDao
{
    public Guid TransactionId { get; init; }
    public Guid UserId { get; init; }
    public Guid OrganizationId { get; init; }
    public string TransactionType { get; init; }
    public string Account { get; init; }
    public decimal Amount { get; init; }
    public DateTime CreatedAt { get; init; }
    public string Status { get; init; }
    public DateTime? ProcessedAt { get; init; }

    public UserTransactionDao() { }

    public UserTransactionDao(UserTransaction dto, Guid userId)
    {
        TransactionId = dto.TransactionId;
        TransactionType = dto.TransactionType;
        UserId = userId;
        OrganizationId = dto.Organization.OrganizationId;
        Account = dto.Account;
        Amount = dto.Amount;
        CreatedAt = dto.CreatedAt.UtcDateTime;
        Status = dto.Status;
        ProcessedAt = dto.ProcessedAt?.UtcDateTime;
    }

    public UserTransaction ToDto(Organization organization, User user) => new()
    {
        TransactionId = TransactionId,
        TransactionType = TransactionType,
        Organization = organization.OrganizationId.Equals(OrganizationId)
            ? organization
            : throw new ArgumentException("Org provided does not match ord id for record."),
        Account = Account,
        Amount = Amount,
        CreatedAt = CreatedAt,
        Status = Status,
        User = user.UserId.Equals(UserId)
            ? user
            : throw new ArgumentException("User provided does not match user id for record."),
        ProcessedAt = ProcessedAt
    };
}
