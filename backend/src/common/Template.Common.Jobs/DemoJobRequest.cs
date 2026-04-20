namespace Template.Common.Jobs;

public sealed record DemoJobRequest(Guid? RequestedByUserId, DateTime RequestedAtUtc);
