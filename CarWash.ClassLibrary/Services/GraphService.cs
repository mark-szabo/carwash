using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Collections.Generic;
using System;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace CarWash.ClassLibrary.Services
{
    /// <inheritdoc />
    public class GraphService : IGraphService
    {
        private GraphServiceClient _graphClient;
        private string _accessToken;

        /// <inheritdoc />
        public GraphServiceClient GetAuthenticatedClient()
        {
            _graphClient = new GraphServiceClient(new BaseBearerTokenAuthenticationProvider(new TokenProvider(_accessToken)));

            return _graphClient;
        }

        /// <inheritdoc />
        public void SaveAccessToken(string accessToken)
        {
            if (accessToken != null) _accessToken = accessToken;
        }

        public class TokenProvider : IAccessTokenProvider
        {
            private string _token;

            public TokenProvider(string token)
            {
                _token = token;
            }

            public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object> additionalAuthenticationContext = default,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_token);
            }

            public AllowedHostsValidator AllowedHostsValidator { get; }
        }
    }
}
