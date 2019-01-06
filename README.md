# CarWash app v2.0

[![Build Status](https://dev.azure.com/mark-szabo/carwash/_apis/build/status/CarWash%20CI?branchName=master)](https://dev.azure.com/mark-szabo/carwash/_build/latest?definitionId=2?branchName=master)

## Features

- [x] Cross-platform
- [x] Cross-company (Microsoft & SAP & Graphisoft & Carwash vendor)
- [x] Company SSO for everybody
- [x] Reminders using either push notification or email
- [x] Calendar integration on every device using Microsoft Graph
- [x] Carwash reservation and state tracking
- [x] Company admin reservation management
- [x] Backlog dashboard for Carwash vendor
- [x] Personalization & customization
- [x] Wrapper app in Microsoft Store
- [x] GDPR
- [x] Progressive Web App (PWA)
- [x] 100 score on Lighthouse audit for everything
- [x] Material design
- [x] Push notification (cross-platform, except iOS as Safari is not yet supporting the standardized API)
- [x] Serverless Azure Functions for reminders

## Projects

- `MSHU.CarWash.ClassLibrary` .NET Standard targeting reusable class library
- `MSHU.CarWash.Functions` Azure functions app for reminder sending
- `MSHU.CarWash.PWA` aspnetcore2.0 API & React frontent (Progressiove Web App)
- `MSHU.CarWash.PWA.Windows` PWA wrapper for the Microsoft Store
- `MSHU.CarWash.DAL` [deprecated] Data access layer
- `MSHU.CarWash.DomainModel` [deprecated] Portable class library
- `MSHU.CarWash.EmailJob` [deprecated] Sendgrid email sending webjob
- `MSHU.CarWash.UWP` [deprecated] UWP app
- `MSHU.CarWash.Web` [deprecated] Angular webapp & API
- `CarWash.Azure.Deployment` [deprecated] Azure deployment project

## Contributors

- Mark Szabo
- Jozsef Vadkerti
- Akos Szego
- Tamas Veiland
- Gabor Kulcsar
- Linda Billinger

## Copyright

Copyright (c) 2018 Microsoft. All rights reserved.
