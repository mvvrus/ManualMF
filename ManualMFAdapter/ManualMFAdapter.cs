using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityServer.Web.Authentication.External;

namespace ManualMFAdapter
{
    public class ManualMFAdapter: IAuthenticationAdapter
    {
        public IAdapterPresentation BeginAuthentication(System.Security.Claims.Claim identityClaim, System.Net.HttpListenerRequest request, IAuthenticationContext context)
        {
            //TODO
            throw new NotImplementedException();
        }

        public bool IsAvailableForUser(System.Security.Claims.Claim identityClaim, IAuthenticationContext context)
        {
            //TODO
            throw new NotImplementedException();
        }

        public IAuthenticationAdapterMetadata Metadata
        {
            //TODO
            get { throw new NotImplementedException(); }
        }

        public void OnAuthenticationPipelineLoad(IAuthenticationMethodConfigData configData)
        {
            //TODO
            throw new NotImplementedException();
        }

        public void OnAuthenticationPipelineUnload()
        {
            //TODO
            throw new NotImplementedException();
        }

        public IAdapterPresentation OnError(System.Net.HttpListenerRequest request, ExternalAuthenticationException ex)
        {
            //TODO
            throw new NotImplementedException();
        }

        public IAdapterPresentation TryEndAuthentication(IAuthenticationContext context, IProofData proofData, System.Net.HttpListenerRequest request, out System.Security.Claims.Claim[] claims)
        {
            //TODO
            throw new NotImplementedException();
        }
    }
}
