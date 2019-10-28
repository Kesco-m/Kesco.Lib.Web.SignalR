namespace Kesco.Lib.Web.SignalR
{
    /// <summary>
    ///     Сериализуемый в JSON объект, для отправки на клиента
    /// </summary>
    public class SignalMessage
    {
        /// <summary>
        ///     Идентификтор  страницы
        /// </summary>
        public string PageId { get; set; }

        /// <summary>
        ///     Текстовое сообщение или javascript в формате js
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        ///     Признак того, что сообщение в формате js
        /// </summary>
        public bool IsV4Script { get; set; }

        /// <summary>
        ///     Идентификатор открытого объекта
        /// </summary>
        public string ItemId { get; set; }

        /// <summary>
        ///     Название формы
        /// </summary>
        public string ItemName { get; set; }
    }
}