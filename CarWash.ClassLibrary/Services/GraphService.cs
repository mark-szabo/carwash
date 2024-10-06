using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Collections.Generic;
using System;
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

        /// <summary>
        /// Defines a contract for obtaining access tokens for a given url.
        /// </summary>
        /// <remarks>
        /// Defines a contract for obtaining access tokens for a given url.
        /// </remarks>
        public class TokenProvider(string token) : IAccessTokenProvider
        {
            /// <summary>
            ///     This method is called by the <see cref="BaseBearerTokenAuthenticationProvider" /> class to get the access token.
            /// </summary>
            /// <param name="uri">The target URI to get an access token for.</param>
            /// <param name="additionalAuthenticationContext">Additional authentication context to pass to the authentication library.</param>
            /// <param name="cancellationToken">The cancellation token for the task</param>
            /// <returns>A Task that holds the access token to use for the request.</returns>
            public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object> additionalAuthenticationContext = default,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(token);
            }

            /// <summary>
            /// Returns the <see cref="AllowedHostsValidator"/> for the provider.
            /// </summary>
            public AllowedHostsValidator AllowedHostsValidator { get; }
        }
    }
}
