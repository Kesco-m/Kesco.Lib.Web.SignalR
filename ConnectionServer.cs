using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kesco.Lib.BaseExtention;
using Microsoft.AspNet.SignalR.Hubs;

namespace Kesco.Lib.Web.SignalR
{
    /// <summary>
    ///     Класс, реализующий хранение активных соединений и подключенных страниц
    /// </summary>
    public class ConnectionServer
    {
        /// <summary>
        ///     Экземпляр текущего объекта ConnectionServer, используемый для реализации шаблона проектирования Singleton
        /// </summary>
        private static readonly Lazy<ConnectionServer> _instance = new Lazy<ConnectionServer>(() =>
            new ConnectionServer(KescoHub.CurrentContext.Clients));

        /// <summary>
        ///     Потокобезопасный словарь для хранения активных соединений
        /// </summary>
        private readonly ConcurrentDictionary<string, ConnectionHelper> _connections =
            new ConcurrentDictionary<string, ConnectionHelper>();

        /// <summary>
        ///     Потокобезопасный словарь для хранения объектов подключенных страниц
        /// </summary>
        private readonly ConcurrentDictionary<string, object> _pages = new ConcurrentDictionary<string, object>();

        /// <summary>
        ///     Конструктор
        /// </summary>
        /// <param name="clients">Клиенты, установившие соединения</param>
        private ConnectionServer(IHubConnectionContext<dynamic> clients)
        {
            Clients = clients;
        }

        /// <summary>
        ///     Свойство, возвращающее экземпляр объекта ConnectionServer
        /// </summary>
        public static ConnectionServer Instance => _instance.Value;

        /// <summary>
        ///     Клиенты, установившие соединения
        /// </summary>
        private IHubConnectionContext<dynamic> Clients { get; }

        /// <summary>
        ///     Метод, возращающий все активные соединения
        /// </summary>
        /// <returns>Перечеслитель активных соединений</returns>
        public IEnumerable<ConnectionHelper> GetAllConnectionHelpers()
        {
            return _connections.Values?.OrderByDescending(x => x.StartTime);
        }

        /// <summary>
        ///     Метод, возвращающий объект соединения по идентификатору соединения
        /// </summary>
        /// <param name="connectionId">Идентификатору соединения</param>
        /// <returns>Объект соединения</returns>
        public ConnectionHelper GetConnectionHelper(string connectionId)
        {
            var connectionHelper = _connections.Where(e => e.Key == connectionId)
                .Select(e => (KeyValuePair<string, ConnectionHelper>?) e)
                .FirstOrDefault();

            var p = connectionHelper;
            return p?.Value;
        }

        /// <summary>
        ///     Метод, добавления соединения в словарь
        /// </summary>
        /// <param name="connectionId">Идентификатор соединения</param>
        /// <param name="connection">Объект соединения</param>
        public void AddConnection(string connectionId, ConnectionHelper connection)
        {
            _connections.TryAdd(connectionId, connection);
            Task.WhenAll(Trace.InsertHelperInDataBase(connection));
        }

        /// <summary>
        ///     Метод, удаления объекта соединения по интификатору соединения
        /// </summary>
        /// <param name="connectionId">Идентификатор соединения</param>
        /// <param name="stopCalled">Причина разрыва соединения</param>
        public void RemoveConnection(string connectionId, bool stopCalled)
        {
            var isRemove = _connections.TryRemove(connectionId);
            if (isRemove)
                Task.WhenAll(Trace.UpdateHelperInDataBase(connectionId, stopCalled));
        }


        /// <summary>
        ///     Метод, удаления объектов страниц V4, у которых нет активных соединений
        /// </summary>
        /// <returns>Количество удаленных страниц</returns>
        public int RemovePagesWithOutConnection()
        {
            var countForDelete = 0;

            foreach (var p in _pages)
            {
                var ch = _connections.Where(e => e.Value.PageId == p.Key)
                    .Select(e => (KeyValuePair<string, ConnectionHelper>?) e)
                    .FirstOrDefault();

                if (ch != null) continue;

                var pageCreateTime = p.Value?.GetType().GetProperty("CreateTime")?.GetValue(p.Value, null);
                if (pageCreateTime != null)
                {
                    var now = DateTime.Now;
                    if(now.Subtract((DateTime)pageCreateTime).TotalSeconds < 30) continue;
                }

                RemovePage(p.Key);
                countForDelete++;
            }

            return countForDelete;
        }

        /// <summary>
        ///     Метод, поулчающий всех объеты страниц V4
        /// </summary>
        /// <returns></returns>
        public IEnumerable<object> GetAllPages()
        {
            return _pages.Values;
        }

        /// <summary>
        ///     Метод, возвращающий объект страницы по идентификатору страницы
        /// </summary>
        /// <param name="pageId">Иденификатор страницы</param>
        /// <returns>Объект страницы</returns>
        public object GetPage(string pageId)
        {
            var page = _pages.Where(e => e.Key == pageId)
                .Select(e => (KeyValuePair<string, object>?) e)
                .FirstOrDefault();

            return page?.Value;
        }

        /// <summary>
        ///     Метод добавления объекта страницы в словарь
        /// </summary>
        /// <param name="pageId">Идентификатор страницы</param>
        /// <param name="page">Объект страницы</param>
        public void AddPage(string pageId, object page)
        {
            var p = GetPage(pageId);
            if (p == null)
                _pages.TryAdd(pageId, page);
            else
                _pages[pageId] = page;
        }

        /// <summary>
        ///     Метод удаления страницы
        /// </summary>
        /// <param name="pageId">Идентификатор страницы</param>
        public void RemovePage(string pageId)
        {
            _pages.TryRemove(pageId);
        }
    }
}