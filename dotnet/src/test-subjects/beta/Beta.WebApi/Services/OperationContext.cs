using Beta.Common;

namespace Test.Beta.Services;

public class OperationContext : IOperationContext
{
    public Guid OperationId { get; }

    public DateTimeOffset Start { get; }

    public OperationContext(IHttpContextAccessor httpContextAccessor)
    {
        var opid = httpContextAccessor.HttpContext?.Items["OperationId"]?.ToString();

        OperationId = Guid.TryParse(opid, out var opIdGuid) ? opIdGuid : Guid.NewGuid();
        Start = DateTime.Now;
    }
}
