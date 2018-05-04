using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using Datadog.Trace.Agent;
using Datadog.Trace.TestHelpers;
using Moq;
using Xunit;

namespace Datadog.Trace.IntegrationTests
{
    public class SendTracesToAgent
    {
        private Tracer CreateTracer(string defaultServiceName, DelegatingHandler delegatingHandler)
        {
            var uri = new Uri("http://localhost:8126");
            var api = new Api(uri, delegatingHandler);
            var agentWriter = new AgentWriter(api);
            return new Tracer(agentWriter, defaultServiceName);
        }

        [Fact]
        public async void MinimalSpan()
        {
            var httpHandler = new RecordHttpHandler();
            var tracer = CreateTracer("SendTracesToAgent", httpHandler);
            var scope = tracer.StartActive("Operation");
            scope.Dispose();

            // Check that the HTTP calls went as expected
            await httpHandler.WaitForCompletion(1);
            Assert.Single(httpHandler.Requests);
            Assert.Single(httpHandler.Responses);
            Assert.All(httpHandler.Responses, (x) => Assert.Equal(HttpStatusCode.OK, x.StatusCode));

            var trace = httpHandler.Traces.Single();
            MsgPackHelpers.AssertSpanEqual(scope.Span, trace.Single());
        }

        [Fact]
        public async void CustomServiceName()
        {
            var httpHandler = new RecordHttpHandler();
            var tracer = CreateTracer("SendTracesToAgent", httpHandler);

            var scope = tracer.StartActive("Operation");
            scope.Span.ResourceName = "This is a resource";
            scope.Dispose();

            // Check that the HTTP calls went as expected
            await httpHandler.WaitForCompletion(1);
            Assert.Single(httpHandler.Requests);
            Assert.Single(httpHandler.Responses);
            Assert.All(httpHandler.Responses, (x) => Assert.Equal(HttpStatusCode.OK, x.StatusCode));

            var trace = httpHandler.Traces.Single();
            MsgPackHelpers.AssertSpanEqual(scope.Span, trace.Single());
        }

        [Fact]
        public async void Utf8Everywhere()
        {
            var httpHandler = new RecordHttpHandler();
            var tracer = CreateTracer("SendTracesToAgent", httpHandler);

            var scope = tracer.StartActive("Aᛗᚪᚾᚾᚪ", serviceName: "На берегу пустынных волн");
            scope.Span.ResourceName = "η γλώσσα μου έδωσαν ελληνική";
            scope.Span.SetTag("யாமறிந்த", "ნუთუ კვლა");
            scope.Dispose();

            // Check that the HTTP calls went as expected
            await httpHandler.WaitForCompletion(1);
            Assert.Single(httpHandler.Requests);
            Assert.Single(httpHandler.Responses);
            Assert.All(httpHandler.Responses, (x) => Assert.Equal(HttpStatusCode.OK, x.StatusCode));

            var trace = httpHandler.Traces.Single();
            MsgPackHelpers.AssertSpanEqual(scope.Span, trace.Single());
        }

        [Fact]
        public void WithDefaultFactory()
        {
            // This test does not check anything it validates that this codepath runs without exceptions
            var httpHandler = new Mock<DelegatingHandler>();
            var tracer = CreateTracer("SendTracesToAgent", httpHandler.Object);

            tracer.StartActive("Operation")
                .Dispose();
        }

        [Fact]
        public void WithGlobalTracer()
        {
            // This test does not check anything it validates that this codepath runs without exceptions
            Tracer.Instance.StartActive("Operation")
                .Dispose();
        }
    }
}
