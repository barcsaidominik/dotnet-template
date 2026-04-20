using Polly;

using Serilog;

namespace Template.Common.Retry;

public static class RetryHelpers
{
    public const int RETRY_COUNT = 3;
    public const int RETRY_DELAY_IN_SECONDS = 2;
    public const bool IS_EXPONENTIAL_WAIT = false;

    /// <summary>
    /// Retries the asynchronous <paramref name="action"/> which accepts a <see cref="CancellationToken"/>,
    /// and optionally retries when <paramref name="shouldRetry"/> returns true for the action result.<br />
    /// Use this overload when you need:<br />
    /// - cancellable retry delays and cancellable inner actions (pass the <paramref name="cancellationToken"/>),<br />
    /// - and optional result-based retry logic via <paramref name="shouldRetry"/>.<br />
    /// <br />
    /// Notes:<br />
    /// - Exceptions matching <paramref name="shouldNotRetry"/> are NOT retried and are rethrown immediately.<br />
    /// - If <paramref name="shouldNotRetry"/> is null, only <see cref="OperationCanceledException"/> is excluded from retry.<br />
    /// - Cancellation-related exceptions (<see cref="OperationCanceledException"/>) are NOT retried and are propagated as-is.<br />
    /// - The <paramref name="isExponentialWait"/> parameter controls the backoff strategy:<br />
    ///     false (default): constant delay (baseDelay),<br />
    ///     true: exponential backoff (delay = baseDelay * 2^(attempt - 1)).<br />
    /// - The <paramref name="shouldRetry"/> predicate is evaluated on the returned result; if it returns true,
    ///   a retry will be attempted.
    /// </summary>
    /// <typeparam name="TResult">Return type of the action.</typeparam>
    /// <param name="action">Async action that accepts a cancellation token.</param>
    /// <param name="shouldRetry">Optional predicate indicating whether to retry based on the returned result.</param>
    /// <param name="shouldNotRetry">Optional predicate indicating whether an exception should NOT be retried. If null, only OperationCanceledException is excluded.</param>
    /// <param name="retryCount">Number of retry attempts (in addition to the initial try).</param>
    /// <param name="retryDelayInSeconds">Base delay in seconds between retries.</param>
    /// <param name="isExponentialWait">If true, uses exponential backoff (delay = baseDelay * 2^(attempt - 1)).</param>
    /// <param name="timeout">Optional timeout for each action invocation.</param>
    /// <param name="cancellationToken">Cancellation token to stop retries and the wrapped action.</param>
    /// <returns>Result of <paramref name="action"/> or throws the final exception.</returns>
    public static Task<TResult> RetryAsync<TResult>(
        Func<CancellationToken, Task<TResult>> action,
        Func<TResult, bool>? shouldRetry = null,
        Func<Exception, bool>? shouldNotRetry = null,
        int retryCount = RETRY_COUNT,
        int retryDelayInSeconds = RETRY_DELAY_IN_SECONDS,
        bool isExponentialWait = IS_EXPONENTIAL_WAIT,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default
    )
    {
        return RetryAsync<TResult, Exception>(
            action,
            shouldRetry,
            customExceptionAction: null,
            shouldNotRetry,
            retryCount,
            retryDelayInSeconds,
            isExponentialWait,
            timeout,
            cancellationToken
        );
    }

