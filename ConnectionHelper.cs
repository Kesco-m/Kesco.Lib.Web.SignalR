using System;

namespace Kesco.Lib.Web.SignalR
{
    /// <summary>
    ///     Класс, описыващий соединение SignalR
    /// </summary>
    public class ConnectionHelper
    {
        /// <summary>
        ///     Идентификатор соединения (GUID) - генерится автоматически
        /// </summary>
        public string ConnectionId { get; set; }

        /// <summary>
        ///     Идентификатор страницы V4
        /// </summary>
        public string PageId { get; set; }


        /// <summary>
        ///     КодСотрудника - текущий пользователь, открывший страницу
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        ///     Сотрудник - полное имя текущего пользователя
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        ///     Employee - - полное имя текущего пользователя на английском
        /// </summary>
        public string UserNameLat { get; set; }

        /// <summary>
        ///     Логин текущего пользователя
        /// </summary>
        public string UserLogin { get; set; }

        /// <summary>
        ///     Сервер
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        ///     Сайт
        /// </summary>
        public string Site { get; set; }

        /// <summary>
        ///     Схема
        /// </summary>
        public string UriScheme { get; set; }

        /// <summary>
        ///     Сайт
        /// </summary>
        public string VirtualPath { get; set; }

        /// <summary>
        ///     Компьютер клиента
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        ///     Транспорт соединения
        /// </summary>
        public string TransportSignalR { get; set; }

        /// <summary>
        ///     Название страницы
        /// </summary>
        public string ItemName { get; set; }

        /// <summary>
        ///     Идентификтор сущности
        /// </summary>
        public string EntityId { get; set; }

        /// <summary>
        ///     Название сущности
        /// </summary>
        public string EntityName { get; set; }

        /// <summary>
        ///     Текстовое значения свойства IsEditable
        /// </summary>
        public string EntityState => IsEditable ? "редактирует" : "просматривает";


        /// <summary>
        ///     Признак того, что страница находится в режиме редактирования
        /// </summary>
        public bool IsEditable { get; set; }

        /// <summary>
        ///     Признак того, что данные на страницы были изменены
        /// </summary>
        public bool IsChanged { get; set; }

        /// <summary>
        ///     Время установления постоянного соединения
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        ///     Преобразование времени постоянного соединения к формату "yyyy-MM-dd HH:mm:ss" для последующей локализации на
        ///     клиенте
        /// </summary>
        public string StartTimeFormat => StartTime.ToString("yyyy-MM-dd HH:mm:ss");
    }
}