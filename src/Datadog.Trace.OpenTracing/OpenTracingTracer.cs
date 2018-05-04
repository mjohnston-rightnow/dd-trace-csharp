﻿using System;
using System.Collections.Generic;
using Datadog.Trace.Logging;
using OpenTracing;
using OpenTracing.Propagation;

namespace Datadog.Trace.OpenTracing
{
    internal class OpenTracingTracer : ITracer
    {
        private static readonly ILog _log = LogProvider.For<OpenTracingTracer>();

        private readonly Dictionary<string, ICodec> _codecs;

        public OpenTracingTracer()
            : this(new Tracer())
        {
        }

        public OpenTracingTracer(IDatadogTracer datadogTracer)
            : this(datadogTracer, new global::OpenTracing.Util.AsyncLocalScopeManager())
        {
        }

        public OpenTracingTracer(IDatadogTracer datadogTracer, IScopeManager scopeManager)
        {
            DatadogTracer = datadogTracer;
            ServiceName = datadogTracer.DefaultServiceName;
            ScopeManager = scopeManager;
            _codecs = new Dictionary<string, ICodec> { { BuiltinFormats.HttpHeaders.ToString(), new HttpHeadersCodec() } };
        }

        internal IDatadogTracer DatadogTracer { get; }

        public string ServiceName { get; }

        public IScopeManager ScopeManager { get; }

        public ISpan ActiveSpan => ScopeManager.Active?.Span;

        public ISpanBuilder BuildSpan(string operationName)
        {
            return new OpenTracingSpanBuilder(this, operationName);
        }

        public ISpanContext Extract<TCarrier>(IFormat<TCarrier> format, TCarrier carrier)
        {
            _codecs.TryGetValue(format.ToString(), out ICodec codec);
            if (codec != null)
            {
                return codec.Extract(carrier);
            }
            else
            {
                string message = $"Tracer.Extract is not implemented for {format} by Datadog.Trace";
                _log.Error(message);
                throw new NotSupportedException(message);
            }
        }

        public void Inject<TCarrier>(ISpanContext spanContext, IFormat<TCarrier> format, TCarrier carrier)
        {
            _codecs.TryGetValue(format.ToString(), out ICodec codec);
            if (codec != null)
            {
                var ddSpanContext = spanContext as OpenTracingSpanContext;
                if (ddSpanContext == null)
                {
                    throw new ArgumentException("Inject should be called with a Datadog.Trace.SpanContext argument");
                }

                codec.Inject(ddSpanContext, carrier);
            }
            else
            {
                string message = $"Tracer.Inject is not implemented for {format} by Datadog.Trace";
                _log.Error(message);
                throw new NotSupportedException(message);
            }
        }
    }
}