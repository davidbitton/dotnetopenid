using System;

namespace DotNetOpenAuth.Configuration {
    public class OAuthConsumerElement {
        public OAuthConsumerSecuritySettingsElement SecuritySettings {
            get { return new OAuthConsumerSecuritySettingsElement(); }
        }
    }
}