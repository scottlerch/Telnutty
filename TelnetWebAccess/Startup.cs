using Owin;
using Microsoft.Owin;
using TelnetWebAccess;

[assembly: OwinStartup(typeof(Startup))]
namespace TelnetWebAccess
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Any connection or hub wire up and configuration should go here
            app.MapSignalR();
        }
    }
}