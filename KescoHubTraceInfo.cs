namespace Kesco.Lib.Web.SignalR
{
    /// <summary>
    ///     Вспомогательный класс, использующийся для сбора информации о работе SignalR
    /// </summary>
    public class KescoHubTraceInfo
    {
        /// <summary>
        ///     Количество страниц в словаре ConnectioServer
        /// </summary>
        public int CountPages { get; set; }

        /// <summary>
        ///     Сообщение трасировки(описание текущего действия)
        /// </summary>
        public string TraceInfo { get; set; }
    }
}