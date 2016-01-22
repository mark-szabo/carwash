using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(MSHU.CarWash.Startup))]

namespace MSHU.CarWash
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}