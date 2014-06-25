using Owin;
using Microsoft.Owin;
using Telnutty;

[assembly: OwinStartup(typeof(Startup))]
namespace Telnutty
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