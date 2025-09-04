using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CarWash.PWA.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [AllowAnonymous]
    public class AuthController(IConfiguration configuration) : ControllerBase
    {

        [HttpGet("google-login")]
        public async Task<IActionResult> GoogleLoginAsync([FromQuery] string code)
        {
            var fileDataStore = new FileDataStore("Google");
            var userId = "user"; // in real app it should be unique for each user
            try
            {
                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = configuration["Authentication:Google:ClientId"],
                        ClientSecret = configuration["Authentication:Google:ClientSecret"],
                    },
                    //Scopes = ["email"],
                    DataStore = fileDataStore,
                });

                var tokenResponse = await fileDataStore.GetAsync<TokenResponse>(userId);

                tokenResponse ??= await flow.ExchangeCodeForTokenAsync(
                        userId,
                        code,
                        "https://localhost:51145/api/auth/google-login",
                        CancellationToken.None);

                var userCredential = new UserCredential(flow, userId, tokenResponse);

                return Ok(userCredential.Token.IdToken);
            }
            catch (Exception exception)
            {
                throw;
            }
        }
    }
}
