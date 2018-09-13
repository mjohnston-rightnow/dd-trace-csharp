#if NET45

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.ExtensionMethods;

namespace Datadog.Trace.ClrProfiler.Integrations
{
    /// <summary>
    /// ApsNetWeb5Integration wraps the Web API.
    /// </summary>
    public static class AspNetWebApi2Integration
    {
        private const string OperationName = "aspnet_webapi.request";

        /// <summary>
        /// ExecuteAsync calls the underlying ExecuteAsync and traces the request.
        /// </summary>
        /// <param name="this">The Api Controller</param>
        /// <param name="controllerContext">The controller context for the call</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A task with the result</returns>
        public static async Task<HttpResponseMessage> ExecuteAsync(dynamic @this, dynamic controllerContext, CancellationToken cancellationToken)
        {
            // Task<HttpResponseMessage> System.Web.Http.Controllers.IHttpController.ExecuteAsync(HttpControllerContext controllerContext, CancellationToken cancellationToken)
            Scope scope = null;

            try
            {
                scope = CreateScope(controllerContext);
            }
            catch
            {
                // TODO: log this as an instrumentation error, but continue
            }

            HttpResponseMessage result;

            try
            {
                // call the original method, catching and rethrowing any unhandled exceptions
                result = await @this.ExecuteAsync(controllerContext, cancellationToken);
            }
            catch (Exception ex)
            {
                scope?.Span?.SetException(ex);
                throw;
            }

            try
            {
                // some fields aren't set till after execution, so repopulate anything missing
                if (scope?.Span != null)
                {
                    UpdateSpan(scope.Span, controllerContext);
                }
            }
            catch
            {
                // TODO: log this as an instrumentation error, but continue
            }

            return result;
        }

        private static Scope CreateScope(dynamic controllerContext)
        {
            var scope = Tracer.Instance.StartActive(OperationName);
            UpdateSpan(scope.Span, controllerContext);
            return scope;
        }

        private static void UpdateSpan(Span span, dynamic controllerContext)
        {
            var req = controllerContext?.Request;

            string host = req?.Headers?.Host ?? string.Empty;
            string rawUrl = req?.RequestUri?.ToString()?.ToLowerInvariant() ?? string.Empty;
            string method = controllerContext?.Request?.Method?.Method?.ToUpperInvariant() ?? "GET";
            string route = null;
            try
            {
                route = controllerContext?.RouteData?.Route?.RouteTemplate;
            }
            catch
            {
            }

            string resourceName = $"{method} {rawUrl}";
            if (route != null)
            {
                resourceName = $"{method} {route}";
            }

            string controller = string.Empty;
            string action = string.Empty;
            try
            {
                if (controllerContext?.RouteData?.Values is IDictionary<string, object> routeValues)
                {
                    controller = (routeValues.GetValueOrDefault("controller") as string) ?? string.Empty;
                    action = (routeValues.GetValueOrDefault("action") as string) ?? string.Empty;
                }
            }
            catch
            {
            }

            span.ResourceName = resourceName;
            span.Type = SpanTypes.Web;
            span.SetTag(Tags.AspNetAction, action);
            span.SetTag(Tags.AspNetController, controller);
            span.SetTag(Tags.AspNetRoute, route);
            span.SetTag(Tags.HttpMethod, method);
            span.SetTag(Tags.HttpRequestHeadersHost, host);
            span.SetTag(Tags.HttpUrl, rawUrl);
        }
    }
}

#endif
