using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Threading;
using System.Threading.Tasks;
using ApiService.Services;

namespace ApiService.Filters
{
    public class ApiKeyAuthorizeAttribute : FilterAttribute, IAuthorizationFilter
    {
        private const string ApiKeyHeaderNameUsername = "Username";
        private const string ApiKeyHeaderNamePassword = "Password";
        private const string ApiKeyHeaderNameApiKey = "ApiKey";

        /// <summary>
        /// Async authorization so the P_Verify_Key round trip does not block an ASP.NET
        /// thread-pool thread.
        ///
        /// Implements IAuthorizationFilter directly rather than deriving from
        /// AuthorizationFilterAttribute. On that base class ExecuteAuthorizationFilterAsync
        /// is an EXPLICIT interface implementation, so it cannot be overridden (CS0115).
        /// Implementing the interface here means there is no override to get wrong, and it
        /// is the same entry point Web API calls either way.
        ///
        /// Contract to reproduce: return a response to short-circuit the pipeline, or
        /// await continuation() to let the request proceed to the action.
        ///
        /// NOTE: txtResult used to be an instance field. Web API caches one filter
        /// instance per action and reuses it across all concurrent requests, so that
        /// field was shared mutable state - two simultaneous failing requests could read
        /// each other's error message. It is a local now.
        /// </summary>
        public async Task<HttpResponseMessage> ExecuteAuthorizationFilterAsync(
            HttpActionContext actionContext,
            CancellationToken cancellationToken,
            Func<Task<HttpResponseMessage>> continuation) {
            string txtResult = "";
            bool authorized = false;
            var headers = actionContext.Request.Headers;

            // Captured up front so the code does not depend on HttpContext.Current being
            // restored after the await (which needs httpRuntime targetFramework >= 4.5,
            // and Web.config is not in the repo).
            var httpContext = HttpContext.Current;
            //checy username
            if (headers.Contains(ApiKeyHeaderNameUsername)) {
                //check password
                if (headers.Contains(ApiKeyHeaderNamePassword)) {
                    //check apiKey
                    if (headers.Contains(ApiKeyHeaderNameApiKey)) {
                        var apiKeyHeaderUsernameValue = headers.GetValues(ApiKeyHeaderNameUsername).FirstOrDefault();
                        var apiKeyHeaderPasswordValue = headers.GetValues(ApiKeyHeaderNamePassword).FirstOrDefault();
                        var apiKeyHeaderApiValue = headers.GetValues(ApiKeyHeaderNameApiKey).FirstOrDefault();
                        // No ConfigureAwait(false) here on purpose: the continuation runs the
                        // action, and downstream code reads HttpContext.Current. Resuming on
                        // the ASP.NET context keeps that intact.
                        var verifyApiKey = await new VerifyApiKey()
                            .CheckApiKeyAsync(apiKeyHeaderUsernameValue, apiKeyHeaderPasswordValue, apiKeyHeaderApiValue);
                        //api pass
                        if (verifyApiKey == "Y") {
                            if (httpContext != null) {
                                httpContext.Items["HeaderUserLogin"] = apiKeyHeaderUsernameValue;
                            }
                            authorized = true;
                        }
                        else  //api not pass
                        {
                            txtResult = verifyApiKey;
                        }
                    }
                    else {
                        txtResult = "Required Header ApiKey";
                    }
                }
                else {
                    txtResult = "Required Header Password";
                }
            }
            else {
                txtResult = "Required Header Username";
            }
            if (!authorized && txtResult != "") {
                var dataResult = new {
                    statusCode = HttpStatusCode.Forbidden,
                    errorMessage = txtResult,
                    result = ""
                };
                // Set Response as well as returning it, so anything inspecting the
                // action context sees the same short-circuit the base class produced.
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Forbidden, dataResult);
                return actionContext.Response;
            }

            return await continuation();
        }
    }
}