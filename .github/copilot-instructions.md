# CarWash Application Developer Instructions

CarWash is a cross-platform Progressive Web Application (PWA) built with ASP.NET Core 8.0 and React, designed for enterprise car wash service management. The system includes Azure Functions for background processing, comprehensive testing, and cloud deployment capabilities.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Prerequisites and Environment Setup
- .NET 8.0 SDK (version 8.0.x) - already available
- Node.js (version 20.x) and npm (version 10.x) - already available
- Required for building and running the application

### Core Build and Test Workflow
Execute these commands in sequence for a complete build and validation:

1. **Restore NuGet packages:**
   ```bash
   dotnet restore
   ```
   - Time: ~3s (subsequent runs), ~50s (first run)
   - Expected: Package version warnings (NU1608) are NORMAL and can be ignored

2. **Build the entire solution:**
   ```bash
   dotnet build --configuration Release
   ```
   - Time: ~33s - NEVER CANCEL. Set timeout to 120+ seconds.
   - Expected: Many analyzer warnings (CS8032) are NORMAL and can be ignored
   - Must succeed with 0 errors before proceeding

3. **Run unit tests:**
   ```bash
   dotnet test --configuration Release --no-build
   ```
   - Time: ~6s - NEVER CANCEL. Set timeout to 30+ seconds.
   - Expected: 192 tests must pass
   - All tests MUST pass before making changes

4. **Build React frontend (if making frontend changes):**
   ```bash
   cd CarWash.PWA/ClientApp
   npm install
   npm run build
   ```
   - npm install time: ~62s - NEVER CANCEL. Set timeout to 180+ seconds.
   - npm run build time: ~26s - NEVER CANCEL. Set timeout to 60+ seconds.
   - Expected: Some npm deprecation warnings are NORMAL

### Individual Project Builds
For focused development, you can build individual projects:

- **PWA only:** `dotnet build CarWash.PWA --configuration Release`
- **Functions only:** `dotnet build CarWash.Functions --configuration Release`
- **Tests only:** `dotnet build CarWash.PWA.Tests --configuration Release`

### Publishing for Deployment
```bash
dotnet publish CarWash.PWA --configuration Release --output publish/
```
- Time: ~24s - NEVER CANCEL. Set timeout to 60+ seconds.
- This builds both .NET backend and React frontend automatically

## Validation Requirements

### Always Run Before Committing
1. **Full build and test cycle:** Run the core workflow above
2. **Frontend linting (if changing React code):**
   ```bash
   cd CarWash.PWA/ClientApp
   npx eslint src/ --ext .js,.jsx,.ts,.tsx
   ```
   - Time: ~3s

3. **Code formatting check (if changing React code):**
   ```bash
   cd CarWash.PWA/ClientApp
   npx prettier --check "src/**/*.{js,jsx}"
   ```
   - Time: ~1s
   - Expected: May show formatting warnings that should be fixed

### Manual Validation Scenarios
**CRITICAL**: You cannot run the application locally due to Azure cloud dependencies. The PWA requires:
- Azure Key Vault configuration
- Azure App Configuration
- Azure Active Directory setup
- Service Bus and other Azure services

**UI Testing Limitations:**
- React tests fail due to Babel version conflicts (7.20.5 vs 7.22.0+ required)
- Document this as a known limitation: "npm test fails due to Babel version incompatibility"
- Selenium UI tests require a running instance with proper Azure configuration

## Project Structure and Key Components

### Active Projects (in solution)
- **CarWash.PWA** - Main ASP.NET Core 8.0 web application with React frontend
  - Backend API controllers in `/Controllers`
  - React frontend in `/ClientApp` (Create React App)
  - Authentication via Azure AD
  - SignalR hubs for real-time communication

- **CarWash.ClassLibrary** - Shared .NET 8.0 library
  - Entity Framework models
  - Services and utilities
  - Azure service integrations

- **CarWash.Functions** - Azure Functions v4 (.NET 8.0)
  - Background processing and notifications
  - Service Bus integration
  - Timer-triggered functions

- **CarWash.PWA.Tests** - Unit tests (MSTest)
  - Controller tests
  - Service tests
  - 192 test cases total

### Archived/Inactive Projects
- **CarWash.Bot** - Microsoft Bot Framework chatbot (ARCHIVED - not in solution)
- **CarWash.PWA.UiTests** - Selenium UI tests (requires running application)

### Frontend Technology Stack
- React 17.x with Material-UI v5
- Create React App build system
- TypeScript support available
- Progressive Web App features
- Service Worker for offline capabilities

## Common Development Tasks

### Making Backend Changes
1. Run core build workflow first
2. Make changes to `.cs` files in CarWash.PWA or CarWash.ClassLibrary
3. Build and test: `dotnet build && dotnet test`
4. Always ensure 192 tests still pass

### Making Frontend Changes
1. Navigate to: `cd CarWash.PWA/ClientApp`
2. Install dependencies: `npm install` (if package.json changed)
3. Make changes to files in `/src`
4. Build frontend: `npm run build`
5. Lint: `npx eslint src/ --ext .js,.jsx,.ts,.tsx`
6. Format check: `npx prettier --check "src/**/*.{js,jsx}"`
7. Run full solution build to ensure integration works

### Adding New Dependencies
- **.NET packages:** Add to appropriate `.csproj` file, then `dotnet restore`
- **npm packages:** `cd CarWash.PWA/ClientApp && npm install <package>`

## Known Issues and Limitations

### Build Warnings (NORMAL - can be ignored)
- **NU1608 warnings** - Package version constraints (Microsoft.CodeAnalysis)
- **CS8032 warnings** - Analyzer loading issues (many instances)
- **CS0618 warnings** - Obsolete API usage warnings

### Runtime Limitations
- **Cannot run locally** - Requires Azure cloud services configuration
- **React tests fail** - Babel version incompatibility (npm test)
- **Bot project archived** - No longer actively maintained

### CI/CD Integration
- Main workflow: `.github/workflows/mimosonk.yml` deploys to production
- Build includes both .NET and React compilation
- Tests run as part of CI pipeline

## Development Environment Notes

### File Locations Reference
```
/CarWash.PWA/               # Main web application
  /ClientApp/               # React frontend
    /src/                   # React source code
    /package.json           # Frontend dependencies
  /Controllers/             # ASP.NET Core API controllers
  /CarWash.PWA.csproj      # Main project file

/CarWash.ClassLibrary/      # Shared library
/CarWash.Functions/         # Azure Functions
/CarWash.PWA.Tests/         # Unit tests
```

### Configuration Files
- **appsettings.json** - Application configuration (requires Azure services)
- **package.json** - Frontend dependencies and scripts
- **.eslintrc.json** - Frontend linting rules
- **.prettierrc** - Code formatting rules

## Emergency Troubleshooting

### If build fails:
1. Clean: `dotnet clean && cd CarWash.PWA/ClientApp && rm -rf node_modules package-lock.json`
2. Restore: `cd ../../ && dotnet restore && cd CarWash.PWA/ClientApp && npm install`
3. Rebuild: `cd ../../ && dotnet build --configuration Release`

### If tests fail:
1. Check for breaking changes in your code
2. Run tests in isolation: `dotnet test CarWash.PWA.Tests --configuration Release`
3. Review test output for specific failures
4. Only fix tests related to your changes

Always prioritize fixing compilation errors over warnings. The application produces many warnings but must build successfully with 0 errors.