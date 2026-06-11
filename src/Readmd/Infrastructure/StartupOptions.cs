namespace Readmd.Infrastructure;

public sealed record StartupOptions(string InstanceName)
{
    public static StartupOptions Current { get; } = new(
        Environment.GetEnvironmentVariable("READMD_INSTANCE_NAME") is { Length: > 0 } instanceName
            ? instanceName
            : "default");
}
