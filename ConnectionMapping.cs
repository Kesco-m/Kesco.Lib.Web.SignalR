using System.Collections.Generic;
using System.Linq;

namespace Kesco.Lib.Web.SignalR
{
    public class ConnectionMapping<T>
    {
        private readonly Dictionary<T, PageHelper> _connections = new Dictionary<T, PageHelper>();

        public int Count
        {
            get
            {
                lock (_connections)
                {
                  return  _connections.Count;
                }
            }
        }
        
        public void Add(T key, PageHelper page)
        {
            lock (_connections)
            {
                var x = _connections.FirstOrDefault(x => x.Key = key.ToString());
                HashSet<PageHelper> connection;
                if (!_connections.TryGetValue(key, out connection))
                {
                    connection = new HashSet<PageHelper>();
                    _connections.Add(key, connection);
                }

                lock (connection)
                {
                    connection.Add(page);
                }
            }
        }

        public IEnumerable<PageHelper> GetConnection(T key)
        {
            lock (_connections)
            {
                
                HashSet<PageHelper> connection;
                if (_connections.TryGetValue(key, out connection))
                {
                    return connection;
                }
            }
            
            return Enumerable.Empty<PageHelper>();
        }


        public List<HashSet<PageHelper>> GetConnections()
        {
            lock (_connections)
            {
                return _connections.Values.ToList();
            }
        }

        public void Remove(T key, PageHelper page)
        {
            lock (_connections)
            {
                HashSet<PageHelper> connections;
                if (!_connections.TryGetValue(key, out connections))
                {
                    return;
                }

                lock (connections)
                {
                    connections.Remove(page);

                    if (connections.Count == 0)
                    {
                        _connections.Remove(key);
                    }
                }
            }
        }
    }
}