namespace Template.Application.Common.Dtos;

public sealed record QueuedBackgroundJobResult(Guid JobId, string Function, DateTime ScheduledAtUtc);
