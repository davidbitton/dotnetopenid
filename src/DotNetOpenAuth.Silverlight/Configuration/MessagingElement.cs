using System;

namespace DotNetOpenAuth.Configuration {
    public class MessagingElement {
        public TimeSpan MaximumMessageLifetime {
            get { return TimeSpan.Zero; }
        }

        public bool Strict {
            get { return true; }
        }
    }
}