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

- `CarWash.ClassLibrary` .NET Standard targeting reusable class library
- `CarWash.Functions` Azure functions app for reminder sending
- `CarWash.PWA` ASP.NET Core 2.1 API & React frontent (Progressiove Web App)
- `CarWash.PWA.Windows` PWA wrapper for the Microsoft Store
- `CarWash.Bot` Bot with Microsoft Bot Framework

## Contributors

- Mark Szabo
- Jozsef Vadkerti
- Akos Szego
- Tamas Veiland
- Gabor Kulcsar
- Linda Billinger

## Copyright

Copyright (c) 2018-19 Microsoft. All rights reserved.
