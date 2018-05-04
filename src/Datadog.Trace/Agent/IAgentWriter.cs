using System.Collections.Generic;
using System.Threading.Tasks;

namespace Datadog.Trace.Agent
{
    public interface IAgentWriter
    {
        void WriteTrace(List<Span> trace);

        Task FlushAsync();

        Task FlushAndCloseAsync();
    }
}
