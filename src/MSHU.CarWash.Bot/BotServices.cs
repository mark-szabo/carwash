﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Configuration;

namespace MSHU.CarWash.Bot
{
    /// <summary>
    /// Represents references to external services.
    ///
    /// For example, LUIS services are kept here as a singleton. This external service is configured
    /// using the <see cref="BotConfiguration"/> class.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    /// <seealso cref="https://www.luis.ai/home"/>
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
                    case ServiceTypes.QnA:
                        {
                            // Create a QnA Maker that is initialized and suitable for passing
                            // into the IBot-derived class (QnABot).
                            var qna = (QnAMakerService)service;

                            if (string.IsNullOrWhiteSpace(qna.KbId))
                            {
                                throw new InvalidOperationException("The QnA KnowledgeBaseId ('kbId') is required to run this sample. Please update your '.bot' file.");
                            }

                            if (string.IsNullOrWhiteSpace(qna.EndpointKey))
                            {
                                throw new InvalidOperationException("The QnA EndpointKey ('endpointKey') is required to run this sample. Please update your '.bot' file.");
                            }

                            if (string.IsNullOrWhiteSpace(qna.Hostname))
                            {
                                throw new InvalidOperationException("The QnA Host ('hostname') is required to run this sample. Please update your '.bot' file.");
                            }

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

                            var app = new LuisApplication(luis.AppId, luis.AuthoringKey, luis.GetEndpoint());
                            var recognizer = new LuisRecognizer(app);
                            LuisServices.Add(luis.Name, recognizer);

                            break;
                        }
                }
            }
        }

        /// <summary>
        /// Gets the set of QnA Maker services used.
        /// Given there can be multiple <see cref="QnAMaker"/> services used in a single bot,
        /// QnA Maker instances are represented as a Dictionary.  This is also modeled in the
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
        /// LuisServices is represented as a dictionary.  This is also modeled in the
        /// ".bot" file since the elements are named.
        /// </summary>
        /// <remarks>The LUIS services collection should not be modified while the bot is running.</remarks>
        /// <value>
        /// A <see cref="LuisRecognizer"/> client instance created based on configuration in the .bot file.
        /// </value>
        public Dictionary<string, LuisRecognizer> LuisServices { get; } = new Dictionary<string, LuisRecognizer>();
    }
}