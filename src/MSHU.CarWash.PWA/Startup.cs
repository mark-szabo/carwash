using System.IO.Compression;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Net.Http.Headers;

namespace MSHU.CarWash.PWA
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetry(Configuration);

            //services.AddDbContextPool<ApplicationDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });

            // Add gzip compression
            services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);
            services.AddResponseCompression(options =>
            {
                options.Providers.Add<GzipCompressionProvider>();
                //options.EnableForHttps = true;
                options.MimeTypes = new[]
                {
                    // Default
                    "text/plain",
                    "text/css",
                    "application/javascript",
                    "text/html",
                    "application/xml",
                    "text/xml",
                    "application/json",
                    "text/json",
 
                    // Custom
                    "image/svg+xml",
                    "application/font-woff2"
                };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("X-Frame-Options", new[] { "DENY" });
                //context.Response.Headers.Add("Strict-Transport-Security", new[] { "max-age=31536000; includeSubDomains; preload" });
                context.Response.Headers.Add("Expect-CT", new[] { "expect-ct: max-age=604800, report-uri=https://markszabo.report-uri.com/r/d/ct/enforce" });
                context.Response.Headers.Add("X-XSS-Protection", new[] { "1; mode=block; report=https://markszabo.report-uri.com/r/d/xss/enforce" });
                context.Response.Headers.Add("X-Content-Type-Options", new[] { "nosniff" });
                context.Response.Headers.Add("Referrer-Policy", new[] { "strict-origin-when-cross-origin" });
                context.Response.Headers.Add("Feature-Policy", new[] { "accelerometer 'none'; camera 'none'; geolocation 'self'; gyroscope 'none'; magnetometer 'none'; microphone 'none'; payment 'none'; usb 'none'" });
                context.Response.Headers.Add("Content-Security-Policy", new[] { "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline' fonts.googleapis.com fonts.gstatic.com; img-src 'self'; connect-src 'self' fonts.googleapis.com fonts.gstatic.com; font-src 'self' fonts.googleapis.com fonts.gstatic.com; form-action 'self'; upgrade-insecure-requests; report-uri https://markszabo.report-uri.com/r/d/csp/enforce" });
                context.Response.Headers.Remove(HeaderNames.Server);
                context.Response.Headers.Remove("X-Powered-By");
                await next();
            });

            app.UseResponseCompression();

            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    const int cacheExpirationInSeconds = 60 * 60 * 24 * 30; //one month
                    ctx.Context.Response.Headers[HeaderNames.CacheControl] =
                        "public,max-age=" + cacheExpirationInSeconds;
                }
            });
            app.UseSpaStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });
        }
    }
}
