using System.Collections.Concurrent;

namespace TestControl.AppServices;

public sealed class TestMetricsService
{
    private readonly ConcurrentDictionary<string, long> _instantiationDictionary = new();

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
        _instantiationDictionary.Clear();
    }
}
