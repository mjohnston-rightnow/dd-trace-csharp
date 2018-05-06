using System;
using System.Collections.Generic;
using System.Globalization;
using Datadog.Trace.Logging;
using OpenTracing;

namespace Datadog.Trace.OpenTracing
{
    internal class OpenTracingSpanBuilder : ISpanBuilder
    {
        private static ILog _log = LogProvider.For<OpenTracingSpanBuilder>();

        private readonly OpenTracingTracer _tracer;
        private readonly object _lock = new object();
        private readonly string _operationName;
        private OpenTracingSpanContext _parent;
        private DateTimeOffset? _start;
        private Dictionary<string, string> _tags;
        private string _serviceName;
        private bool _ignoreActiveSpan;
        private string _resourceName;
        private bool? _error;
        private string _type;

        internal OpenTracingSpanBuilder(OpenTracingTracer tracer, string operationName)
        {
            _tracer = tracer;
            _operationName = operationName;
        }

        public ISpanBuilder AddReference(string referenceType, ISpanContext referencedContext)
        {
            lock (_lock)
            {
                if (referenceType == References.ChildOf)
                {
                    _parent = (OpenTracingSpanContext)referencedContext;
                    return this;
                }
            }

            _log.Debug("ISpanBuilder.AddReference is not implemented for other references than ChildOf by Datadog.Trace");
            return this;
        }

        public ISpanBuilder AsChildOf(ISpan parent)
        {
            lock (_lock)
            {
                _parent = (OpenTracingSpanContext)parent.Context;
                return this;
            }
        }

        public ISpanBuilder AsChildOf(ISpanContext parent)
        {
            lock (_lock)
            {
                _parent = (OpenTracingSpanContext)parent;
                return this;
            }
        }

        public ISpanBuilder IgnoreActiveSpan()
        {
            _ignoreActiveSpan = true;
            return this;
        }

        public ISpan Start()
        {
            lock (_lock)
            {
                Span parent = GetParent();

                // if service name was not set, inherit parent's service name;
                // if there is no parent, fallback to default service name
                string serviceName = _serviceName ?? parent?.ServiceName ?? _tracer.DefaultServiceName;

                Span span = _tracer.DatadogTracer.StartSpan(_operationName, parent?.Context, serviceName, _start, _ignoreActiveSpan);

                if (_resourceName != null)
                {
                    span.ResourceName = _resourceName;
                }

                if (_error != null)
                {
                    span.Error = _error.Value;
                }

                if (_type != null)
                {
                    span.Type = _type;
                }

                if (_tags != null)
                {
                    foreach (var pair in _tags)
                    {
                        span.SetTag(pair.Key, pair.Value);
                    }
                }

                return new OpenTracingSpan(span);
            }
        }

        public IScope StartActive(bool finishSpanOnDispose)
        {
            lock (_lock)
            {
                ISpan span = Start();

                if (_tags != null)
                {
                    foreach (var pair in _tags)
                    {
                        span.SetTag(pair.Key, pair.Value);
                    }
                }

                return _tracer.ScopeManager.Activate(span, finishSpanOnDispose);
            }
        }

        public ISpanBuilder WithStartTimestamp(DateTimeOffset startTimestamp)
        {
            lock (_lock)
            {
                _start = startTimestamp;
                return this;
            }
        }

        public ISpanBuilder WithTag(string key, bool value)
        {
            return WithTag(key, value.ToString());
        }

        public ISpanBuilder WithTag(string key, double value)
        {
            return WithTag(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public ISpanBuilder WithTag(string key, int value)
        {
            return WithTag(key, value.ToString());
        }

        public ISpanBuilder WithTag(string key, string value)
        {
            lock (_lock)
            {
                if (key == DatadogTags.ServiceName)
                {
                    _serviceName = value;
                    return this;
                }

                if (key == DatadogTags.ResourceName)
                {
                    _resourceName = value;
                    return this;
                }

                if (key == global::OpenTracing.Tag.Tags.Error.Key)
                {
                    _error = value == bool.TrueString;
                    return this;
                }

                if (key == DatadogTags.SpanType)
                {
                    _type = value;
                    return this;
                }

                if (_tags == null)
                {
                    _tags = new Dictionary<string, string>();
                }

                _tags[key] = value;
                return this;
            }
        }

        private Span GetParent()
        {
            Span explicitParent = _parent?.Span;

            if (explicitParent == null && !_ignoreActiveSpan)
            {
                // if parent was not set explicitly, default to active span as parent (unless disabled)
                return _tracer.ActiveSpan?.Span;
            }

            return explicitParent;
        }
    }
}
