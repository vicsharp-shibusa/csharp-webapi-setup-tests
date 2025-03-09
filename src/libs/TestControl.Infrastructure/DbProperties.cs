using System.Data;

namespace TestControl.Infrastructure;

public class DbProperties
{
    public string DbVersion { get; init; }
    public DbEngine DbEngine { get; init; }
    public IDbConnection CommandConnection { get; init; }
    public IDbConnection QueryConnection { get; init; }
}