#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using Microsoft.ApplicationInsights.AspNetCore;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.SnapshotCollector;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Services;
using CarWash.PWA.Controllers;
using CarWash.PWA.Hubs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using CarWash.ClassLibrary.Enums;

namespace CarWash.PWA
{
    public class Startup
    {
        private const string ContentSecurityPolicy = @"default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline' 'unsafe-eval' *.msecnd.net storage.googleapis.com; " +
                    "style-src 'self' 'unsafe-inline' fonts.googleapis.com fonts.gstatic.com; " +
                    "img-src 'self' data:; " +
                    "connect-src https: wss: 'self' fonts.googleapis.com fonts.gstatic.com; " +
                    "font-src 'self' data: fonts.googleapis.com fonts.gstatic.com; " +
                    "frame-src 'self' login.microsoftonline.com *.powerbi.com; " +
                    "form-action 'self'; " +
                    "upgrade-insecure-requests; " +
                    "report-uri https://markszabo.report-uri.com/r/d/csp/enforce";

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var config = Configuration.Get<CarWashConfiguration>();            
            if (config.Services.Count == 0)
            {
                config.Services = JsonSerializer.Deserialize<List<Service>>(Configuration.GetValue<string>("Services"), jsonOptions);
            }

            // Add application services
            services.AddSingleton(Configuration);
            services.AddSingleton(config);
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IUsersController, UsersController>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ICalendarService, CalendarService>();
            services.AddScoped<IPushService, PushService>();
            services.AddScoped<IBotService, BotService>();

            // Add framework services
            services.AddApplicationInsightsTelemetry(Configuration);
            services.AddApplicationInsightsTelemetryProcessor<SignalrTelemetryFilter>();
            // services.AddApplicationInsightsTelemetryProcessor<ForbiddenTelemetryFilter>();

            // Configure SnapshotCollector from application settings
            services.Configure<SnapshotCollectorConfiguration>(Configuration.GetSection(nameof(SnapshotCollectorConfiguration)));

            // Add SnapshotCollector telemetry processor.
            services.AddSingleton<ITelemetryProcessorFactory>(sp => new SnapshotCollectorTelemetryProcessorFactory(sp));

            services.AddDbContextPool<ApplicationDbContext>(options => options.UseSqlServer(config.ConnectionStrings.SqlDatabase));

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.Audience = config.AzureAd.ClientId;
                    options.Authority = config.AzureAd.Instance;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        /*IssuerValidator = (issuer, token, tvp) =>
                        {
                            issuer = issuer.Substring(24, 36); // Get the tenant id out of the issuer string (eg. https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/)
                            if (config.Companies.Select(i => i.TenantId).Contains(issuer))
                                return issuer;
                            else
                                throw new SecurityTokenInvalidIssuerException("Invalid issuer");
                        },*/
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            // Check if request is coming from an authorized service application.
                            var serviceAppId = context.Principal.FindFirstValue("appid");
                            if (serviceAppId != null && serviceAppId != config.AzureAd.ClientId)
                            {
                                if (config.AzureAd.AuthorizedApplications.Contains(serviceAppId))
                                {
                                    context.Principal.AddIdentity(new ClaimsIdentity([new("appId", serviceAppId)]));

                                    return;
                                }
                                throw new Exception($"Application ({serviceAppId}) is not authorized.");
                            }

                            // Get EF context
                            var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();

