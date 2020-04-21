// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using CarWash.Bot.Middlewares;
using CarWash.Bot.Proactive;
using CarWash.Bot.States;
using CarWash.ClassLibrary.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.AspNetCore;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.SnapshotCollector;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarWash.Bot
{
    /// <summary>
    /// The Startup class configures services and the app's request pipeline.
    /// </summary>
    public class Startup
    {
        private readonly bool _isProduction;
        private ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// This method gets called by the runtime and adds services to the container.
        /// </summary>
        /// <param name="env">Provides information about the web hosting environment an application is running in.</param>
        public Startup(IWebHostEnvironment env)
        {
            _isProduction = env.IsProduction();

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        /// <summary>
        /// Gets the configuration that represents a set of key/value application configuration properties.
        /// </summary>
        /// <value>
        /// The <see cref="IConfiguration"/> that represents a set of key/value application configuration properties.
        /// </value>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">Specifies the contract for a <see cref="IServiceCollection"/> of service descriptors.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            var config = Configuration.Get<CarWashConfiguration>();
            services.AddSingleton(config);

            var secretKey = Configuration.GetSection("botFileSecret")?.Value;
            var botFilePath = Configuration.GetSection("botFilePath")?.Value;

            // Loads .bot configuration file and adds a singleton that your Bot can access through dependency injection.
            BotConfiguration botConfig;
            try
            {
                botConfig = BotConfiguration.Load(botFilePath ?? @".\carwashubot.bot", secretKey);
            }
            catch (Exception)
            {
                var msg = @"Error reading bot file. Please ensure you have valid botFilePath and botFileSecret set for your environment.
    - You can find the botFilePath and botFileSecret in the Azure App Service application settings.
    - If you are running this bot locally, consider adding a appsettings.json file with botFilePath and botFileSecret.
    - See https://aka.ms/about-bot-file to learn more about .bot file its use and bot configuration.
    ";
                throw new InvalidOperationException(msg);
            }

            services.AddSingleton(sp => botConfig ?? throw new InvalidOperationException($"The .bot config file could not be loaded. ({botConfig})"));

            // Add BotServices singleton.
            // Create the connected services from .bot file.
            services.AddSingleton(sp => new BotServices(botConfig));

            // Retrieve current endpoint.
            var environment = _isProduction ? "production" : "development";
            var service = botConfig.Services.FirstOrDefault(s => s.Type == "endpoint" && s.Name == environment);
            if (!(service is EndpointService endpointService))
            {
                throw new InvalidOperationException($"The .bot file does not contain an endpoint with name '{environment}'.");
            }

            // Configure AppInsights
            services.AddApplicationInsightsTelemetry(Configuration);

            // Configure SnapshotCollector from application settings
            services.Configure<SnapshotCollectorConfiguration>(Configuration.GetSection(nameof(SnapshotCollectorConfiguration)));

            // Add SnapshotCollector telemetry processor.
            services.AddSingleton<ITelemetryProcessorFactory>(sp => new SnapshotCollectorTelemetryProcessorFactory(sp));

            // Add proactive message services
            services.AddSingleton<DropoffReminderMessage, DropoffReminderMessage>();
            services.AddSingleton<WashStartedMessage, WashStartedMessage>();
            services.AddSingleton<WashCompletedMessage, WashCompletedMessage>();
            services.AddSingleton<CarWashCommentLeftMessage, CarWashCommentLeftMessage>();
            services.AddSingleton<VehicleArrivedMessage, VehicleArrivedMessage>();

            // Memory Storage is for local bot debugging only. When the bot
            // is restarted, everything stored in memory will be gone.
            // IStorage dataStore = new MemoryStorage();

            // Storage configuration name or ID from the .bot file.
            const string storageConfigurationId = "carwashstorage";
            var blobConfig = botConfig.FindServiceByNameOrId(storageConfigurationId);
            if (!(blobConfig is BlobStorageService blobStorageConfig))
            {
                throw new InvalidOperationException($"The .bot file does not contain an blob storage with name '{storageConfigurationId}'.");
            }

            // Default container name.
            const string defaultBotContainer = "botstate";
            var storageContainer = string.IsNullOrWhiteSpace(blobStorageConfig.Container) ? defaultBotContainer : blobStorageConfig.Container;
            IStorage dataStore = new AzureBlobStorage(blobStorageConfig.ConnectionString, storageContainer);

            // Create and add conversation state.
            var conversationState = new ConversationState(dataStore);
            services.AddSingleton(conversationState);

            // Create and add user state.
            var userState = new UserState(dataStore);
            services.AddSingleton(userState);

            services.AddBot<CarWashBot>(options =>
            {
                options.CredentialProvider = new SimpleCredentialProvider(endpointService.AppId, endpointService.AppPassword);

                // Enable the show typing middleware.
                options.Middleware.Add(new ShowTypingMiddleware());

                // Enable the conversation transcript middleware.
                var transcriptStore = new AzureBlobTranscriptStore(blobStorageConfig.ConnectionString, "transcripts");
                options.Middleware.Add(new TranscriptLoggerWorkaroundMiddleware(transcriptStore));

                // Add Teams authentication workaround middleware.
                options.Middleware.Add(new TeamsAuthWorkaroundMiddleware());

                // Catches any errors that occur during a conversation turn and logs them to currently
                // configured ILogger.
                ILogger logger = _loggerFactory.CreateLogger<CarWashBot>();
                options.OnTurnError = async (context, exception) =>
                {
                    var telemetryClient = new TelemetryClient();
                    telemetryClient.TrackException(exception);
                    logger.LogError($"Exception caught : {exception}");
                    await context.SendActivityAsync("Sorry, it looks like something went wrong.");
                };
            });

            // Create and register state accessors.
            // Accessors created here are passed into the IBot-derived class on every turn.
            services.AddSingleton(sp =>
            {
                if (conversationState == null)
                {
                    throw new InvalidOperationException("ConversationState must be defined and added before adding conversation-scoped state accessors.");
                }

                if (userState == null)
                {
                    throw new InvalidOperationException("UserState must be defined and added before adding user-scoped state accessors.");
                }

                // Create the custom state accessor.
                // State accessors enable other components to read and write individual properties of state.
                var accessors = new StateAccessors(conversationState, userState);

                return accessors;
            });
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">Application Builder.</param>
        /// <param name="env">Hosting Environment.</param>
        /// <param name="serviceProvider">Service Provider.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to create logger object for tracing.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;

            // Register proactive message handlers
            serviceProvider.GetService<DropoffReminderMessage>().RegisterHandler();
            serviceProvider.GetService<WashStartedMessage>().RegisterHandler();
            serviceProvider.GetService<WashCompletedMessage>().RegisterHandler();
            serviceProvider.GetService<CarWashCommentLeftMessage>().RegisterHandler();
            serviceProvider.GetService<VehicleArrivedMessage>().RegisterHandler();

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseBotFramework();
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
    }
}
