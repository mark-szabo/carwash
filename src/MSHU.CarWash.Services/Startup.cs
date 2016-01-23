using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(MSHU.CarWash.Services.Startup))]

namespace MSHU.CarWash.Services
{
    // A .NET backend server project is initialized similar to other ASP.NET projects, 
    // by including an OWIN startup class.
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureMobileApp(app);
            ConfigureAutoMapper();
        }
    }
}