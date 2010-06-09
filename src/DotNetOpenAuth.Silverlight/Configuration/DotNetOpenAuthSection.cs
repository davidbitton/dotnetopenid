using System;

namespace DotNetOpenAuth.Configuration {
    public class DotNetOpenAuthSection {
        public static DotNetOpenAuthSection Configuration { get; private set; }

        public ReportingElement Reporting {
            get { return new ReportingElement(); }
        }

        public OAuthElement OAuth {
            get { return new OAuthElement();}
        }

        public MessagingElement Messaging {
            get { return new MessagingElement();}
        }
    }
}
