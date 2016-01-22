using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(MSHU.CarWash.Services.Startup))]

namespace MSHU.CarWash.Services
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureMobileApp(app);
        }
    }
}