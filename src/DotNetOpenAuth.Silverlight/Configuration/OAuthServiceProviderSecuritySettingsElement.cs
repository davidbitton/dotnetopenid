using System;

namespace DotNetOpenAuth.Configuration {
    public class OAuthServiceProviderSecuritySettingsElement {
        public TimeSpan MaximumRequestTokenTimeToLive {
            get { return TimeSpan.MaxValue; }
        }
    }
}