namespace Allpaypayz;

using System;

public sealed record RetryOptions(
    int MaxAttempts = 3,
    TimeSpan? InitialBackoff = null,
    TimeSpan? MaxBackoff = null,
    TimeSpan? Jitter = null
)
{
    public TimeSpan EffectiveInitialBackoff => InitialBackoff ?? TimeSpan.FromMilliseconds(250);
    public TimeSpan EffectiveMaxBackoff => MaxBackoff ?? TimeSpan.FromSeconds(4);
    public TimeSpan EffectiveJitter => Jitter ?? TimeSpan.FromMilliseconds(250);

    public static RetryOptions Defaults() => new();
}
