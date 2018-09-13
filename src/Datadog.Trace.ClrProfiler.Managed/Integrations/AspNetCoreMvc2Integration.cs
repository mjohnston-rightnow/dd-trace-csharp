using System;
using System.Collections.Generic;
using System.Reflection;

namespace Datadog.Trace.ClrProfiler.Integrations
{
    /// <summary>
    /// The ASP.NET Core MVC 2 integration.
    /// </summary>
    public static class AspNetCoreMvc2Integration
    {
        private const string HttpContextKey = "__Datadog.Trace.ClrProfiler.Integrations." + nameof(AspNetCoreMvc2Integration);
        private const string OperationName = "aspnet_core_mvc.request";

        private static Action<object, object, object, object> _beforeAction;
        private static Action<object, object, object, object> _afterAction;

        /// <summary>
        /// Wrapper method used to instrument Microsoft.AspNetCore.Mvc.Internal.MvcCoreDiagnosticSourceExtensions.BeforeAction()
        /// </summary>
        /// <param name="diagnosticSource">The DiagnosticSource that this extension method was called on.</param>
        /// <param name="actionDescriptor">An ActionDescriptor with information about the current action.</param>
        /// <param name="httpContext">The HttpContext for the current request.</param>
        /// <param name="routeData">A RouteData with information about the current route.</param>
        public static void BeforeAction(
            object diagnosticSource,
            dynamic actionDescriptor,
            dynamic httpContext,
            object routeData)
        {
            Scope scope = null;

            try
            {
                scope = Tracer.Instance.StartActive(OperationName);
                UpdateSpan(scope.Span, actionDescriptor, httpContext);

                IDictionary<object, object> contextItems = httpContext.Items;
                contextItems[HttpContextKey] = scope;
            }
            catch
            {
                // TODO: log this as an instrumentation error, but continue calling instrumented method
            }

            try
            {
                if (_beforeAction == null)
                {
                    Type type = actionDescriptor.GetType()
                                                .GetTypeInfo()
                                                .Assembly
                                                .GetType("Microsoft.AspNetCore.Mvc.Internal.MvcCoreDiagnosticSourceExtensions");

                    _beforeAction = DynamicMethodBuilder.CreateMethodCallDelegate<Action<object, object, object, object>>(
                        type,
                        "BeforeAction",
                        isStatic: true);
                }
            }
            catch
            {
                // TODO: log this as an instrumentation error, we cannot call instrumented method,
                // profiled app will continue working without DiagnosticSource
            }

            try
            {
                // call the original method, catching and rethrowing any unhandled exceptions
                _beforeAction?.Invoke(diagnosticSource, actionDescriptor, httpContext, routeData);
            }
            catch (Exception ex)
            {
                scope?.Span?.SetException(ex);
                throw;
            }
        }

        /// <summary>
        /// Wrapper method used to instrument Microsoft.AspNetCore.Mvc.Internal.MvcCoreDiagnosticSourceExtensions.AfterAction()
        /// </summary>
        /// <param name="diagnosticSource">The DiagnosticSource that this extension method was called on.</param>
        /// <param name="actionDescriptor">An ActionDescriptor with information about the current action.</param>
        /// <param name="httpContext">The HttpContext for the current request.</param>
        /// <param name="routeData">A RouteData with information about the current route.</param>
        public static void AfterAction(
            object diagnosticSource,
            object actionDescriptor,
            dynamic httpContext,
            object routeData)
        {
            Scope scope = null;

            try
            {
                IDictionary<object, object> contextItems = httpContext?.Items;
                scope = contextItems?[HttpContextKey] as Scope;
            }
            catch
            {
                // TODO: log this as an instrumentation error, but continue calling instrumented method
            }

            try
            {
                if (_afterAction == null)
                {
                    Type type = actionDescriptor.GetType()
                                                .GetTypeInfo()
                                                .Assembly
                                                .GetType("Microsoft.AspNetCore.Mvc.Internal.MvcCoreDiagnosticSourceExtensions");

                    _afterAction = DynamicMethodBuilder.CreateMethodCallDelegate<Action<object, object, object, object>>(
                        type,
                        "AfterAction",
                        isStatic: true);
                }
            }
            catch
            {
                // TODO: log this as an instrumentation error, we cannot call instrumented method,
                // profiled app will continue working without DiagnosticSource
            }

            try
            {
                // call the original method, catching and rethrowing any unhandled exceptions
                _afterAction?.Invoke(diagnosticSource, actionDescriptor, httpContext, routeData);
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
                    if (httpContext != null)
                    {
                        scope.Span.SetTag(Tags.HttpStatusCode, httpContext.Response.StatusCode.ToString());
                    }

                    scope.Span.Dispose();
                }
            }
        }

        private static void UpdateSpan(Span span, dynamic actionDescriptor, dynamic httpContext)
        {
            string controllerName = actionDescriptor.ControllerName;
            string actionName = actionDescriptor.ActionName;
            string resourceName = $"{controllerName}.{actionName}";

            string httpMethod = httpContext.Request.Method.ToUpperInvariant();
            string url = httpContext.Request.GetDisplayUrl().ToLowerInvariant();

            span.Type = SpanTypes.Web;
            span.ResourceName = resourceName;
            span.SetTag(Tags.HttpMethod, httpMethod);
            span.SetTag(Tags.HttpUrl, url);
            span.SetTag(Tags.AspNetController, controllerName);
            span.SetTag(Tags.AspNetAction, actionName);
        }
    }
}
