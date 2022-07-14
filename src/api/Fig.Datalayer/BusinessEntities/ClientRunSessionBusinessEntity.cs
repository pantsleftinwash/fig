namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class ClientRunSessionBusinessEntity
{
    public virtual Guid Id { get; init; }

    public virtual Guid RunSessionId { get; init; }

    public virtual DateTime LastSeen { get; set; } = DateTime.UtcNow;

    public virtual bool? LiveReload { get; set; }

    public virtual double PollIntervalMs { get; set; } = 30000;

    public virtual double UptimeSeconds { get; set; }

    public virtual string? IpAddress { get; set; }

    public virtual string? Hostname { get; set; }

    public virtual string FigVersion { get; set; } = default!;

    public virtual string ApplicationVersion { get; set; } = default!;

    public virtual bool OfflineSettingsEnabled { get; set; }

    public virtual bool SupportsRestart { get; set; }

    public virtual bool RestartRequested { get; set; }

    public virtual string RunningUser { get; set; } = default!;

    public virtual long MemoryUsageBytes { get; set; }
}