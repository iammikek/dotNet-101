using dotNet101.Application.Exceptions;

namespace dotNet101.Infrastructure.RateLimiting;

public sealed class RequestRateLimiter
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, Queue<DateTimeOffset>> _hits = new();

    public bool Enabled { get; set; } = true;

    public void Consume(string key, int maxRequests, TimeSpan window)
    {
        if (!Enabled)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var cutoff = now - window;

        lock (_syncRoot)
        {
            if (!_hits.TryGetValue(key, out var queue))
            {
                queue = new Queue<DateTimeOffset>();
                _hits[key] = queue;
            }

            while (queue.Count > 0 && queue.Peek() <= cutoff)
            {
                queue.Dequeue();
            }

            if (queue.Count >= maxRequests)
            {
                throw new RateLimitExceededException();
            }

            queue.Enqueue(now);
        }
    }

    public void Reset()
    {
        lock (_syncRoot)
        {
            _hits.Clear();
        }
    }
}
