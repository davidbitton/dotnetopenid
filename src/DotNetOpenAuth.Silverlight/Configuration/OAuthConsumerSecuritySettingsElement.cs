using System;
using DotNetOpenAuth.OAuth;

namespace DotNetOpenAuth.Configuration {
    public class OAuthConsumerSecuritySettingsElement {
        internal ConsumerSecuritySettings CreateSecuritySettings() {
            return new ConsumerSecuritySettings();
        }
    }
}