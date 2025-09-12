#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Services;
using CarWash.PWA.Hubs;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.AspNetCore;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.SnapshotCollector;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Azure.Devices;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using static CarWash.ClassLibrary.Constants;

namespace CarWash.PWA
{
    public class Startup(IConfiguration configuration, IWebHostEnvironment currentEnvironment)
    {
        private const string ContentSecurityPolicy = @"default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline' 'unsafe-eval' *.msecnd.net storage.googleapis.com static.cloudflareinsights.com; " +
                    "style-src 'self' 'unsafe-inline' fonts.googleapis.com fonts.gstatic.com; " +
                    "img-src 'self' data:; " +
                    "connect-src https: wss: 'self' fonts.googleapis.com fonts.gstatic.com; " +
                    "font-src 'self' data: fonts.googleapis.com fonts.gstatic.com; " +
                    "frame-src 'self' login.microsoftonline.com *.powerbi.com; " +
                    "form-action 'self'; " +
                    "upgrade-insecure-requests; " +
                    "report-uri https://markszabo.report-uri.com/r/d/csp/enforce";

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAzureAppConfiguration();
            services.AddFeatureManagement();

            services.Configure<CarWashConfiguration>(configuration);
            services.PostConfigure<CarWashConfiguration>(config =>
            {
                if (config.Services.Count == 0)
                {
                    config.Services = JsonSerializer.Deserialize<List<Service>>(configuration.GetValue<string>("Services") ?? throw new Exception("Services are missing from configuration."), DefaultJsonSerializerOptions) ?? throw new Exception("Parsed Services are null.");
                }

                config.BuildNumber = configuration.GetValue<string>("BUILD_NUMBER") ?? "0.0.0";
            });
            var config = configuration.Get<CarWashConfiguration>() ?? throw new Exception("Failed to parse configuration to CarWashConfiguration.");

            var blobServiceClient = new BlobServiceClient(config.ConnectionStrings.StorageAccount);
            services.AddSingleton(blobServiceClient);

            var queueServiceClient = new QueueServiceClient(config.ConnectionStrings.StorageAccount);
            services.AddSingleton(queueServiceClient);

            var iotHubServiceClient = ServiceClient.CreateFromConnectionString(configuration.GetConnectionString("IotHub"), Microsoft.Azure.Devices.TransportType.Amqp, new ServiceClientOptions { SdkAssignsMessageId = Microsoft.Azure.Devices.Shared.SdkAssignsMessageId.WhenUnset });
            services.AddSingleton(iotHubServiceClient);

            // Add application services
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IBlobStorageService, BlobStorageService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ICalendarService, CalendarService>();
            services.AddScoped<IPushService, PushService>();
            services.AddScoped<IBotService, BotService>();
            services.AddScoped<IKeyLockerService, KeyLockerService>();
            services.AddHttpClient<ICloudflareService, CloudflareService>();

            // Add framework services
            services.AddApplicationInsightsTelemetry(configuration);
            services.AddApplicationInsightsTelemetryProcessor<SignalrTelemetryFilter>();
            // services.AddApplicationInsightsTelemetryProcessor<ForbiddenTelemetryFilter>();

            // Configure SnapshotCollector from application settings
            services.Configure<SnapshotCollectorConfiguration>(configuration.GetSection(nameof(SnapshotCollectorConfiguration)));

            // Add SnapshotCollector telemetry processor.
            services.AddSingleton<ITelemetryProcessorFactory>(sp => new SnapshotCollectorTelemetryProcessorFactory(sp));

