using TestControl.Infrastructure.SubjectApiPublic;

namespace TestControl.Infrastructure;

public static class TestDataCreationService
{
    private readonly static Random _rnd = new();
    private const int DefaultUniqueStringSize = 5;

    private static readonly ThreadLocal<Random> _threadLocalRandom = new(() => new Random());
    private static int _counter = 0;

    public static string GetUniqueString(int size = DefaultUniqueStringSize)
    {
        size = Math.Max(size, 1);
        var b = new byte[size];
        _threadLocalRandom.Value.NextBytes(b);
        var baseString = new string([.. Convert.ToBase64String(b).ToCharArray().Where(c =>
            char.IsAsciiLetterOrDigit(c))]);
        var uniqueString = $"{baseString}_{Interlocked.Increment(ref _counter)}";

        return string.IsNullOrWhiteSpace(baseString) ? GetUniqueString(size + 1) : uniqueString;
    }

    public static Organization CreateOrg(Organization parentOrg = null)
    {
        return new Organization()
        {
            ParentOrganization = parentOrg,
            Name = $"org_{GetUniqueString()}"
        };
    }

    public static User CreateUser(Organization org = null, string role = Infrastructure.Constants.WorkerTypes.Worker)
    {
        var name = $"{GetUniqueString()} {GetUniqueString()}".Trim();
        var email = $"{name.Replace(" ", ".")}@test.org";

        return new User()
        {
            Email = email,
            Name = name,
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


