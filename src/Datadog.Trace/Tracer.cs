using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.Agent;
using Datadog.Trace.Logging;

namespace Datadog.Trace
{
    /// <summary>
    /// The tracer is responsible for creating spans and flushing them to the Datadog agent
    /// </summary>
    public class Tracer : IDatadogTracer
    {
        private const string UnknownServiceName = "UnknownService";
        private static readonly ILog _log = LogProvider.For<Tracer>();

        private static Lazy<Tracer> _defaultInstance;
        private static Tracer _instance;

        // TODO: IScopeManager
        private AsyncLocalScopeManager _scopeManager;

        private string _defaultServiceName;
        private IAgentWriter _agentWriter;

        static Tracer()
        {
            _defaultInstance = new Lazy<Tracer>(LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tracer"/> class with default settings.
        /// </summary>
        public Tracer()
            : this(DefaultAgentUri)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tracer" /> class with the
        /// specified Datadog Agent URI.
        /// </summary>
        /// <param name="uri">The Datadog Agent URI.</param>
        public Tracer(string uri)
            : this(uri, CreateDefaultServiceName())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tracer" /> class with the
        /// specified Datadog Agent URI and default service name.
        /// </summary>
        /// <param name="uri">The Datadog Agent URI.</param>
        /// <param name="defaultServiceName">The default service name.</param>
        public Tracer(string uri, string defaultServiceName)
            : this(new AgentWriter(new Api(new Uri(uri))), defaultServiceName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tracer" /> class with the
        /// specified <see cref="IAgentWriter" />.
        /// </summary>
        /// <param name="agentWriter">The writer used to send data to the Datadog Agent.</param>
        public Tracer(IAgentWriter agentWriter)
            : this(agentWriter, CreateDefaultServiceName())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tracer" /> class with the
        /// specified <see cref="IAgentWriter" /> and default service name.
        /// </summary>
        /// <param name="agentWriter">The writer used to send data to the Datadog Agent.</param>
        /// <param name="defaultServiceName">The default service name.</param>
        public Tracer(IAgentWriter agentWriter, string defaultServiceName)
        {
            _agentWriter = agentWriter;
            _defaultServiceName = defaultServiceName;
            _scopeManager = new AsyncLocalScopeManager();
        }

        /// <summary>
        /// Gets the default Datadog Agent URI.
        /// </summary>
        public static string DefaultAgentUri => "http://localhost:8126";

        /// <summary>
        /// Gets the global tracer object
        /// </summary>
        public static Tracer Instance => _instance ?? _defaultInstance.Value;

        /// <summary>
        /// Gets the active scope
        /// </summary>
        public Scope ActiveScope => _scopeManager.Active;

        /// <summary>
        /// Gets the default service name for traces where a service name is not specified.
        /// </summary>
        string IDatadogTracer.DefaultServiceName => _defaultServiceName;

        /// <summary>
        /// Registers the specified <see cref="Tracer" /> instance as the global instance
        /// that can be accessed through <see cref="Tracer.Instance"/>.
        /// </summary>
        /// <param name="tracer">The tracer instance to be registered as the global instance..</param>
        public static void RegisterInstance(Tracer tracer)
        {
            _instance = tracer;
        }

        /// <summary>
        /// Writes the specified <see cref="Span"/> collection to the agent writer.
        /// </summary>
        /// <param name="trace">The <see cref="Span"/> collection to write.</param>
        void IDatadogTracer.Write(List<Span> trace)
        {
            _agentWriter.WriteTrace(trace);
        }

        /// <summary>
        /// Make a span active and return a scope that can be disposed to desactivate the span
        /// </summary>
        /// <param name="span">The span to activate</param>
        /// <param name="finishOnClose">If set to false, closing the returned scope will not close the enclosed span </param>
        /// <returns>A Scope object wrapping this span</returns>
        public Scope ActivateSpan(Span span, bool finishOnClose = true)
        {
            return _scopeManager.Activate(span, finishOnClose);
        }

        /// <summary>
        /// This is a shortcut for <see cref="StartSpan"/> and <see cref="ActivateSpan"/>, it creates a new span with the given parameters and makes it active.
        /// </summary>
        /// <param name="operationName">The span's operation name</param>
        /// <param name="childOf">The span's parent</param>
        /// <param name="serviceName">The span's service name</param>
        /// <param name="startTime">An explicit start time for that span</param>
        /// <param name="ignoreActiveScope">If set the span will not be a child of the currently active span</param>
        /// <param name="finishOnClose">If set to false, closing the returned scope will not close the enclosed span </param>
        /// <returns>A scope wrapping the newly created span</returns>
        public Scope StartActive(string operationName, SpanContext childOf = null, string serviceName = null, DateTimeOffset? startTime = null, bool ignoreActiveScope = false, bool finishOnClose = true)
        {
            var span = StartSpan(operationName, childOf, serviceName, startTime, ignoreActiveScope);
            return _scopeManager.Activate(span, finishOnClose);
        }

        /// <summary>
        /// This create a Span with the given parameters
        /// </summary>
        /// <param name="operationName">The span's operation name</param>
        /// <param name="childOf">The span's parent</param>
        /// <param name="serviceName">The span's service name</param>
        /// <param name="startTime">An explicit start time for that span</param>
        /// <param name="ignoreActiveScope">If set the span will not be a child of the currently active span</param>
        /// <returns>The newly created span</returns>
        public Span StartSpan(string operationName, SpanContext childOf = null, string serviceName = null, DateTimeOffset? startTime = null, bool ignoreActiveScope = false)
        {
            if (childOf == null && !ignoreActiveScope)
            {
                childOf = _scopeManager.Active?.Span?.Context;
            }

            var span = new Span(this, childOf, operationName, serviceName, startTime);
            span.TraceContext.AddSpan(span);
            return span;
        }

        /// <summary>
        /// Sends any pending spans to the Datadog Agent asynchronously.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous flush operation.</returns>
        public async Task FlushTracesAsync()
        {
            await _agentWriter.FlushAsync().ConfigureAwait(false);
        }

        private static string CreateDefaultServiceName()
        {
            return Assembly.GetEntryAssembly()?.GetName().Name ??
                   Process.GetCurrentProcess().ProcessName;
        }
    }
}
