using Microsoft.Graph;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace CarWash.PWA.Services
{
    /// <inheritdoc />
    public class GraphService : IGraphService
    {
        private GraphServiceClient _graphClient;
        private string _accessToken;

        /// <inheritdoc />
        public GraphServiceClient GetAuthenticatedClient()
        {
            _graphClient = new GraphServiceClient(new DelegateAuthenticationProvider(
                request =>
                {
                    // Append the access token to the request
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                    return Task.FromResult(0);
                }));

            return _graphClient;
        }

        /// <inheritdoc />
        public void SaveAccessToken(string accessToken)
        {
            if (accessToken != null) _accessToken = accessToken;
        }
    }
}
