using Microsoft.IdentityServer.Web.Authentication.External;
using System;
using System.Security.Claims;

namespace ManualMF
{
    public interface IAuthenticationAdapterExtension
    {
        IAdapterPresentation BeginAuthentication(Claim IdentityClaim, IHttpRequestParams Request, IAuthenticationContext Context);
        IAdapterPresentation TryEndAuthentication(IAuthenticationContext Context, IProofData ProofData, IHttpRequestParams Request, out Claim[] Claims);
        IAdapterPresentation OnError(IHttpRequestParams request, ExternalAuthenticationException ex);
    }
}
