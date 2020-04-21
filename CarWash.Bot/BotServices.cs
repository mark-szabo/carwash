// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

using System;
using System.Collections.Generic;
using Microsoft.Azure.ServiceBus;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Configuration;
using Microsoft.WindowsAzure.Storage;

namespace CarWash.Bot
{
    /// <summary>
    /// Represents references to external services.
    ///
    /// For example, LUIS services are kept here as a singleton. This external service is configured
    /// using the <see cref="BotConfiguration"/> class.
    /// </summary>
    public class BotServices
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BotServices"/> class.
        /// </summary>
        /// <param name="botConfiguration">Parsed .bot configuration file.</param>
        public BotServices(BotConfiguration botConfiguration)
        {
            foreach (var service in botConfiguration.Services)
            {
                switch (service.Type)
                {
                    case ServiceTypes.Endpoint:
                        {
                            var endpoint = (EndpointService)service;
                            EndpointServices.Add(endpoint.Name, endpoint);

                            break;
                        }

                    case ServiceTypes.BlobStorage:
                        {
                            // Create a Storage client.
                            var storage = (BlobStorageService)service;

                            if (string.IsNullOrWhiteSpace(storage.ConnectionString))
                                throw new InvalidOperationException("The Storage ConnectionString ('connectionString') is required. Please update your '.bot' file.");

                            var storageAccount = CloudStorageAccount.Parse(storage.ConnectionString);
                            StorageServices.Add(storage.Name, storageAccount);

                            break;
                        }

                    case ServiceTypes.QnA:
                        {
                            // Create a QnA Maker that is initialized and suitable for passing
                            // into the IBot-derived class (QnABot).
                            var qna = (QnAMakerService)service;

                            if (string.IsNullOrWhiteSpace(qna.KbId))
                                throw new InvalidOperationException("The QnA KnowledgeBaseId ('kbId') is required. Please update your '.bot' file.");

                            if (string.IsNullOrWhiteSpace(qna.EndpointKey))
                                throw new InvalidOperationException("The QnA EndpointKey ('endpointKey') is required. Please update your '.bot' file.");

                            if (string.IsNullOrWhiteSpace(qna.Hostname))
                                throw new InvalidOperationException("The QnA Host ('hostname') is required. Please update your '.bot' file.");

                            var qnaEndpoint = new QnAMakerEndpoint()
                            {
                                KnowledgeBaseId = qna.KbId,
                                EndpointKey = qna.EndpointKey,
                                Host = qna.Hostname,
                            };

                            var qnaMaker = new QnAMaker(qnaEndpoint);
                            QnAServices.Add(qna.Name, qnaMaker);

                            break;
                        }

                    case ServiceTypes.Luis:
                        {
                            var luis = (LuisService)service;

                            if (string.IsNullOrWhiteSpace(luis.AppId))
                                throw new InvalidOperationException("The LUIS AppId ('appId') is required. Please update your '.bot' file.");

                            if (string.IsNullOrWhiteSpace(luis.AuthoringKey))
                                throw new InvalidOperationException("The LUIS AuthoringKey ('authoringKey') is required. Please update your '.bot' file.");

                            if (string.IsNullOrWhiteSpace(luis.Region))
                                throw new InvalidOperationException("The LUIS Region ('region') is required. Please update your '.bot' file.");

                            var app = new LuisApplication(luis.AppId, luis.AuthoringKey, luis.GetEndpoint());
                            var recognizer = new LuisRecognizer(new LuisRecognizerOptionsV3(app));
                            LuisServices.Add(luis.Name, recognizer);

                            break;
                        }

                    case ServiceTypes.Generic:
                        {
                            var genericService = (GenericService)service;
                            if (genericService.Name == "carwashuservicebus")
                            {
                                if (string.IsNullOrWhiteSpace(genericService.Configuration["connectionString"]))
                                    throw new InvalidOperationException("The ServiceBus ConnectionString ('connectionString') is required. Please update your '.bot' file.");

                                var serviceBusConnection = new ServiceBusConnection(genericService.Configuration["connectionString"]);
                                ServiceBusServices.Add(genericService.Name, serviceBusConnection);
                            }

                            break;
                        }
                }
            }
        }

        /// <summary>
        /// Gets the set of QnA Maker services used.
        /// Given there can be multiple <see cref="EndpointService"/> endpoints for a single bot (development/production),
        /// Endpoint instances are represented as a Dictionary.  This is also modeled in the
        /// ".bot" file using named elements.
        /// </summary>
        /// <remarks>The Endpoint services collection should not be modified while the bot is running.</remarks>
        /// <value>
        /// An <see cref="EndpointService"/> instance created based on configuration in the .bot file.
        /// </value>
        public Dictionary<string, EndpointService> EndpointServices { get; } = new Dictionary<string, EndpointService>();

        /// <summary>
        /// Gets the set of Storage services used.
        /// Given there can be multiple <see cref="CloudStorageAccount"/> services used in a single bot,
        /// Storage Account instances are represented as a Dictionary. This is also modeled in the
        /// ".bot" file using named elements.
        /// </summary>
        /// <remarks>The Storage services collection should not be modified while the bot is running.</remarks>
        /// <value>
        /// A <see cref="CloudStorageAccount"/> client instance created based on configuration in the .bot file.
        /// </value>
        public Dictionary<string, CloudStorageAccount> StorageServices { get; } = new Dictionary<string, CloudStorageAccount>();

        /// <summary>
        /// Gets the set of QnA Maker services used.
        /// Given there can be multiple <see cref="QnAMaker"/> services used in a single bot,
        /// QnA Maker instances are represented as a Dictionary. This is also modeled in the
        /// ".bot" file using named elements.
        /// </summary>
        /// <remarks>The QnA Maker services collection should not be modified while the bot is running.</remarks>
        /// <value>
        /// A <see cref="QnAMaker"/> client instance created based on configuration in the .bot file.
        /// </value>
        public Dictionary<string, QnAMaker> QnAServices { get; } = new Dictionary<string, QnAMaker>();

        /// <summary>
        /// Gets the set of LUIS Services used.
        /// Given there can be multiple <see cref="LuisRecognizer"/> services used in a single bot,
        /// LuisServices is represented as a dictionary. This is also modeled in the
        /// ".bot" file since the elements are named.
        /// </summary>
        /// <remarks>The LUIS services collection should not be modified while the bot is running.</remarks>
        /// <value>
        /// A <see cref="LuisRecognizer"/> client instance created based on configuration in the .bot file.
        /// </value>
        public Dictionary<string, LuisRecognizer> LuisServices { get; } = new Dictionary<string, LuisRecognizer>();

        /// <summary>
        /// Gets the set of Service Bus Services used.
        /// Given there can be multiple <see cref="ServiceBusConnection"/> services used in a single bot,
        /// ServiceBusServices is represented as a dictionary. This is also modeled in the
        /// ".bot" file since the elements are named.
        /// </summary>
        /// <remarks>The Service Bus Services collection should not be modified while the bot is running.</remarks>
        /// <value>
        /// A <see cref="ServiceBusConnection"/> client instance created based on configuration in the .bot file.
        /// </value>
        public Dictionary<string, ServiceBusConnection> ServiceBusServices { get; } = new Dictionary<string, ServiceBusConnection>();
    }
}
