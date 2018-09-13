#if NET45

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Web;
using Datadog.Trace.ExtensionMethods;

namespace Datadog.Trace.ClrProfiler.Integrations
{
    /// <summary>
    /// The ASP.NET MVC 5 integration.
    /// </summary>
    public static class AspNetMvc5Integration
    {
        private const string HttpContextKey = "__Datadog.Trace.ClrProfiler.Integrations." + nameof(AspNetMvc5Integration);
        private const string OperationName = "aspnet_mvc.request";

        /// <summary>
        /// Wrapper method used to instrument System.Web.Mvc.Async.AsyncControllerActionInvoker.BeginInvokeAction().
        /// </summary>
        /// <param name="this">The AsyncControllerActionInvoker instance.</param>
        /// <param name="controllerContext">The ControllerContext for the current request.</param>
        /// <param name="actionName">The name of the controller action.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate.</param>
        /// <param name="state">An object that holds the state of the async operation.</param>
        /// <returns>Returns the <see cref="IAsyncResult "/> returned by the original BeginInvokeAction() that is later passed to <see cref="EndInvokeAction"/>.</returns>
        public static object BeginInvokeAction(
            dynamic @this,
            dynamic controllerContext,
            dynamic actionName,
            dynamic callback,
            dynamic state)
        {
            // IAsyncResult System.Web.Mvc.Async.AsyncControllerActionInvoker.BeginInvokeAction(ControllerContext controllerContext, string actionName, AsyncCallback callback, object state)
            Scope scope = null;

            try
            {
                scope = Tracer.Instance.StartActive(OperationName);
                UpdateSpan(scope.Span, controllerContext);

                if (HttpContext.Current != null)
                {
                    HttpContext.Current.Items[HttpContextKey] = scope;
                }
            }
            catch
            {
                // TODO: log this as an instrumentation error, but continue calling instrumented method
            }

            try
            {
                // call the original method, catching and rethrowing any unhandled exceptions
                return @this.BeginInvokeAction(controllerContext, actionName, callback, state);
            }
            catch (Exception ex)
            {
                scope?.Span?.SetException(ex);
                throw;
            }
        }

        /// <summary>
        /// Wrapper method used to instrument System.Web.Mvc.Async.AsyncControllerActionInvoker.EndInvokeAction().
        /// </summary>
        /// <param name="this">The AsyncControllerActionInvoker instance.</param>
        /// <param name="asyncResult">The <see cref="IAsyncResult"/> returned by <see cref="BeginInvokeAction"/>.</param>
        /// <returns>Returns the <see cref="bool"/> returned by the original EndInvokeAction().</returns>
        public static bool EndInvokeAction(dynamic @this, dynamic asyncResult)
        {
            // bool System.Web.Mvc.Async.AsyncControllerActionInvoker.EndInvokeAction(IAsyncResult asyncResult)
            Scope scope = null;

            try
            {
                scope = HttpContext.Current?.Items[HttpContextKey] as Scope;
            }
            catch
            {
                // TODO: log this as an instrumentation error, but continue calling instrumented method
            }

            try
            {
                // call the original method, catching and rethrowing any unhandled exceptions
                return @this.EndInvokeAction(asyncResult);
            }
            catch (Exception ex)
            {
                scope?.Span?.SetException(ex);
                throw;
            }
            finally
            {
                if (scope?.Span != null)
                {
                    if (HttpContext.Current != null)
                    {
                        scope.Span.SetTag(Tags.HttpStatusCode, HttpContext.Current.Response.StatusCode.ToString());
                    }

                    scope.Span.Dispose();
                }
            }
        }

        private static void UpdateSpan(Span span, dynamic controllerContext)
        {
            span.Type = SpanTypes.Web;

            // access the controller context without referencing System.Web.Mvc directly
            span.SetTag(Tags.AspNetRoute, (string)controllerContext.RouteData.Route.Url);

            HttpRequestBase request = (controllerContext.HttpContext as HttpContextBase)?.Request;

            if (request != null)
            {
                string url = request.RawUrl.ToLowerInvariant();
                span.SetTag(Tags.HttpUrl, url);

                string host = request.Headers.Get("Host");
                span.SetTag(Tags.HttpRequestHeadersHost, host);

                string httpMethod = request.HttpMethod.ToUpperInvariant();
                span.SetTag(Tags.HttpMethod, httpMethod);
            }

            IDictionary<string, object> routeValues = controllerContext.RouteData.Values;
            string controllerName = (routeValues.GetValueOrDefault("controller") as string)?.ToSentenceCaseInvariant();
            span.SetTag(Tags.AspNetController, controllerName);

            string actionName = (routeValues.GetValueOrDefault("action") as string)?.ToSentenceCaseInvariant();
            span.SetTag(Tags.AspNetAction, actionName);

            string resourceName = $"{controllerName}.{actionName}";
            span.ResourceName = resourceName;
        }
    }
}

#endif
