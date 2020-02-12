using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityServer.Web.Authentication.External;
using System.Globalization;


namespace ManualMF
{
    class ManualMFMetadata : IAuthenticationAdapterMetadata
    {
        public const String AUTH_METHOD = "urn:manualmf:operatorassistedauth";
        public string AdminName
        {
            get { return "Operator-assisted MFA adapter"; }
        }

        public string[] AuthenticationMethods
        {
            get { return new[] { AUTH_METHOD }; }
        }

        public int[] AvailableLcids
        {
            get { return new[] { new CultureInfo("en-us").LCID }; }
        }

        public Dictionary<int, string> Descriptions
        {
            get
            {
                Dictionary<int, string> result = new Dictionary<int, string>();
                result.Add(new CultureInfo("en-us").LCID, "Performs multi-factor authentication via manual permissive action from human operator.");
                return result;
            }
        }

        public Dictionary<int, string> FriendlyNames
        {
            get {
                Dictionary<int, string> result = new Dictionary<int, string>();
                result.Add(new CultureInfo("en-us").LCID, "Operator-assisted MFA Adapter");
                return result;
            }
        }

        public string[] IdentityClaims
        {
            get { return new[] { "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn" }; }
        }

        public bool RequiresIdentity
        {
            get { return true; }
        }
    }
}
