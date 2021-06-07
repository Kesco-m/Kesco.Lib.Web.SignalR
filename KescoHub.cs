using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using Kesco.Lib.BaseExtention.Enums.Controls;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace Kesco.Lib.Web.SignalR
{
    /// <summary>
    ///     Класс, осуществляющий взаимодействие между подключенными клиентами и сервером SignalR
    /// </summary>
    [HubName("kescoSignalHub")]
    public class KescoHub : Hub
    {
        private const string signalView = "signalview";

        /// <summary>
        ///     Контект работы класса KescoHub
        /// </summary>
        public static IHubContext CurrentContext = GlobalHost.ConnectionManager.GetHubContext<KescoHub>();

        private readonly ConnectionServer _connectionServer;

        /// <summary>
        ///     Контруктор, обеспечивающий работу с единственным экземпляром ConnectionServer
        /// </summary>
        public KescoHub() : this(ConnectionServer.Instance)
        {
        }

        /// <summary>
        ///     Конструктор
        /// </summary>
        /// <param name="connectionServer"></param>
        public KescoHub(ConnectionServer connectionServer)
        {
            _connectionServer = connectionServer;
        }


        #region Info

        /// <summary>
        ///     Метод, обеспечивающий актуализацию информации на хендлере трассировки signalview
        /// </summary>
        /// <param name="info">Актуальная информация</param>
        public static void RefreshSignalViewInfo(KescoHubTraceInfo info)
        {
            var connectionServer = ConnectionServer.Instance;
            var allConnectionHelpers = connectionServer.GetAllConnectionHelpers();
            var connectionsView = allConnectionHelpers.Where(x => x.PageId == signalView).ToList();
            if (connectionsView.Count <= 0) return;

            var pages = connectionServer.GetAllPages();
            info.CountPages = pages.Count();
            info.TraceInfo =
                $"<div>{(string.IsNullOrEmpty(info.TraceInfo) ? "неизвестное сообщение" : info.TraceInfo)}</div>";

            var idsSv = new List<string>();
            connectionsView.ForEach(x => { idsSv.Add(x.ConnectionId); });
            CurrentContext.Clients.Clients(idsSv).refreshSignalViewInfo(info);
        }

        #endregion

        #region Messages 

        /// <summary>
        ///     Метод отправки сообщения клиенту
        /// </summary>
        /// <param name="message">Объект сообщения</param>
        /// <param name="receiveClients">Кто получит сообщение</param>
        public static void SendMessage(SignalMessage message, SignaRReceiveClientsMessageEnum receiveClients = SignaRReceiveClientsMessageEnum.ItemId_ItemName)
        {
            var connectionServer = ConnectionServer.Instance;
            if (connectionServer == null) return;

            var allConnectionHelpers = connectionServer.GetAllConnectionHelpers();
            if (string.IsNullOrEmpty(message.ItemId))
            {
                var connectionSignal = allConnectionHelpers.FirstOrDefault(x => x.PageId == message.PageId);
                if (connectionSignal != null)
                    CurrentContext.Clients.Client(connectionSignal.ConnectionId).receiveSignalMessage(message);
                return;
            }


            var connectionsSignal = allConnectionHelpers.Where(x =>
                x.EntityId != signalView && 
                
                ((x.PageId == message.PageId && receiveClients == SignaRReceiveClientsMessageEnum.Self)
                || (x.PageId != message.PageId && (receiveClients == SignaRReceiveClientsMessageEnum.ItemName 
                                               || receiveClients == SignaRReceiveClientsMessageEnum.ItemId_ItemName && x.EntityId == message.ItemId)))
                && x.ItemName.ToLower() == message.ItemName.ToLower()).ToList();

            if (connectionsSignal.Count > 0)
            {
                //Debug.WriteLine(message.Message);
                var ids = new List<string>();
                connectionsSignal.ForEach(x => { ids.Add(x.ConnectionId); });
                CurrentContext.Clients.Clients(ids).receiveSignalMessage(message);
            }
        }

        #endregion


        #region Override events 

        /// <summary>
        ///     Метод, вызываемый клиентом при установлении соединения
        /// </summary>
        /// <returns></returns>
        public override Task OnConnected()
        {
            Clients.Client(Context.ConnectionId).onPageConnected();
            return base.OnConnected();
        }

        /// <summary>
        ///     Метод, вызываемый клиентом при выполнении восстановлении соединения
        /// </summary>
        /// <returns></returns>
        public override Task OnReconnected()
        {
            var connectionHelper = _connectionServer.GetConnectionHelper(Context.ConnectionId);

            if (connectionHelper == null) return base.OnReconnected();
            if (connectionHelper.PageId == signalView) return base.OnReconnected();

            var p = _connectionServer.GetPage(connectionHelper.PageId);
            if (p == null)
                Clients.Client(Context.ConnectionId).onReconnected();

            return base.OnReconnected();
        }

        /// <summary>
        ///     Метод регистрации клиента, вызывается клиентов после установления соединения
        /// </summary>
        /// <param name="pageId">Идентификатор страницы</param>
        /// <param name="clientName">Компьютер клиента</param>
        /// <param name="userId">Код сотрудника</param>
        /// <param name="userName">Сотрудник - полное имя текущего пользователя</param>
        /// <param name="userNameLat">Employee - - полное имя текущего пользователя на английском</param>
        /// <param name="itemId">Идентификтор сущности</param>
        /// <param name="itemName">Название страницы</param>
        /// <param name="entityName">Название объекта сущности</param>
        /// <param name="isEditable">Признак того, что страница находится в режиме редактирования</param>
        /// <param name="isChanged">Признак того, что данные на страницы были изменены</param>
        /// <returns></returns>
        public async Task OnPageRegistered(
            string pageId, string clientName, 
            string userId,string userName, string userNameLat, 
            string itemId, string itemName, string entityName,
            bool isEditable, bool isChanged)
        {
            if (pageId != signalView)
            {
                var p = _connectionServer.GetPage(pageId);
                if (p == null)
                {
                    await Clients.Caller.hubImpossibleConnect();
                    return;
                }
            }


            var connectionId = Context.ConnectionId;
            var scheme = "";

            if (HttpContext.Current != null)
            {
                var req = HttpContext.Current.Request;
                if (req != null)
                {
                    clientName = GetHostName(req, clientName);
                    scheme = GetUriScheme(req);
                }
            }


            var connectionHelper = new ConnectionHelper
            {
                StartTime = DateTime.UtcNow,
                ConnectionId = connectionId,
                PageId = pageId,
                Server = Trace.GetServerName,
                Site = Trace.GetSiteName,
                VirtualPath = Trace.GetVirtualPath,
                UriScheme = scheme,
                HostName = clientName,
                TransportSignalR = Context.QueryString.First(p => p.Key == "transport").Value,
                ItemName = itemName,
                EntityName = entityName,
                UserId = userId
            };


            if (pageId != signalView)
            {
                connectionHelper.UserName = userName;
                connectionHelper.UserNameLat = userNameLat;
                connectionHelper.UserLogin = Context.User.Identity.Name;
                connectionHelper.EntityId = itemId;
                connectionHelper.ItemName = itemName;
                connectionHelper.IsEditable = isEditable;
                connectionHelper.IsChanged = isChanged;
            }

            _connectionServer.AddConnection(connectionId, connectionHelper);

            var allConnections = GetAllConnections();

            var connectionsSignal = allConnections.Where(x =>
                !string.IsNullOrEmpty(x.EntityId) && x.EntityId != "0" && !string.IsNullOrEmpty(itemId) &&
                x.EntityId != signalView && x.EntityId == itemId &&
                x.ItemName.ToLower() == itemName.ToLower()).ToList();

            if (connectionsSignal.Count > 0)
            {
                var ids = new List<string>();
                connectionsSignal.ForEach(x => { ids.Add(x.ConnectionId); });
                if (ids.Count > 1)
                    await Clients.Clients(ids).refreshActivePagesInfo(connectionsSignal);
            }

            var connectionsEntity = allConnections.Where(x => x.PageId != signalView).ToList();
            var connectionsView = allConnections.Where(x => x.PageId == signalView).ToList();

            if (connectionsView.Count > 0)
            {
                var idsSv = new List<string>();
                connectionsView.ForEach(x => { idsSv.Add(x.ConnectionId); });
                await Clients.Clients(idsSv).refreshActivePagesInfo(connectionsEntity);
            }
        }

        private string GetUriScheme(HttpRequest reg)
        {
            return reg.Url.Scheme;
        }


      
        private string GetHostName(HttpRequest req, string clientName)
        {
            if (!string.IsNullOrEmpty(req.UserHostAddress))
                try
                {
                    var entry = Dns.GetHostEntry(req.UserHostAddress);
                    var parts = entry.HostName.Split('.');
                    ;
                    return parts[0];
                }
                catch
                {
                    // ignored
                }

            return clientName;
        }


        /// <summary>
        ///     Метод, вызываемый клиентом при разрыве соединения
        /// </summary>
        /// <param name="stopCalled">Клиент штатно разорвал соединение</param>
        /// <returns></returns>
        public override Task OnDisconnected(bool stopCalled)
        {
            var connectionHelper = _connectionServer.GetConnectionHelper(Context.ConnectionId);

            if (connectionHelper != null)
            {
                var itemId = connectionHelper.EntityId;
                var itemName = connectionHelper.ItemName;
                var pageId = connectionHelper.PageId;

                _connectionServer.RemoveConnection(Context.ConnectionId, stopCalled);

                var allConnections = GetAllConnections();

                if (itemId != signalView && itemId != "0" && !string.IsNullOrEmpty(itemId))
                {
                    var connectionsSignal = allConnections.Where(x =>
                            x.EntityId != "0" && !string.IsNullOrEmpty(itemId) && x.EntityId != signalView &&
                            x.EntityId == itemId && x.ItemName.ToLower() == itemName.ToLower())
                        .ToList();
                    var ids = new List<string>();
                    connectionsSignal.ForEach(x => { ids.Add(x.ConnectionId); });
                    Clients.Clients(ids).refreshActivePagesInfo(connectionsSignal);
                }

                var connectionsEntity = allConnections.Where(x => x.PageId != signalView).ToList();
                var connectionsView = allConnections.Where(x => x.PageId == signalView).ToList();
                if (connectionsView.Count > 0)
                {
                    var idsSv = new List<string>();
                    connectionsView.ForEach(x => { idsSv.Add(x.ConnectionId); });
                    Clients.Clients(idsSv).refreshActivePagesInfo(connectionsEntity);
                }
            }

            return base.OnDisconnected(stopCalled);
        }

        #endregion


        #region Manage hub

        /// <summary>
        ///     Метод добавления подключенной страницы v4
        /// </summary>
        /// <param name="pageId">Идентификатор страницы</param>
        /// <param name="page">Объект страницы</param>
        public static void AddPage(string pageId, object page)
        {
            var connectionServer = ConnectionServer.Instance;
            connectionServer?.AddPage(pageId, page);
        }

        /// <summary>
        ///     Удаления страницы v4
        /// </summary>
        /// <param name="pageId">Идентификатор станицы</param>
        public static void RemovePage(string pageId)
        {
            var connectionServer = ConnectionServer.Instance;
            connectionServer?.RemovePage(pageId);
        }

        /// <summary>
        ///     Аксессор, вызывающий метод удаления сохраненных объектов страниц, которые потеряли соединение
        /// </summary>
        public static void RemovePagesWithOutConnection()
        {
            var connectionServer = ConnectionServer.Instance;
            var cnt = connectionServer?.RemovePagesWithOutConnection();
            var traceInfo =
                $"{DateTime.Now.ToString("dd.MM.yy HH:mm:ss")} -> Проверка актуальности страниц в KescoHub: Все соединения страниц активны";

            if (cnt > 0)
                traceInfo =
                    $"{DateTime.Now.ToString("dd.MM.yy HH:mm:ss")} -> Из KescoHub удалено {cnt} страниц, т.к. соединение с клиентом было разорвано";

            RefreshSignalViewInfo(new KescoHubTraceInfo {TraceInfo = traceInfo});
        }

        /// <summary>
        ///     Аксессор, вызывающий метод получения объекта страницы по идентификатору объекта
        /// </summary>
        /// <param name="pageId">Идентификатор страницы</param>
        /// <returns>Объект страницы</returns>
        public static object GetPage(string pageId)
        {
            var connectionServer = ConnectionServer.Instance;
            return connectionServer?.GetPage(pageId);
        }


        /// <summary>
        ///     Аксессор, вызывающий метод получения перечнья всех зарегестрированных объетов страниц
        /// </summary>
        /// <returns>Перечень объектов страниц</returns>
        public static IEnumerable<object> GetAllPages()
        {
            var connectionServer = ConnectionServer.Instance;
            return connectionServer?.GetAllPages();
        }

        /// <summary>
        ///     Аксессор, вызывающий метод получения перечня объектов всех активных соединений
        /// </summary>
        /// <returns>Перечень объектов соединений</returns>
        public IEnumerable<ConnectionHelper> GetAllConnections()
        {
            return _connectionServer.GetAllConnectionHelpers();
        }

        #endregion
    }
}