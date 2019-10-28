using System;
using System.Threading.Tasks;
using Kesco.Lib.Web.SignalR;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Startup))]

namespace Kesco.Lib.Web.SignalR
{
    /// <summary>
    ///     Класс инициализации Owin
    /// </summary>
    public class Startup
    {
        /// <summary>
        ///     Метод конфигурирования приложения, запущенного в Owin
        /// </summary>
        /// <param name="app">Приложение</param>
        public void Configuration(IAppBuilder app)
        {
            // Make long polling connections wait a maximum of 110 seconds for a
            // response. When that time expires, trigger a timeout command and
            // make the client reconnect.
            //GlobalHost.Configuration.ConnectionTimeout = TimeSpan.FromSeconds(10);

            // Wait a maximum of 30 seconds after a transport connection is lost
            // before raising the Disconnected event to terminate the SignalR connection.
            GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromSeconds(12);

            // For transports other than long polling, send a keepalive packet every
            // 10 seconds. 
            // This value must be no more than 1/3 of the DisconnectTimeout value.
            GlobalHost.Configuration.KeepAlive = TimeSpan.FromSeconds(4);


            app.MapSignalR();
            Task.WhenAll(Trace.UpdateAllHelperInDataBase());
            //app.Run(context =>
            //{
            //    var t = DateTime.Now;
            //    context.Response.Headers.Set("Content-Type", "text/plain; charset=UTF-8");
            //    return context.Response.WriteAsync(t + " Проверка работы");
            //});
        }
    }
}