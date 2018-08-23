using System;
using Microsoft.Graph;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using MSHU.CarWash.PWA.Controllers;
using User = MSHU.CarWash.ClassLibrary.User;

namespace MSHU.CarWash.PWA.Services
{
    /// <inheritdoc />
    public class GraphService : IGraphService
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly IMemoryCache _memoryCache;
        private readonly User _user;
        private readonly ClientCredential _credential;
        private readonly string _aadInstance;
        private readonly string _graphResourceId;

        private GraphServiceClient _graphClient;

        /// <inheritdoc />
        public GraphService(IConfiguration configuration, IMemoryCache memoryCache, UsersController usersController)
        {
            _aadInstance = configuration["AzureAD:Instance"];
            _graphResourceId = configuration["AzureAD:GraphResourceId"];
            var clientId = configuration["AzureAD:ClientId"];
            var clientSecret = configuration["AzureAD:ClientSecret"];

            _credential = new ClientCredential(clientId, clientSecret);

            _memoryCache = memoryCache;
            _user = usersController.GetCurrentUser();
            _telemetryClient = new TelemetryClient();
        }

        /// <inheritdoc />
        public GraphServiceClient GetAuthenticatedClient()
        {
            _graphClient = new GraphServiceClient(new DelegateAuthenticationProvider(
                async request =>
                {
                    var accessToken = await GetUserAccessTokenAsync();

                    // Append the access token to the request
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }));

            return _graphClient;
        }

        private async Task<string> GetUserAccessTokenAsync()
        {
            var userIdentifier = new UserIdentifier(_user.Id, UserIdentifierType.UniqueId);
            var userTokenCache = new SessionTokenCache(_user.Id, _memoryCache).GetCacheInstance();

            try
            {
                var authContext = new AuthenticationContext(_aadInstance, userTokenCache);
                var result = await authContext.AcquireTokenSilentAsync(_graphResourceId, _credential, userIdentifier);

                return result.AccessToken;
            }
            catch (Exception e)
            {
                _telemetryClient.TrackException(e);
                return null;
            }
        }
    }
}
