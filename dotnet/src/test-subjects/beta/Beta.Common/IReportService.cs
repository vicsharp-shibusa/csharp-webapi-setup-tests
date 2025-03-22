using TestControl.Infrastructure.SubjectApiPublic;

namespace Beta.Common;

public interface IReportService
{
    Task<IEnumerable<Organization>> GetAllOrgsAsync();
    Task<IEnumerable<UserTransaction>> GetTransactionsForOrgAsync(Guid orgId, DateTime? start = null, DateTime? finish = null, string status = null);
    Task<IEnumerable<UserTransaction>> GetTransactionsForUserAsync(Guid userId, DateTime? start = null, DateTime? finish = null, string status = null);
    Task<IEnumerable<User>> GetUsersForOrgAsync(Guid orgId);
}