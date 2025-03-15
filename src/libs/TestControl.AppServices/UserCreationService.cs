using TestControl.Infrastructure.SubjectApiPublic;

namespace TestControl.AppServices;

public static class TestDataCreationService
{
    private readonly static Random _rnd = new();
    private const int DefaultUniqueStringSize = 5;

    public static string GetUniqueString(int size = DefaultUniqueStringSize)
    {
        size = Math.Max(size, 1);
        var b = new byte[size];
        _rnd.NextBytes(b);
        var result = new string([.. Convert.ToBase64String(b).ToCharArray().Where(c =>
            char.IsAsciiLetterOrDigit(c))]);

        return string.IsNullOrWhiteSpace(result) ? GetUniqueString(size < DefaultUniqueStringSize ? size + 1 : size) : result;
    }

    public static Organization CreateOrg(Organization parentOrg = null)
    {
        return new Organization()
        {
            ParentOrganization = parentOrg,
            Name = $"org_{GetUniqueString()}"
        };
    }

    public static User CreateUser(Organization org = null, string role = "Worker")
    {
        var snippet = GetUniqueString();

        return new User()
        {
            Email = $"{snippet}@test.org".ToLower(),
            Name = $"{GetUniqueString()} {GetUniqueString()}".Trim(),
            Organization = org,
            Role = role
        };
    }

    public static UserTransaction CreateTransaction(User user, string status = null)
    {
        return new UserTransaction()
        {
            Account = GetUniqueString(),
            Amount = Convert.ToDecimal(_rnd.Next(1, 1_000_000) / 103M),
            Organization = user.Organization,
            Status = status ?? GetUniqueString(),
            User = user ?? throw new ArgumentNullException(nameof(user))
        };
    }
}


