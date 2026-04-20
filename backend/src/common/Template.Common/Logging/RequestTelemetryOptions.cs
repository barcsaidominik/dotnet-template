namespace Template.Common.Logging;

public sealed class RequestTelemetryOptions
{
    public const string SECTION_NAME = "RequestTelemetry";

    public int SlowRequestThresholdMs
    {
        get; set;
    } = 1000;

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