            services.AddDbContextPool<ApplicationDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("SqlDatabase"), o => o.EnableRetryOnFailure()));

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddMicrosoftIdentityWebApi(options =>
                {
                    options.IncludeErrorDetails = true;
                    options.Audience = configuration["AzureAd:ClientId"];
                    options.Authority = configuration["AzureAd:Instance"];
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = true,
                        ValidateIssuer = true,
                        ValidateLifetime = true,
                        RequireExpirationTime = true,
                        RequireSignedTokens = true,
                        RequireAudience = true,
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var telemetryClient = context.HttpContext.RequestServices.GetService<TelemetryClient>();

                            // Check if request is coming from an authorized service application.
                            var serviceAppId = context.Principal?.FindFirstValue("appid");
                            if (serviceAppId != null && serviceAppId != configuration["AzureAd:ClientId"])
                            {
                                if (configuration["AzureAd:AuthorizedApplications"]?.Contains(serviceAppId) == true)
                                {
                                    context.Principal?.AddIdentity(new ClaimsIdentity([new("appId", serviceAppId)]));

                                    return;
                                }

                                telemetryClient?.TrackEvent("UnauthorizedServiceApp", new Dictionary<string, string?>
                                {
                                    ["AppId"] = serviceAppId,
                                    ["TenantId"] = context.Principal?.FindFirstValue(ClaimConstants.Tid) ?? context.Principal?.FindFirstValue(ClaimConstants.TenantId),
                                    ["UserId"] = context.Principal?.FindFirstValue(ClaimConstants.Oid) ?? context.Principal?.FindFirstValue(ClaimConstants.ObjectId),
                                });
                                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                                context.Response.ContentType = "application/json";
                                context.Response.Headers.Append("WWW-Authenticate", "Bearer error=\"invalid_token\", error_description=\"The access token is not authorized\"");

                                return;
                            }

                            // Get EF context
                            var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();

                            var entraTenantId = context.Principal?.FindFirstValue(ClaimConstants.Tid) ??
                                context.Principal?.FindFirstValue(ClaimConstants.TenantId) ??
                                throw new SecurityTokenInvalidIssuerException("Tenant ('tid' or 'tenantid') cannot be found in auth token.");

                            var entraOid = context.Principal.FindFirstValue(ClaimConstants.Oid) ??
                                context.Principal.FindFirstValue(ClaimConstants.ObjectId);

                            var company = (await dbContext.Company.SingleOrDefaultAsync(t => t.TenantId == entraTenantId)) ??
                                throw new SecurityTokenInvalidIssuerException("Tenant ('tenantid') cannot be found in auth token.");
                            var email = context.Principal.FindFirstValue(ClaimTypes.Upn)?.ToLower();
                            if (email == null && company.Name == Company.Carwash) email = context.Principal.FindFirstValue(ClaimTypes.Email)?.ToLower() ??
                                context.Principal.FindFirstValue(ClaimTypes.Name)?.ToLower().Replace("live.com#", "");
                            if (email == null) throw new Exception("Email ('upn' or 'email') cannot be found in auth token.");

                            var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Oid == entraOid);

                            if (user == null)
                            {
                                user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == email);
                                if (user != null)
                                {
                                    user.Oid = entraOid;
                                    dbContext.Update(user);
                                    await dbContext.SaveChangesAsync();
                                }
                            }

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
                                catch (Exception ex) when (ex is DbUpdateException || ex is SqlException)
                                {
                                    user = dbContext.Users.SingleOrDefault(u => u.Email == email);

                                    if (user != null)
                                    {
                                        telemetryClient?.TrackException(ex, new Dictionary<string, string?>
                                        {
                                            ["UserId"] = user.Id,
                                            ["Email"] = user.Email,
                                            ["Message"] = "User already exists. Most likely the user was just created and the exception was thrown by the concurrently firing requests at the first load.",
                                        });

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

                            if (company.Color == null || user.PhoneNumber == null)
                            {
                                try
                                {
                                    // Graph API
                                    var tokenAcquisition = context.HttpContext.RequestServices.GetRequiredService<ITokenAcquisition>();

                                    var graphClient = new GraphServiceClient(
                                        new BaseBearerTokenAuthenticationProvider(
                                            new TokenAcquisitionTokenProvider(
                                                tokenAcquisition,
                                                ["User.Read"],
                                                context.Principal)));

                                    if (user.PhoneNumber == null)
                                        try
                                        {
                                            // Get user information from Graph
                                            var graphUser = await graphClient.Me.GetAsync((requestConfiguration) =>
                                            {
                                                requestConfiguration.QueryParameters.Select = ["mobilePhone", "businessPhones"];
                                            });
                                            var phoneNumber = graphUser?.MobilePhone ?? (graphUser?.BusinessPhones?.Count > 0 ? graphUser.BusinessPhones[0] : null);
                                            if (phoneNumber != null)
                                            {
                                                user.PhoneNumber = phoneNumber;
                                                dbContext.Update(user);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            telemetryClient?.TrackException(ex, new Dictionary<string, string?>
                                            {
                                                ["UserId"] = user.Id,
                                                ["Email"] = user.Email,
                                                ["Company"] = company.Name,
                                                ["Message"] = "Error getting user from Graph.",
                                            });
                                        }

                                    if (company.Color == null)
                                        try
                                        {
                                            // Get company information from Graph
                                            var branding = await graphClient.Organization[entraTenantId]
                                            .Branding
                                            .GetAsync((requestConfiguration) =>
                                            {
                                                requestConfiguration.Headers.Add("Accept-Language", "0");
                                                requestConfiguration.QueryParameters.Select = ["backgroundColor", "bannerLogoRelativeUrl", "CdnList"];
                                            });
                                            if (branding?.BackgroundColor != null)
                                            {
                                                company.Color = branding?.BackgroundColor;
                                                company.UpdatedOn = DateTime.UtcNow;
                                                dbContext.Update(company);
                                            }
                                            if (branding?.BannerLogoRelativeUrl != null)
                                            {
                                                var companyLogo = $"https://{branding.CdnList?[0]}/{branding.BannerLogoRelativeUrl}";

                                                var blobStorageService = context.HttpContext.RequestServices.GetRequiredService<IBlobStorageService>();
                                                await blobStorageService.UploadCompanyLogoFromUrlAsync(companyLogo, company.Name);
                                            }
                                        }
                                        catch (Microsoft.Graph.Models.ODataErrors.ODataError ex)
                                        {
                                            telemetryClient?.TrackException(ex, new Dictionary<string, string?>
                                            {
                                                ["UserId"] = user.Id,
                                                ["Email"] = user.Email,
                                                ["Company"] = company.Name,
                                                ["Message"] = "Error getting company branding information from Graph.",
                                            });

                                            company.Color = "#80d8ff"; // default to carwash blue
                                            company.UpdatedOn = DateTime.UtcNow;
                                            dbContext.Update(company);
                                        }

                                    await dbContext.SaveChangesAsync();
                                }
                                catch (Exception ex)
                                {
                                    telemetryClient?.TrackException(ex, new Dictionary<string, string?>
                                    {
                                        ["UserId"] = user.Id,
                                        ["Email"] = user.Email,
                                        ["Company"] = company.Name,
                                        ["Message"] = "Error connecting to Microsoft Graph.",
                                    });
                                }
                            }

                            // Add claims directly to the existing identity
                            if (context.Principal.Identity is ClaimsIdentity identity)
                            {
                                identity.AddClaim(new Claim("userid", user.Id));
                                identity.AddClaim(new Claim("admin", user.IsAdmin.ToString()));
                                identity.AddClaim(new Claim("carwashadmin", user.IsCarwashAdmin.ToString()));
                            }
                            context.HttpContext.User = context.Principal;
                            context.HttpContext.Items["CurrentUser"] = user;
                        },
                        OnAuthenticationFailed = context =>
                        {
                            var telemetryClient = context.HttpContext.RequestServices.GetService<TelemetryClient>();
                            telemetryClient?.TrackException(context.Exception);

                            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                            context.Response.ContentType = "application/json";
                            return Task.CompletedTask;
                        },
                        // We have to hook the OnMessageReceived event in order to
                        // allow the JWT authentication handler to read the access
                        // token from the query string when a WebSocket or 
                        // Server-Sent Events request comes in.

                        // Sending the access token in the query string is required when using WebSockets or ServerSentEvents
                        // due to a limitation in Browser APIs. We restrict it to only calls to the
                        // SignalR hub in this code.
                        // See https://docs.microsoft.com/aspnet/core/signalr/security#access-token-logging
                        // for more information about security considerations when using
                        // the query string to transmit the access token.
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];

                            // If the request is for our hub...
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) &&
                                (path.StartsWithSegments("/hub")))
                            {
                                // Read the token out of the query string
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                }, options =>
                {
                    if (currentEnvironment.IsDevelopment())
                    {
                        IdentityModelEventSource.ShowPII = true;
                    }
                    configuration.Bind("AzureAd", options);
                })
                .EnableTokenAcquisitionToCallDownstreamApi(
                    options =>
                    {
                        configuration.Bind("AzureAd", options);
                    })
                .AddInMemoryTokenCaches();

            services.AddAuthorization();

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });

            // Add gzip compression
            if (!currentEnvironment.IsDevelopment())
            {
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
            }

            // Configure SignalR
            services.AddSignalR();
            services.AddSingleton<IUserIdProvider, SignalRUserIdProvider>();

            if (currentEnvironment.IsDevelopment())
            {
                // Swagger API Documentation generator
                services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v2", new OpenApiInfo { Title = "CarWash API", Version = "v2" });

                    var authority = $"{configuration["AzureAd:Instance"]}oauth2/v2.0";
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
            }

            services.AddHealthChecks()
                .AddDbContextCheck<ApplicationDbContext>(name: "azure_sql_db_context")
                //.AddSignalRHub($"{config.ConnectionStrings.BaseUrl}/hub/backlog", "signalr_hub_backlog") - needs authentication
                //.AddSignalRHub($"{config.ConnectionStrings.BaseUrl}/hub/keylocker", "signalr_hub_keylocker")
                .AddAzureBlobStorage()
                .AddAzureQueueStorage()
                .AddAzureIoTHubServiceClient()
                .AddAzureServiceBusQueue(config.ConnectionStrings.KeyLockerServiceBus, KeyLockerServiceBusQueueName, name: "azure_servicebus_keylocker")
                .AddAzureApplicationInsights(configuration.GetValue<string>("APPLICATIONINSIGHTS_CONNECTION_STRING")?.Split(';')?[0][19..], name: "azure_appinsights")
                .AddApplicationInsightsPublisher();

            services.AddControllers(options => { options.Filters.Add(new AuthorizeFilter(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())); }).AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
        {
            app.UseHealthChecks("/api/healthcheck", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    var result = JsonSerializer.Serialize(
                        new
                        {
                            status = report.Status.ToString(),
                            services = report.Entries.Select(e => new { key = e.Key, value = Enum.GetName(typeof(HealthStatus), e.Value.Status) })
                        });
                    context.Response.ContentType = MediaTypeNames.Application.Json;
                    await context.Response.WriteAsync(result);
                }
            });

            app.UseAzureAppConfiguration();
            var config = configuration.Get<CarWashConfiguration>();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseResponseCompression();
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

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

            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    const int cacheExpirationInSeconds = 60 * 60 * 24; // one day
                    ctx.Context.Response.Headers[HeaderNames.CacheControl] =
                        "public,max-age=" + cacheExpirationInSeconds;
                }
            });

            app.UseSpaStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = context =>
                {
                    if (!context.File.Exists && Path.HasExtension(context.Context.Request.Path.Value))
                    {
                        context.Context.Response.StatusCode = 404;
                        if (context.Context.Request.Path.Value.Contains(".js"))
                        {
                            context.Context.Response.ContentType = "application/js";
                        }
                        else if (context.Context.Request.Path.Value.Contains(".css"))
                        {
                            context.Context.Response.ContentType = "text/css";
                        }
                    }
                }
            });

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<BacklogHub>("/hub/backlog");
                endpoints.MapHub<KeyLockerHub>("/hub/keylocker");
                endpoints.MapControllerRoute("default", "{controller}/{action=Index}/{id?}");
            });

            if (env.IsDevelopment())
            {
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
            }

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });
        }

        public class SignalRUserIdProvider : IUserIdProvider
        {
            public string GetUserId(HubConnectionContext connection)
            {
                // Get the user id from the ClaimsPrincipal
                return connection.User?.FindFirstValue("userid") ?? throw new Exception("User id ('userid') cannot be found in claims principal.");
            }
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
