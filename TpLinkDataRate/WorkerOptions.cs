using System;

namespace TpLink.Service
{
    /// <summary>Bound from the "Worker" configuration section.</summary>
    public class WorkerOptions
    {
        public const string SectionName = "Worker";

        /// <summary>How often the powerline link rates are polled.</summary>
        public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);
    }
}
