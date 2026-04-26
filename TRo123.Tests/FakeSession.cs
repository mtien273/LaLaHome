using Microsoft.AspNetCore.Http;

public class FakeSession : ISession
{
    private Dictionary<string, byte[]> _session = new();

    public IEnumerable<string> Keys => _session.Keys;

    public string Id => Guid.NewGuid().ToString();

    public bool IsAvailable => true;

    public void Clear() => _session.Clear();

    public Task CommitAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task LoadAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public void Remove(string key) => _session.Remove(key);

    public void Set(string key, byte[] value) => _session[key] = value;

    public bool TryGetValue(string key, out byte[] value)
        => _session.TryGetValue(key, out value);
}