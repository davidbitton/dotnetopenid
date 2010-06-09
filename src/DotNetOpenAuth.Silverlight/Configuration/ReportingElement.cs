using System;

namespace DotNetOpenAuth.Configuration {
    public class ReportingElement {
        public bool IncludeEventStatistics {
            get { return true; }
        }

        public bool IncludeFeatureUsage {
            get { return true; }
        }

        public bool IncludeCultures {
            get { return true; }
        }

        public bool IncludeLocalRequestUris {
            get { return true; }
        }

        public TimeSpan MinimumReportingInterval {
            get { return TimeSpan.Zero; }
        }

        public TimeSpan MinimumFlushInterval {
            get { return TimeSpan.Zero; }
        }

        public bool Enabled {
            get { return true; }
        }
    }
}