﻿using Cdm.Authentication.OAuth2;

namespace Cdm.Authentication.Clients
{
    public class MockServerAuth : AuthorizationCodeFlow
    {
        public const string AuthorizationPath = "/api/oauth/authorize/";
        public const string TokenPath = "/api/oauth/token/oidc/";
        
        public override string authorizationUrl => $"{serverUrl}{AuthorizationPath}";
        public override string accessTokenUrl => $"{serverUrl}{TokenPath}";
        
        public string serverUrl { get; }
        
        public MockServerAuth(Configuration configuration, string serverUrl) : base(configuration)
        {
            this.serverUrl = serverUrl;
        }
    }
}