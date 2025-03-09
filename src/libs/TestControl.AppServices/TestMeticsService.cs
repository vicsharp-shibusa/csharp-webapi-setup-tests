using System.Collections.Concurrent;

namespace TestControl.AppServices;

public class TestMetricsService
{
    private long _numAdmins;
    private long _numWorkers;
    private long _numTransactions;
    private long _numOrganizations;

    private readonly ConcurrentDictionary<string, long> _instantiationDictionary = new();

    public void IncrementAdmins() => Interlocked.Increment(ref _numAdmins);
    public void IncrementWorkers() => Interlocked.Increment(ref _numWorkers);
    public void IncrementTransactions() => Interlocked.Increment(ref _numTransactions);
    public void IncrementOrganizations() => Interlocked.Increment(ref _numOrganizations);

    public long GetAdmins() => Interlocked.Read(ref _numAdmins);
    public long GetWorkers() => Interlocked.Read(ref _numWorkers);
    public long GetTransactions() => Interlocked.Read(ref _numTransactions);
    public long GetOrganizations() => Interlocked.Read(ref _numOrganizations);

    public void IncrementClassInstantiation(string className)
    {
        _instantiationDictionary.AddOrUpdate(
            className,
            1,                                      // Initial value if not present
            (_, existingCount) => existingCount + 1 // Increment if exists
        );
    }

    public IDictionary<string, long> GetInstantiationCounts() => _instantiationDictionary;

    public void Reset()
    {
        _numAdmins = 0;
        _numOrganizations = 0;
        _numTransactions = 0;
        _numWorkers = 0;
        _instantiationDictionary.Clear();
    }
}
