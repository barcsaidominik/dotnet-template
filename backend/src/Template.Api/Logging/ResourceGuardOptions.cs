namespace Template.Api.Logging;

public sealed class ResourceGuardOptions
{
    public const string SECTION_NAME = "ResourceGuard";

    public bool Enabled
    {
        get; set;
    } = true;

    public int MaxWorkingSetMb
    {
        get; set;
    } = 1024;

    public int MaxConcurrentRequests
    {
        get; set;
    } = 200;

    public int MinAvailableWorkerThreads
    {
        get; set;
    } = 16;

    public int CriticalStateDurationSeconds
    {
        get; set;
    } = 5;

    public string[] ExcludedPaths
    {
        get; set;
    } =
    [
        "/health",
        "/openapi",
        "/scalar"
    ];
}