    /// <summary>
    /// Retries the asynchronous <paramref name="action"/> which accepts a <see cref="CancellationToken"/>,
    /// uses <paramref name="shouldRetry"/> to decide retries, and on final failure optionally converts the thrown
    /// exception using <paramref name="customExceptionAction"/> before rethrowing.<br />
    /// This is the primary cancellation-aware implementation used by other overloads.<br />
    /// <br />
    /// Behavior summary:<br />
    /// - Retries exceptions unless <paramref name="shouldNotRetry"/> returns true or it's an <see cref="OperationCanceledException"/>.<br />
    /// - If retries are exhausted and <paramref name="customExceptionAction"/> is provided, the last exception
    ///   is converted via that function and thrown; otherwise the original exception is rethrown.<br />
    /// - The provided <paramref name="cancellationToken"/> is passed to the inner action and to Polly's ExecuteAsync,
    ///   so both the inner call and the retry delays are cancellable.<br />
    /// - Logging: each retry attempt logs either the exception or the result that triggered the retry.
    /// </summary>
    /// <typeparam name="TResult">Return type of the action.</typeparam>
    /// <typeparam name="TCustomException">Type of custom exception to throw on final failure.</typeparam>
    /// <param name="action">Async action that accepts a cancellation token.</param>
    /// <param name="shouldRetry">Optional predicate indicating whether to retry based on the returned result.</param>
    /// <param name="customExceptionAction">Optional converter from the original exception to custom exception type.</param>
    /// <param name="shouldNotRetry">Optional predicate indicating whether an exception should NOT be retried. If null, only OperationCanceledException is excluded.</param>
    /// <param name="retryCount">Number of retry attempts (in addition to the initial try).</param>
    /// <param name="retryDelayInSeconds">Base delay in seconds between retries.</param>
    /// <param name="isExponentialWait">If true, uses exponential backoff (delay = baseDelay * 2^(attempt - 1)).</param>
    /// <param name="timeout">Optional timeout for each action invocation.</param>
    /// <param name="cancellationToken">Cancellation token to stop retries and the wrapped action.</param>
    /// <returns>Result of <paramref name="action"/> or throws the final (possibly converted) exception.</returns>
    public static async Task<TResult> RetryAsync<TResult, TCustomException>(
        Func<CancellationToken, Task<TResult>> action,
        Func<TResult, bool>? shouldRetry = null,
        Func<Exception, TCustomException>? customExceptionAction = null,
        Func<Exception, bool>? shouldNotRetry = null,
        int retryCount = RETRY_COUNT,
        int retryDelayInSeconds = RETRY_DELAY_IN_SECONDS,
        bool isExponentialWait = IS_EXPONENTIAL_WAIT,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default
    )
        where TCustomException : Exception
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfLessThan(retryCount, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(retryDelayInSeconds, 0);
        if (timeout.HasValue)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout.Value, TimeSpan.Zero);
        }

        var policyBuilder = Policy<TResult>.Handle<Exception>(ex =>
            ex is not OperationCanceledException &&
            (shouldNotRetry is null || !shouldNotRetry(ex)));

        if (shouldRetry is not null)
        {
            policyBuilder = policyBuilder.OrResult(shouldRetry);
        }

        var innerCorrelationId = Guid.NewGuid();
        var retryPolicy = policyBuilder
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => CalculateDelay(retryDelayInSeconds, retryAttempt, isExponentialWait),
                (outcome, timeSpan, retryAttempt, _) =>
                {
                    if (outcome.Exception is not null)
                    {
                        Log.Error(
                            "Retry #{RetryAttempt}. Waiting {RetryDelay}s before next attempt. Inner correlation ID: {InnerCorrelationId}. Exception: {Exception}",
                            retryAttempt,
                            timeSpan.TotalSeconds,
                            innerCorrelationId,
                            outcome.Exception
                        );
                    }
                    else
                    {
                        Log.Error(
                            "Retry #{RetryAttempt}. Waiting {RetryDelay}s before next attempt. Inner correlation ID: {InnerCorrelationId}. Result triggered retry: {@Result}",
                            retryAttempt,
                            timeSpan.TotalSeconds,
                            innerCorrelationId,
                            outcome.Result
                        );
                    }
                }
            );

        try
        {
            var result = await retryPolicy
                .ExecuteAsync(
                    ct =>
                    {
                        if (timeout.HasValue)
                        {
                            return action(ct).WaitAsync(timeout.Value, ct);
                        }

                        return action(ct);
                    },
                    cancellationToken
                )
                .ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            if (customExceptionAction is null)
            {
                Log.Error("Retry failed. Inner correlation ID: {InnerCorrelationId}. Exception: {Exception}", innerCorrelationId, ex);
                throw;
            }

            Log.Error("Retry failed. Inner correlation ID: {InnerCorrelationId}. Custom exception provided. Original exception: {Exception}", innerCorrelationId, ex);
            var customException = customExceptionAction(ex);
            throw customException;
        }
    }

    private static TimeSpan CalculateDelay(int baseDelayInSeconds, int retryAttempt, bool isExponentialWait)
    {
        if (!isExponentialWait)
        {
            return TimeSpan.FromSeconds(baseDelayInSeconds);
        }

        var multiplier = Math.Pow(2, retryAttempt - 1);
        var delayInSeconds = baseDelayInSeconds * multiplier;
        return TimeSpan.FromSeconds(delayInSeconds);
    }
}
