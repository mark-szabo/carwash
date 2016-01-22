using MSHU.CarWash.App_Start;
using MSHU.CarWash.DAL;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Security;
using System.Web.SessionState;

namespace MSHU.CarWash
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<MSHUCarWashContext, MSHU.CarWash.Migrations.Configuration>("MSHUCarWashConnectionString"));

            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}