using System.Diagnostics;
using System.Text;

namespace TestControl.Infrastructure.SubjectApiPublic;

/// <summary>
/// Represents a snapshot status of a test cycle.
/// </summary>
public record TestStatus
{
    public string Status { get; set; } = "Unknown";
    public DateTimeOffset TimeStamp { get; init; } = DateTime.Now;
    public TestStatusCounts DbCounts { get; set; } = new();
    public double MovingAvgResponseTime { get; set; }
    public double ResponseTimeThreshold { get; set; }
    public MemoryUsage MemoryUsage { get; } = new();
    public long TotalMilliseconds { get; set; }
    public int NumberCalls { get; set; }
    public double CallsPerSecond => NumberCalls == 0 || TotalMilliseconds == 0 ? 0 : NumberCalls / (TotalMilliseconds / 1_000D);
    public double AverageResponseTime => NumberCalls == 0 ? 0D : TotalMilliseconds / NumberCalls;
    public long TotalServiceInstantiations => ServiceInstantiations.Values.Sum();
    public IDictionary<string, long> ServiceInstantiations { get; init; } = new Dictionary<string, long>();
    public string HealthStatus
    {
        get => ResponseTimeThreshold switch
        {
            <= 0D => "BLACK",
            _ => MovingAvgResponseTime switch
            {
                var rt when rt >= 0.75 * ResponseTimeThreshold => "DANGER",
                var rt when rt >= 0.50 * ResponseTimeThreshold => "WARNING",
                _ => "HEALTHY"
            }
        };
    }

    public override string ToString()
    {
        const string Boundary = "###############";
        const string Divider = "---------------";

        StringBuilder sb = new();

        sb.AppendLine($"{Environment.NewLine}{Boundary} STATUS UPDATE\t[{TimeStamp.ToLocalTime():HH:mm:ss}]");
        sb.AppendLine($"Status        : {Status}");
        sb.AppendLine($"Health Status : {HealthStatus}");
        sb.AppendLine(Divider);

        sb.AppendLine("DB Counts:");
        sb.AppendLine(DbCounts.ToString());
        sb.AppendLine(Divider);

        sb.AppendLine($"Instantiations By Type:");
        foreach (var kvp in ServiceInstantiations)
        {
            sb.AppendLine($" - {kvp.Key} : {kvp.Value}");
        }
        sb.AppendLine($"TOTAL Instantiations: {TotalServiceInstantiations}");
        sb.AppendLine(Divider);

        sb.AppendLine($"SMA Response Time (ms) : {MovingAvgResponseTime:#,##0.00}"); // SMA = simple moving average.
        sb.AppendLine($"Number of API calls    : {NumberCalls}");
        sb.AppendLine($"Calls per second       : {CallsPerSecond:F2}");
        sb.AppendLine(Divider);

        sb.AppendLine("Memory Usage:");
        sb.AppendLine(MemoryUsage.ToString());
        
        sb.AppendLine(Boundary);

        return sb.ToString();
    }
}

public record MemoryUsage
{
    public long TotalAllocatedBytes { get; } = GC.GetTotalAllocatedBytes();
    public long TotalMemory { get; } = GC.GetTotalMemory(false);
    public TimeSpan TotalPauseDuration { get; } = GC.GetTotalPauseDuration();
    public long TotalCommittedBytes { get; } = GC.GetGCMemoryInfo().TotalCommittedBytes;
    public long WorkingSet { get; } = Process.GetCurrentProcess().WorkingSet64;
    public long SurvivedMemorySize { get; } = AppDomain.CurrentDomain.MonitoringSurvivedMemorySize;
    public long PrivateMemorySize { get; } = Process.GetCurrentProcess().PrivateMemorySize64;
    public long PagedMemorySize { get; } = Process.GetCurrentProcess().PagedMemorySize64;
    public long VirtualMemorySize { get; } = Process.GetCurrentProcess().VirtualMemorySize64;
    public long PeakWorkingSet { get; } = Process.GetCurrentProcess().PeakWorkingSet64;
    public int ThreadCount { get; } = Process.GetCurrentProcess().Threads.Count;
    public TimeSpan TotalProcessorTime { get; } = Process.GetCurrentProcess().TotalProcessorTime;
    public DateTimeOffset StartTime { get; } = Process.GetCurrentProcess().StartTime;
    public TimeSpan UserProcessorTime { get; } = Process.GetCurrentProcess().UserProcessorTime;
    public TimeSpan PrivilegedProcessorTime { get; } = Process.GetCurrentProcess().PrivilegedProcessorTime;

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.AppendLine($"Start Time: {StartTime.ToString("yyyy-MM-dd HH:mm:ss")}");
        sb.AppendLine();
        sb.AppendLine($"Total Memory: {FormatBytes(TotalMemory)}");
        sb.AppendLine($"Total Allocated Bytes: {FormatBytes(TotalAllocatedBytes)}");
        sb.AppendLine($"Total Committed Bytes: {FormatBytes(TotalCommittedBytes)}");
        sb.AppendLine();
        sb.AppendLine($"Working Set: {FormatBytes(WorkingSet)}");
        sb.AppendLine($"Peak Working Set: {FormatBytes(PeakWorkingSet)}");
        sb.AppendLine();
        sb.AppendLine($"Private Memory Size: {FormatBytes(PrivateMemorySize)}");
        sb.AppendLine($"Virtual Memory Size: {FormatBytes(VirtualMemorySize)}");
        sb.AppendLine($"Survived Memory Size: {FormatBytes(SurvivedMemorySize)}");
        sb.AppendLine($"Paged Memory Size: {FormatBytes(PagedMemorySize)}");
        sb.AppendLine();
        sb.AppendLine($"Total Pause Duration: {TotalPauseDuration.TotalMilliseconds:F2} ms");
        sb.AppendLine();
        sb.AppendLine($"Total Processor Time: {TotalProcessorTime.TotalSeconds:F2} s");
        sb.AppendLine($"User Processor Time: {UserProcessorTime.TotalSeconds:F2} s");
        sb.AppendLine($"Privileged Processor Time: {PrivilegedProcessorTime.TotalSeconds:F2} s");
        sb.AppendLine();
        sb.AppendLine($"Thread Count: {ThreadCount}");

        return sb.ToString();
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes == 0)
            return "0 B";
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double bytesDouble = bytes;
        int order = 0;
        while (bytesDouble >= 1024 && order < sizes.Length - 1)
        {
            order++;
            bytesDouble /= 1024;
        }
        return $"{bytesDouble:0.##} {sizes[order]}";
    }
}

public record TestStatusCounts
{
    public long Transactions { get; init; }
    public long Admins { get; init; }
    public long Workers { get; init; }
    public long Organizations { get; init; }
    public long ParentOrganizations { get; init; }
    public long Total => Transactions + Admins + Workers + Organizations + ParentOrganizations;
    public override string ToString()
    {
        StringBuilder sb = new();
        sb.AppendLine($"# Transactions  : {Transactions}");
        sb.AppendLine($"# Admins        : {Admins}");
        sb.AppendLine($"# Workers       : {Workers}");
        sb.AppendLine($"# Organizations : {Organizations}");
        sb.AppendLine($"# Parent Orgs   : {ParentOrganizations}");
        sb.AppendLine($"{Environment.NewLine}          TOTAL : {Total}");
        return sb.ToString();
    }
}