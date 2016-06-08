using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure.MobileServices;
using MSHU.CarWash.DomainModel;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Linq;
using Windows.Security.Credentials;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Authentication.Web;

namespace MSHU.CarWash.UWP.Common
{
    /// <summary>
    /// Manages authentication related scenarios for the application.
    /// </summary>
    public class AuthenticationManager
    {
        //mshucarwashuniversalapp's properies in Azure AD
        private string m_ClientId = "c9349a86-5ab0-45db-a0d3-2f4a1ca89a1d";
        private Uri m_AppUri = new Uri("ms-app://s-1-15-2-348789351-3529148773-2918319933-3807175127-3638082815-3054471230-807679675/");
       
        //Session to Azure AD
        private const string s_TenantId = "microsoft.onmicrosoft.com";
        private const string s_Authority = "https://login.microsoftonline.com/"+ s_TenantId;
        public const string s_BackendAddress = "https://mshucarwash.azurewebsites.net";
        private AuthenticationContext m_AuthContext = new AuthenticationContext(s_Authority);

       
        /// <summary>
        /// Value indicates if the user has already been authenticated.
        /// </summary>
        public bool IsUserAuthenticated { get; private set; }

        /// <summary>
        /// Gets the UserInfo structure for the current user.
        /// </summary>
        public MSHU.CarWash.UWP.ServiceClient.UserInfo UserData { get; private set; }

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
            string brokerUri = string.Format("ms-appx-web://Microsoft.AAD.BrokerPlugIn/{0}", Windows.Security.Authentication.Web.WebAuthenticationBroker.GetCurrentApplicationCallbackUri().Host.ToUpper());

        }


        /// <summary>
        /// Login via Azure Active Directory
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <returns></returns>
        public async Task<bool> LoginWithAAD()
        {
            bool result = await TrySignInWithAadAsync(PromptBehavior.Always);
            return result;
        }

        private async Task<bool> TrySignInWithAadAsync(PromptBehavior promptBehavior)
        {
            bool result = false;
            result = await TrySignInWithWebBrokerAsync(promptBehavior);
            if (result == false)
            {
                result = await TrySignInWithADALAsync(promptBehavior);
            }
            return result;
        }

        /// <summary>
        /// Try to sign in with WebBroker (Windows 10 mode)
        /// </summary>
        /// <param name="promptBehavior"></param>
        /// <returns></returns>
        private async Task<bool> TrySignInWithWebBrokerAsync(PromptBehavior promptBehavior)
        {
            bool result = false;

            WebAccountProvider wap = await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.microsoft.com", s_Authority);
            if(wap == null)
            {
                return false;
            }

            WebTokenRequest wtr = new WebTokenRequest(wap, string.Empty, m_ClientId);
            wtr.Properties.Add("resource", s_BackendAddress);

            WebTokenRequestResult wtrr = null;

            if (promptBehavior == PromptBehavior.Never)
            {
                wtrr = await WebAuthenticationCoreManager.GetTokenSilentlyAsync(wtr);
            }
            else
            {
                wtrr = await WebAuthenticationCoreManager.RequestTokenAsync(wtr);
            }

            if (wtrr.ResponseStatus == WebTokenRequestStatus.Success)
            {
                string accessToken = wtrr.ResponseData[0].Token;
                var account = wtrr.ResponseData[0].WebAccount;
                var properties = wtrr.ResponseData[0].Properties;

                ServiceClient.UserInfo info = new ServiceClient.UserInfo();
                info.DisplayableId = account.UserName;
                //[0]: "UPN"
                //[1]: "DisplayName"
                //[2]: "TenantId"
                //[3]: "FirstName"
                //[4]: "OID"
                //[5]: "Authority"
                //[6]: "SignInName"
                //[7]: "UserName"
                //[8]: "UID"
                //[9]: "LastName"
                info.FamilyName = account.Properties["LastName"];
                info.GivenName = account.Properties["FirstName"];
                UserData = info;
                BearerAccessToken = accessToken;
                // Get the Employee instance assigned to the current user.
                try
                {
                    CurrentEmployee = await ServiceClient.ServiceClient.GetCurrentUser(BearerAccessToken);
                    result = true;
                }
                catch (HttpRequestException)
                {
                    result = false;
                }
            }
            
            return result;
        }

        /// <summary>
        /// Try to sign in with Active Directory Authentication Library
        /// </summary>
        /// <param name="promptBehavior"></param>
        /// <returns></returns>
        private async Task<bool> TrySignInWithADALAsync(PromptBehavior promptBehavior)
        {
            bool result = false;
            var authenticationResult = await m_AuthContext.AcquireTokenAsync(
                                s_BackendAddress,
                                m_ClientId,
                                m_AppUri,
                                promptBehavior);

            if (authenticationResult.Status == AuthenticationStatus.Success)
            {
                //successful sign in
                IsUserAuthenticated = true;
                ServiceClient.UserInfo info = new ServiceClient.UserInfo();
                info.DisplayableId = authenticationResult.UserInfo.DisplayableId;
                info.FamilyName = authenticationResult.UserInfo.DisplayableId;
                info.GivenName = authenticationResult.UserInfo.DisplayableId;
                UserData = info;
                BearerAccessToken = authenticationResult.AccessToken;
                // Get the Employee instance assigned to the current user.
                CurrentEmployee = await ServiceClient.ServiceClient.GetCurrentUser(BearerAccessToken);
                result = true;
            }
            if (authenticationResult.Status != AuthenticationStatus.Success)
            {
                if (authenticationResult.Error == "authentication_canceled" || 
                    (authenticationResult.Error == "user_interaction_required" && promptBehavior == PromptBehavior.Never) ||
                    (authenticationResult.Error.Contains("interaction_required") && promptBehavior == PromptBehavior.Never))
                {
                    // The user cancelled the sign-in or we couldn't perform auto sign-in - no need to display a message.
                }
                else
                {
                    //pop up the error message
                    Windows.UI.Popups.MessageDialog dialog =
                        new Windows.UI.Popups.MessageDialog(
                            string.Format("If the error continues, please contact your administrator.\n\nError: {0}\n\nError Description:\n\n{1}", authenticationResult.Error, authenticationResult.ErrorDescription), "Sorry, an error occurred while signing you in.");
                    await dialog.ShowAsync();
                }
                result = false;
            }

            return result;
        }

        public async Task<bool> TryAutoSignInWithAadAsync()
        {
            try
            {
                var result = await TrySignInWithAadAsync(PromptBehavior.Never);
                return result;
            }
            catch (HttpRequestException)
            {
                return false;
            }
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

            try
            {
                var authResult = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, new Uri(requestUrl));
                if(authResult.ResponseStatus == WebAuthenticationStatus.UserCancel)
                {
                    result = true;
                }
            }
            catch (Exception)
            {
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
