namespace Template.Infrastructure.BackgroundJobs;

public sealed record DemoLongRunningJobRequest(Guid? RequestedByUserId, DateTime RequestedAtUtc);
