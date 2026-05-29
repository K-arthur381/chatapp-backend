using System.Collections.Concurrent;

namespace ChatApp.Api.Hubs;

public class ConnectionManager
{
    private static readonly ConcurrentDictionary<Guid, HashSet<string>> UserConnections = new();

    public void AddConnection(Guid userId, string connectionId)
    {
        UserConnections.AddOrUpdate(userId,
            _ => new HashSet<string> { connectionId },
            (_, set) => { set.Add(connectionId); return set; });
    }

    public void RemoveConnection(Guid userId, string connectionId)
    {
        if (UserConnections.TryGetValue(userId, out var set))
        {
            set.Remove(connectionId);
            if (set.Count == 0) UserConnections.TryRemove(userId, out _);
        }
    }

    public HashSet<string> GetConnections(Guid userId) =>
        UserConnections.TryGetValue(userId, out var set) ? set : new HashSet<string>();

    public IEnumerable<Guid> GetOnlineUsers() => UserConnections.Keys;
}