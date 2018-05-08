using System.Collections.Generic;
using System.Threading.Tasks;

namespace Datadog.Trace.Agent
{
    /// <summary>
    /// Defines methods use to write a <see cref="Span"/> collection to the Datadog Agent.
    /// </summary>
    public interface IAgentWriter
    {
        /// <summary>
        /// Writes a <see cref="Span"/> collection to the Datadog Agent.
        /// </summary>
        /// <param name="trace">The spans to write to the Datadog Agent.</param>
        void WriteTrace(List<Span> trace);

        Task FlushAsync();
    }
}
