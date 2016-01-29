using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace MSHU.CarWash.UWP.Common
{
    /// <summary>
    /// Manages authentication related scenarios for the application.
    /// </summary>
    public class AuthenticationManager
    {
        // Native client application settings
        private string _clientID = "d79fea3f-2357-4797-9be8-48d630f6e1a3";
        private Uri _redirectUri = new Uri("http://aszegowebapp.webapi.client");
        // Session to Azure AD
        private const string _authority = "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47";
        private AuthenticationContext _authContext = new AuthenticationContext(_authority);

        public static string TokenForUser;

        public const string TenantName = "GraphDir1.onMicrosoft.com";
        public const string TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
        public const string ClientIdForUserAuthn = "66133929-66a4-4edc-aaee-13b04b03207d";
        public const string AuthString = "https://login.microsoftonline.com/" + TenantName;
        public const string ResourceUrl = "https://aszegoappservice.azurewebsites.net";

        /// <summary>
        /// Value indicates if the user has already been authenticated.
        /// </summary>
        public bool IsUserAuthenticated { get; private set; }

        /// <summary>
        /// Gets the UserInfo structure for the current user.
        /// </summary>
        public UserInfo UserData { get; private set; }

        public async Task<bool> LoginWithAAD()
        {
            bool success = false;

            AuthenticationResult result = await _authContext.AcquireTokenAsync(
                ResourceUrl,
                _clientID,
                _redirectUri,
                PromptBehavior.Auto);
            if (result.Status != AuthenticationStatus.Success)
            {
                if (result.Error == "authentication_canceled")
                {
                    // The user cancelled the sign-in, no need to display a message.
                }
                else
                {
                    MessageDialog dialog = new MessageDialog(string.Format("If the error continues, please contact your administrator.\n\nError: {0}\n\nError Description:\n\n{1}", result.Error, result.ErrorDescription), "Sorry, an error occurred while signing you in.");
                    await dialog.ShowAsync();
                }
            }
            else
            {
                IsUserAuthenticated = true;
                UserData = result.UserInfo;
                success = true;
            }

            return success;
        }

        /// <summary>
        /// Signs out the current user.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> SignOutWithAAD()
        {
            AuthenticationResult result = await _authContext.AcquireTokenAsync(
                ResourceUrl,
                _clientID,
                _redirectUri,
                PromptBehavior.Auto);
            // Create an HTTP client and add the token to the Authorization header
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                result.AccessTokenType, result.AccessToken);

            // Call the Web API to get the values
            Uri requestURI = new Uri(new Uri("https://aszegoappservice.azurewebsites.net/"), "api/values");
            Debug.WriteLine("Reading values from '{0}'.", requestURI);
            HttpResponseMessage httpResponse = await httpClient.GetAsync(requestURI);
            Debug.WriteLine("HTTP Status Code: '{0}'", httpResponse.StatusCode.ToString());
            if (httpResponse.IsSuccessStatusCode)
            {
                //
                // Code to do something with the data returned goes here.
                //
            }
            return (httpResponse.IsSuccessStatusCode);

            //bool result = false;
            //_authContext.TokenCache.Clear();
            //string requestUrl = "https://login.windows.net/common/oauth2/logout";
            //HttpResponseMessage msg = await SendMessage(requestUrl);
            //if (msg.IsSuccessStatusCode)
            //{
            //    result = true;
            //}
            //return result;
        }

        private async Task<HttpResponseMessage> SendMessage(string requestUrl)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            var response = await client.SendAsync(request);
            return response;
        }

        /// <summary>
        /// Async task to acquire token for User.
        /// </summary>
        /// <returns>Token for user.</returns>
        public static async Task<string> AcquireTokenAsyncForUser()
        {
            return await GetTokenForUser();
        }

        /// <summary>
        /// Get Token for User.
        /// </summary>
        /// <returns>Token for user.</returns>
        public static async Task<string> GetTokenForUser()
        {
            if (TokenForUser == null)
            {
                var redirectUri = new Uri("https://localhost");
                AuthenticationContext authenticationContext = new AuthenticationContext(AuthString, false);
                AuthenticationResult userAuthnResult = await authenticationContext.AcquireTokenAsync(
                    ResourceUrl,
                    ClientIdForUserAuthn,
                    redirectUri,
                    PromptBehavior.Always);
                TokenForUser = userAuthnResult.AccessToken;
                //Console.WriteLine("\n Welcome " + userAuthnResult.UserInfo.GivenName + " " +
                //                  userAuthnResult.UserInfo.FamilyName);
            }
            return TokenForUser;
        }

        ///// <summary>
        ///// Get Active Directory Client for User.
        ///// </summary>
        ///// <returns>ActiveDirectoryClient for User.</returns>
        //public static ActiveDirectoryClient GetActiveDirectoryClientAsUser()
        //{
        //    Uri servicePointUri = new Uri(Constants.ResourceUrl);
        //    Uri serviceRoot = new Uri(servicePointUri, Constants.TenantId);
        //    ActiveDirectoryClient activeDirectoryClient = new ActiveDirectoryClient(serviceRoot,
        //        async () => await AcquireTokenAsyncForUser());
        //    return activeDirectoryClient;
        //}

    }

}
