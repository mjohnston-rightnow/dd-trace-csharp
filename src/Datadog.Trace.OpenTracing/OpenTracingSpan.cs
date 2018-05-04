﻿using System;
using System.Collections.Generic;
using System.Globalization;
using Datadog.Trace.Logging;
using OpenTracing;

namespace Datadog.Trace.OpenTracing
{
    internal class OpenTracingSpan : ISpan
    {
        private static ILog _log = LogProvider.For<OpenTracingSpan>();

        internal OpenTracingSpan(Span span)
        {
            Span = span;
            Context = new OpenTracingSpanContext(Span);
        }

        public OpenTracingSpanContext Context { get; }

        ISpanContext ISpan.Context => Context;

        internal Span Span { get; }

        // TODO lucas: inline this in a separate commit, it will modify a lot of files
        // This is only exposed for tests
        internal Span DDSpan => Span;

        internal string OperationName => Span.OperationName;

        internal TimeSpan Duration => Span.Duration;

        public string GetBaggageItem(string key)
        {
            _log.Debug("ISpan.GetBaggageItem is not implemented by Datadog.Trace");
            return null;
        }

        public ISpan Log(string eventName)
        {
            _log.Debug("ISpan.Log is not implemented by Datadog.Trace");
            return this;
        }

        public ISpan Log(DateTimeOffset timestamp, string eventName)
        {
            _log.Debug("ISpan.Log is not implemented by Datadog.Trace");
            return this;
        }

        public ISpan Log(DateTimeOffset timestamp, IDictionary<string, object> fields)
        {
            _log.Debug("ISpan.Log is not implemented by Datadog.Trace");
            return this;
        }

        public ISpan Log(IDictionary<string, object> fields)
        {
            _log.Debug("ISpan.Log is not implemented by Datadog.Trace");
            return this;
        }

        public ISpan SetBaggageItem(string key, string value)
        {
            _log.Debug("ISpan.SetBaggageItem is not implemented by Datadog.Trace");
            return this;
        }

        public ISpan SetOperationName(string operationName)
        {
            Span.OperationName = operationName;
            return this;
        }

        public string GetTag(string key)
        {
            return Span.GetTag(key);
        }

        public ISpan SetTag(string key, bool value)
        {
            return SetTag(key, value.ToString());
        }

        public ISpan SetTag(string key, double value)
        {
            return SetTag(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public ISpan SetTag(string key, int value)
        {
            return SetTag(key, value.ToString());
        }

        public ISpan SetTag(string key, string value)
        {
            if (key == DatadogTags.ResourceName)
            {
                Span.ResourceName = value;
                return this;
            }

            if (key == global::OpenTracing.Tag.Tags.Error.Key)
            {
                Span.Error = value == bool.TrueString;
                return this;
            }

            if (key == DatadogTags.SpanType)
            {
                Span.Type = value;
                return this;
            }

            Span.SetTag(key, value);
            return this;
        }

        public void Finish()
        {
            Span.Finish();
        }

        public void Finish(DateTimeOffset finishTimestamp)
        {
            Span.Finish(finishTimestamp);
        }
    }
}