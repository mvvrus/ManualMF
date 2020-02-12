using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityServer.Web.Authentication.External;
using System.Security.Claims;

namespace ManualMF
{
    public class ManualMFAdapter: IAuthenticationAdapter
    {

        public IAdapterPresentation BeginAuthentication(System.Security.Claims.Claim identityClaim, System.Net.HttpListenerRequest request, IAuthenticationContext context)
        {
            //TODO Perform required initialization
            return new ManualMFPresentation();
        }

        public bool IsAvailableForUser(System.Security.Claims.Claim identityClaim, IAuthenticationContext context)
        {
            //Currently, all users are allowed for authentication
            return true;
            //TODO Decide wether to implement user segregation
        }

        public IAuthenticationAdapterMetadata Metadata
        {
            get { return new ManualMFMetadata(); }
        }

        public void OnAuthenticationPipelineLoad(IAuthenticationMethodConfigData configData)
        {
            //Currently, we have nothing to do here
            //TODO Perform required adapter initialization 
        }

        public void OnAuthenticationPipelineUnload()
        {
            //Currently, we have nothing to do here
            //TODO Perform required adapter resources de-allocation
        }

        public IAdapterPresentation OnError(System.Net.HttpListenerRequest request, ExternalAuthenticationException ex)
        {
            //Just retry with the same form for now
            return new ManualMFPresentation();
        }

        public IAdapterPresentation TryEndAuthentication(IAuthenticationContext context, IProofData proofData, System.Net.HttpListenerRequest request, out Claim[] claims)
        {
            //Just for now - simply allow all
            claims = new[] { new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/authenticationmethod", ManualMFMetadata.AUTH_METHOD) };

            return null;
            //TODO Really perform authentication
        }
    }
}