                            var company = (await dbContext.Company.SingleOrDefaultAsync(t => t.TenantId == context.Principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/tenantid"))) ??
                                throw new SecurityTokenInvalidIssuerException("Tenant ('tenantid') cannot be found in auth token.");
                            var email = context.Principal.FindFirstValue(ClaimTypes.Upn)?.ToLower();
                            if (email == null && company.Name == Company.Carwash) email = context.Principal.FindFirstValue(ClaimTypes.Email)?.ToLower() ??
                                context.Principal.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.ToLower().Replace("live.com#", "");
                            if (email == null) throw new Exception("Email ('upn' or 'email') cannot be found in auth token.");

                            var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == email);

                            if (user == null)
                            {
                                user = new User
                                {
                                    FirstName = context.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "User", // throw new Exception("First name ('givenname') cannot be found in auth token."),
                                    LastName = context.Principal.FindFirstValue(ClaimTypes.Surname),
                                    Email = email,
                                    Company = company.Name,
                                    IsCarwashAdmin = company.Name == Company.Carwash
                                };

                                await dbContext.Users.AddAsync(user);

                                try
                                {
                                    await dbContext.SaveChangesAsync();
                                }
                                catch (DbUpdateException)
                                {
                                    user = dbContext.Users.SingleOrDefault(u => u.Email == email);

                                    if (user != null)
                                    {
                                        Debug.WriteLine(
                                            "User already exists. Most likely the user was just created and the exception was thrown by the concurrently firing requests at the first load.");

                                        // Remove above added user from the EF Change Tracker.
                                        // It would re-throw the exception as it would try to insert it again into the db at the next SaveChanges()
                                        dbContext.ChangeTracker.Entries()
                                            .Where(e => e.Entity != null && e.State == EntityState.Added)
                                            .ToList()
                                            .ForEach(e => e.State = EntityState.Detached);
                                    }
                                    else throw;
                                }
                            }

                            var claims = new List<Claim>
                            {
                                new Claim("admin", user.IsAdmin.ToString()),
                                new Claim("carwashadmin", user.IsCarwashAdmin.ToString())
                            };
                            context.Principal.AddIdentity(new ClaimsIdentity(claims));
                        },
                        OnAuthenticationFailed = context =>
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                            context.Response.ContentType = "application/json";
                            return Task.CompletedTask;
                        }
                    };
                });

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

            services.Configure<HstsOptions>(options =>
            {
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            // Configure SignalR
            services.AddSignalR();

            // Swagger API Documentation generator
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v2", new OpenApiInfo { Title = "CarWash API", Version = "v2" });

                var authority = $"{config.AzureAd.Instance}oauth2/v2.0";
                c.AddSecurityDefinition("OAuth2", new OpenApiSecurityScheme
                {
                    Description = "OAuth2 SSO authentication.",
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        Implicit = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri(authority + "/authorize"),
                            TokenUrl = new Uri(authority + "/connect/token"),
                            Scopes = new Dictionary<string, string>
                            {
                                { "openid","User offline" },
                            }
                        }
                    }
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header
                        },
                        new List<string>()
                    }
                });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                try
                {
                    c.IncludeXmlComments(xmlPath);
                }
                catch (FileNotFoundException) { }
            });

            services.AddHealthChecks();

            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
        {
            var config = Configuration.Get<CarWashConfiguration>();

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

            app.UseAuthentication();

            app.Use(async (context, next) =>
            {
                context.Response.Headers.Append("X-Frame-Options", new[] { "SAMEORIGIN" });
                //context.Response.Headers.Append("Strict-Transport-Security", new[] { "max-age=31536000; includeSubDomains" });
                context.Response.Headers.Append("X-XSS-Protection", new[] { "1; mode=block; report=https://markszabo.report-uri.com/r/d/xss/enforce" });
                context.Response.Headers.Append("X-Content-Type-Options", new[] { "nosniff" });
                context.Response.Headers.Append("Referrer-Policy", new[] { "strict-origin-when-cross-origin" });
                context.Response.Headers.Append("Permissions-Policy", new[] { "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()" });
                context.Response.Headers.Append("Content-Security-Policy", new[] { ContentSecurityPolicy });
                context.Response.Headers.Remove(HeaderNames.Server);
                context.Response.Headers.Remove("X-Powered-By");
                await next();
            });

            app.UseResponseCompression();

            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    const int cacheExpirationInSeconds = 60 * 60 * 24; // one day
                    ctx.Context.Response.Headers[HeaderNames.CacheControl] =
                        "public,max-age=" + cacheExpirationInSeconds;
                }
            });
            app.UseSpaStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/api/healthcheck");
                endpoints.MapHub<BacklogHub>("/hub/backlog");
                endpoints.MapControllerRoute("default", "{controller}/{action=Index}/{id?}");
            });

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v2/swagger.json", "CarWash API");
                c.EnableDeepLinking();
                c.DocumentTitle = "CarWash API";
                c.OAuth2RedirectUrl("https://localhost:44340/swagger/index.html");
                c.OAuthClientId(config.AzureAd.ClientId);
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

        private class SnapshotCollectorTelemetryProcessorFactory : ITelemetryProcessorFactory
        {
            private readonly IServiceProvider _serviceProvider;

            public SnapshotCollectorTelemetryProcessorFactory(IServiceProvider serviceProvider) =>
                _serviceProvider = serviceProvider;

            public ITelemetryProcessor Create(ITelemetryProcessor next)
            {
                var snapshotConfigurationOptions = _serviceProvider.GetService<IOptions<SnapshotCollectorConfiguration>>();
                return new SnapshotCollectorTelemetryProcessor(next, configuration: snapshotConfigurationOptions.Value);
            }
        }

        private class ForbiddenTelemetryFilter : ITelemetryProcessor
        {
            private ITelemetryProcessor Next { get; set; }

            public ForbiddenTelemetryFilter(ITelemetryProcessor next)
            {
                Next = next;
            }

            public void Process(ITelemetry item)
            {
                // Filter 401 Forbidden responses
                if (item is RequestTelemetry request && request.ResponseCode.Equals("401", StringComparison.OrdinalIgnoreCase)) return;

                // Send everything else
                Next.Process(item);
            }
        }

        private class SignalrTelemetryFilter : ITelemetryProcessor
        {
            private ITelemetryProcessor Next { get; set; }

            public SignalrTelemetryFilter(ITelemetryProcessor next)
            {
                Next = next;
            }

            public void Process(ITelemetry item)
            {
                // Filter SignalR responses
                if (item is RequestTelemetry request && request.Name != null && request.Name.Contains("/hub/"))
                    return;

                // Send everything else
                Next.Process(item);
            }
        }
    }
}
