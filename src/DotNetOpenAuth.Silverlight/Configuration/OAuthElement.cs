using System;

namespace DotNetOpenAuth.Configuration {
    public class OAuthElement {
        public OAuthConsumerElement Consumer {
            get { return new OAuthConsumerElement(); }
        }

        public OAuthServiceProviderElement ServiceProvider {
            get { return new OAuthServiceProviderElement();}
        }
    }
}