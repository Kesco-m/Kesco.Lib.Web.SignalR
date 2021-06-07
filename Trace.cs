using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using Kesco.Lib.DALC;
using Kesco.Lib.Web.Settings;

namespace Kesco.Lib.Web.SignalR
{
    public class Trace
    {
        public static async Task UpdateHelperInDataBase(string connectionId, DateTime startTime, bool stopCalled)
        {
            await Task.Run(() =>
            {
                var sqlParams = new Dictionary<string, object>
                {
                    {"@ConnectionId", connectionId},
                    {"@ЗавершениеРаботы", DateTime.UtcNow},
                    {"@КакЗавершиласьРабота", stopCalled ? 1 : 2}
                };
                var sqlText = @"
UPDATE  SignalRLog
SET     ЗавершениеРаботы = @ЗавершениеРаботы,
        КакЗавершиласьРабота = @КакЗавершиласьРабота        
WHERE   ConnectionId = @ConnectionId 
";
                if (startTime != DateTime.MinValue)
                {
                    sqlParams.Add("@НачалоРаботы", startTime);
                    sqlText = @"
UPDATE  SignalRLog
SET     ЗавершениеРаботы = @ЗавершениеРаботы,
        КакЗавершиласьРабота = @КакЗавершиласьРабота        
WHERE   НачалоРаботы = @НачалоРаботы AND ConnectionId = @ConnectionId 
";
                }

                try
                {
                    DBManager.ExecuteNonQuery(sqlText, CommandType.Text, Config.DS_signalr, sqlParams);
                }
                catch
                {
                    // ignored
                }
            });
        }

        public static async Task UpdateAllHelperInDataBase()
        {
            await Task.Run(() =>
            {
                var server = GetServerName;
                var site = GetSiteName;
                var virtualPath = GetVirtualPath;

                var sqlParams = new Dictionary<string, object>
                {
                    {"@ЗавершениеРаботы", DateTime.UtcNow},
                    {"@КакЗавершиласьРабота", 0},
                    {"@Server", server},
                    {"@Site", site},
                    {"@VirtualPath", virtualPath}
                };

                var sqlText = @"
DECLARE @Дата date = DATEADD(day, -1, GETDATE())
UPDATE  SignalRLog
SET     ЗавершениеРаботы = @ЗавершениеРаботы,
        КакЗавершиласьРабота = @КакЗавершиласьРабота        
WHERE   ЗавершениеРаботы IS NULL AND НачалоРаботы > @Дата AND Server = @Server AND @Site = Site AND @VirtualPath = VirtualPath
";
                try
                {
                    DBManager.ExecuteNonQuery(sqlText, CommandType.Text, Config.DS_signalr, sqlParams);
                }
                catch
                {
                    // ignored
                }
            });
        }

        public static async Task InsertHelperInDataBase(ConnectionHelper connection)
        {
            await Task.Run(() =>
            {
                var sqlParams = new Dictionary<string, object>
                {
                    {"@НачалоРаботы", connection.StartTime},
                    {"@ConnectionId", connection.ConnectionId},

                    {"@Server", connection.Server},
                    {"@Site", connection.Site},
                    {"@VirtualPath", connection.VirtualPath},
                    {"@UriScheme", connection.UriScheme},
                    {"@HostName", connection.HostName.ToUpper()},
                    {"@TransportSignalR", connection.TransportSignalR},
                    {"@НазваниеФормы", connection.ItemName},

                    {"@EntityName", string.IsNullOrEmpty(connection.EntityName) ? DBNull.Value : (object)connection.EntityName},
                    {"@EntityId", connection.EntityId},
                    {"@EntityState", connection.EntityState},

                    { "@КодСотрудника", string.IsNullOrEmpty(connection.UserId) ? DBNull.Value : (object)connection.UserId},
                };

                var sqlText = @"
IF (@EntityName IS NULL)
BEGIN
    SET @EntityId = NULL
    SET @EntityState = NULL
END
INSERT SignalRLog(НачалоРаботы, ConnectionId, Server, Site, VirtualPath, UriScheme, HostName, TransportSignalR, НазваниеФормы, EntityName, EntityId, EntityState, КодСотрудника)
VALUES(@НачалоРаботы, @ConnectionId, @Server, @Site, @VirtualPath, @UriScheme, @HostName, @TransportSignalR, @НазваниеФормы, @EntityName, @EntityId, @EntityState, @КодСотрудника)

";
                try
                {
                    DBManager.ExecuteNonQuery(sqlText, CommandType.Text, Config.DS_signalr, sqlParams);
                }
                catch
                {
                    // ignored
                }
            });
        }

        public static string GetVirtualPath => HttpRuntime.AppDomainAppVirtualPath;
        public static string GetSiteName => HostingEnvironment.ApplicationHost.GetSiteName();
        public static string GetServerName => Environment.MachineName;
    }
}