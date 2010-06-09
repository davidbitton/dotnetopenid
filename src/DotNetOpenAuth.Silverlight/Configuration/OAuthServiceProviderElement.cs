using System;

namespace DotNetOpenAuth.Configuration {
    public class OAuthServiceProviderElement {
        public OAuthServiceProviderSecuritySettingsElement SecuritySettings {
            get { return new OAuthServiceProviderSecuritySettingsElement();}
        }
    }
}