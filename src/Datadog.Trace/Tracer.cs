﻿using Datadog.Trace.Logging;
using OpenTracing;
using OpenTracing.Propagation;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Datadog.Trace
{
    internal class Tracer : ITracer, IDatadogTracer
    {
        private static readonly ILog _log = LogProvider.For<Tracer>();

        private AsyncLocalCompat<TraceContext> _currentContext = new AsyncLocalCompat<TraceContext>("Datadog.Trace.Tracer._currentContext");
        private string _defaultServiceName;
        private Dictionary<string, ServiceInfo> _services = new Dictionary<string, ServiceInfo>();
        private IAgentWriter _agentWriter;

        string IDatadogTracer.DefaultServiceName => _defaultServiceName;

        public Tracer(IAgentWriter agentWriter, List<ServiceInfo> serviceInfo = null, string defaultServiceName = null)
        {
            _agentWriter = agentWriter;
            _defaultServiceName = GetExecutingAssemblyName() ?? Constants.UnkownService;
            if (serviceInfo != null)
            {
                foreach(var service in serviceInfo)
                {
                    _services[service.ServiceName] = service;
                }
            }
            foreach(var service in _services.Values)
            {
                _agentWriter.WriteServiceInfo(service);
            }
        }

        private string GetExecutingAssemblyName()
        {
            try
            {
                var name = Assembly.GetExecutingAssembly().GetName();
                return name.Name;
            }
            catch
            {
                return null;
            }
        }

        public ISpanBuilder BuildSpan(string operationName)
        {
            return new SpanBuilder(this, operationName);
        }

        public ISpanContext Extract<TCarrier>(Format<TCarrier> format, TCarrier carrier)
        {
            _log.Error("Tracer.Extract is not implemented by Datadog.Trace");
            throw new UnsupportedFormatException();
        }

        public void Inject<TCarrier>(ISpanContext spanContext, Format<TCarrier> format, TCarrier carrier)
        {
            _log.Error("Tracer.Extract is not implemented by Datadog.Trace");
            throw new UnsupportedFormatException();
        }

        void IDatadogTracer.Write(List<Span> trace)
        {
            _agentWriter.WriteTrace(trace);
        }

        ITraceContext IDatadogTracer.GetTraceContext()
        {
            if(_currentContext.Get() == null)
            {
                _currentContext.Set(new TraceContext(this));
            }
            return _currentContext.Get();
        }

        void IDatadogTracer.CloseCurrentTraceContext()
        {
            _currentContext.Set(null);
        }
    }
}