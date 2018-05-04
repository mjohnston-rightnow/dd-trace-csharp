using System;
using System.Collections.Generic;

namespace Datadog.Trace
{
    internal interface IDatadogTracer
    {
        string DefaultServiceName { get; }

        Span StartSpan(string operationName, SpanContext parentContext, string serviceName, DateTimeOffset? start, bool ignoreActiveSpan);

        void Write(List<Span> span);
    }
}