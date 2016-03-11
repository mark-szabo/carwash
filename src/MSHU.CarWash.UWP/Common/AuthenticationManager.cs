using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure.MobileServices;
using MSHU.CarWash.DomainModel;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Linq;

namespace MSHU.CarWash.UWP.Common
{
    /// <summary>
    /// Manages authentication related scenarios for the application.
    /// </summary>
    public class AuthenticationManager
    {
        //vadkertimobiletestnativeapp's properies in Azure AD
        private string m_ClientId = "1d316939-3200-4b05-9072-a5c92ae8c5a0";
        private Uri m_AppUri = new Uri("ms-app://s-1-15-2-348789351-3529148773-2918319933-3807175127-3638082815-3054471230-807679675/");
       
        //Session to Azure AD
        private const string s_TenantId = "microsoft.onmicrosoft.com";
        private const string s_Authority = "https://login.microsoftonline.com/"+ s_TenantId;
        private AuthenticationContext m_AuthContext = new AuthenticationContext(s_Authority);

       
        /// <summary>
        /// Value indicates if the user has already been authenticated.
        /// </summary>
        public bool IsUserAuthenticated { get; private set; }

        /// <summary>
        /// Gets the UserInfo structure for the current user.
        /// </summary>
        public UserInfo UserData { get; private set; }

        /// <summary>
        /// Gets the access token for further HttpRequest 
        /// </summary>
        public string BearerAccessToken { get; private set; }

        /// <summary>
        /// Gets the Employee instance that represents the currently signed-in user.
        /// </summary>
        public Employee CurrentEmployee { get; private set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public AuthenticationManager()
        {
            // To determine this application's redirect URI, which is necessary when registering the app
            // in AAD, set a breakpoint on the next line, run the app, and copy the string value of the URI.
            // This is the only purposes of this line of code, it has no functional purpose in the application.
            this.m_AppUri = 
                Windows.Security.Authentication.Web.WebAuthenticationBroker.GetCurrentApplicationCallbackUri();

        }


        /// <summary>
        /// Login via Azure Active Directory
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <returns></returns>
        public async Task<bool> LoginWithAAD()
        {
            AuthenticationResult result = await TrySignInWithAadAsync(PromptBehavior.Auto);

            if (result.Status != AuthenticationStatus.Success)
            {
                if (result.Error == "authentication_canceled")
                {
                    // The user cancelled the sign-in, no need to display a message.
                }
                else
                {
                    //pop up the error message
                    Windows.UI.Popups.MessageDialog dialog =
                        new Windows.UI.Popups.MessageDialog(string.Format("If the error continues, please contact your administrator.\n\nError: {0}\n\nError Description:\n\n{1}", result.Error, result.ErrorDescription), "Sorry, an error occurred while signing you in.");
                    await dialog.ShowAsync();
                }

                return false;
            }

            return true;
        }

        private async Task<AuthenticationResult> TrySignInWithAadAsync(PromptBehavior promptBehavior)
        {
            var result = await m_AuthContext.AcquireTokenAsync(
                "https://vadkertitestwebapp.azurewebsites.net",
                m_ClientId,
                m_AppUri,
                promptBehavior);

            if (result.Status == AuthenticationStatus.Success)
            {
                //successful sign in
                IsUserAuthenticated = true;
                UserData = result.UserInfo;
                BearerAccessToken = result.AccessToken;
                // Get the Employee instance assigned to the current user.
                CurrentEmployee = await ServiceClient.ServiceClient.GetCurrentUser(BearerAccessToken);
            }

            return result;
        }

        public async Task<bool> TryAutoSignInWithAadAsync()
        {
            var result = await TrySignInWithAadAsync(PromptBehavior.Never);
            return result.Status == AuthenticationStatus.Success;
        }

        /// <summary>
        /// Signs out the current user.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> SignOutWithAAD()
        {
            bool result = false;
            m_AuthContext.TokenCache.Clear();
            string requestUrl = "https://login.windows.net/common/oauth2/logout";
            HttpResponseMessage msg = await SendMessage(requestUrl);
            if (msg.IsSuccessStatusCode)
            {
                result = true;
            }
            return result;
        }

        private async Task<HttpResponseMessage> SendMessage(string requestUrl)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            var response = await client.SendAsync(request);
            return response;
        }

        /// <summary>
        /// Retrieves the current Employee instance from the service, again.
        /// </summary>
        /// <returns></returns>
        public async Task RefreshCurrentUser()
        {
            // Get the Employee instance assigned to the current user.
            CurrentEmployee = await ServiceClient.ServiceClient.GetCurrentUser(BearerAccessToken);
        }
    }

}
