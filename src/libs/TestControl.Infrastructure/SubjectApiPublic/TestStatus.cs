using System.Diagnostics;
using System.Text;

namespace TestControl.Infrastructure.SubjectApiPublic;

public record TestStatus
{
    public DateTime TimeStamp { get; init; } = DateTime.Now;
    public TestStatusCounts MemoryCounts { get; set; } = new();
    public TestStatusCounts DbCounts { get; set; } = new();
    public double MovingAvgResponseTime { get; set; }
    public double ResponseTimeThreshold { get; set; }
    public MemoryUsage MemoryUsage { get; } = new();
    public long TotalServiceInstantiations => ServiceInstantiations.Values.Sum();
    public IDictionary<string, long> ServiceInstantiations { get; init; } = new Dictionary<string, long>();
    public string Status
    {
        get
        {
            if (ResponseTimeThreshold <= 0)
            { return "BLACK"; }

            if (MovingAvgResponseTime >= (ResponseTimeThreshold * 0.75))
            { return "DANGER"; }

            if (MovingAvgResponseTime >= (ResponseTimeThreshold * 0.50))
            { return "WARNING"; }

            return "HEALTHY";
        }
    }

    public override string ToString()
    {
        const string Boundary = "###############";
        const string Divider = "---";

        StringBuilder sb = new();

        sb.AppendLine($"{Boundary} STATUS UPDATE\t[{TimeStamp.ToLocalTime():HH:mm:ss}]");
        sb.AppendLine($"Status: {Status}");
        sb.AppendLine(Divider);
        sb.AppendLine("Memory Counts:");
        sb.AppendLine(MemoryCounts.ToString());
        sb.AppendLine("DB Counts:");
        sb.AppendLine(DbCounts.ToString());
        sb.AppendLine(Divider);
        sb.AppendLine($"Avg Response Time (ms): {MovingAvgResponseTime:#,##0.00}");
        sb.AppendLine(Divider);
        sb.AppendLine("Memory Usage:");
        sb.AppendLine(MemoryUsage.ToString());
        sb.AppendLine(Divider);
        sb.AppendLine($"Total Instantiations: {TotalServiceInstantiations}");
        sb.AppendLine($"Instantiations By Type:");
        foreach (var kvp in ServiceInstantiations)
        {
            sb.AppendLine($"{kvp.Key} : {kvp.Value}");
        }
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
    public DateTimeOffset StartTime { get; } = Process.GetCurrentProcess().StartTime;
    public TimeSpan UserProcessorTime { get; } = Process.GetCurrentProcess().UserProcessorTime;
    public TimeSpan PrivilegedProcessorTime { get; } = Process.GetCurrentProcess().PrivilegedProcessorTime;

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.AppendLine($"Total Allocated Bytes: {FormatBytes(TotalAllocatedBytes)}");
        sb.AppendLine($"Total Memory: {FormatBytes(TotalMemory)}");
        sb.AppendLine($"Total Pause Duration: {TotalPauseDuration.TotalMilliseconds} ms");
        sb.AppendLine($"Total Committed Bytes: {FormatBytes(TotalCommittedBytes)}");
        sb.AppendLine($"Working Set: {FormatBytes(WorkingSet)}");
        sb.AppendLine($"Survived Memory Size: {FormatBytes(SurvivedMemorySize)}");
        sb.AppendLine($"Total Processor Time: {Process.GetCurrentProcess().TotalProcessorTime.TotalSeconds} s");
        sb.AppendLine($"Private Memory Size: {FormatBytes(PrivateMemorySize)}");
        sb.AppendLine($"Paged Memory Size: {FormatBytes(PagedMemorySize)}");
        sb.AppendLine($"Virtual Memory Size: {FormatBytes(VirtualMemorySize)}");
        sb.AppendLine($"Peak Working Set: {FormatBytes(PeakWorkingSet)}");
        sb.AppendLine($"Thread Count: {ThreadCount}");
        sb.AppendLine($"Start Time: {StartTime}");
        sb.AppendLine($"User Processor Time: {UserProcessorTime.TotalSeconds} s");
        sb.AppendLine($"Privileged Processor Time: {PrivilegedProcessorTime.TotalSeconds} s");

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

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.AppendLine($"# Transactions  : {Transactions}");
        sb.AppendLine($"# Admins        : {Admins}");
        sb.AppendLine($"# Workers       : {Workers}");
        sb.AppendLine($"# Organizations : {Organizations}");
        sb.AppendLine($"# Parent Orgs   : {ParentOrganizations}");
        return sb.ToString();
    }
}