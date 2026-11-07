using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Sayyit.API.Infrastructure.Errors;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    private static readonly EventId CustomClientErrorEvent = new(1001, "CustomClientError");
    private static readonly EventId CustomServerErrorEvent = new(1002, "CustomServerError");
    private static readonly EventId UnexpectedServerErrorEvent = new(1003, "UnexpectedServerError");
    private static readonly EventId RequestAbortedEvent = new(1004, "RequestAborted");

    private const string UnexpectedFriendlyMessage = "An unexpected error occurred. Please try again.";
    private const string UnexpectedInternalMessage = "Unhandled exception during request processing.";

    private readonly ILogger<GlobalExceptionHandler> _logger = logger;
    private readonly IProblemDetailsService _problemDetailsService = problemDetailsService;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        if (httpContext.Response.HasStarted)
        {
            return false;
        }

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var requestMethod = httpContext.Request.Method;
        var requestPath = httpContext.Request.Path.Value ?? string.Empty;
        var exceptionType = exception.GetType().FullName ?? exception.GetType().Name;

        if (IsRequestAbortedCancellation(httpContext, exception))
        {
            const int clientClosedRequestStatus = 499;

            _logger.LogInformation(
                RequestAbortedEvent,
                "EventName={EventName}; Request aborted by client. StatusCode={StatusCode}; TraceId={TraceId}; RequestMethod={RequestMethod}; RequestPath={RequestPath}; ExceptionType={ExceptionType}; InternalMessage={InternalMessage}",
                RequestAbortedEvent.Name,
                clientClosedRequestStatus,
                traceId,
                requestMethod,
                requestPath,
                exceptionType,
                "Client disconnected before response completed.");

            httpContext.Response.StatusCode = clientClosedRequestStatus;
            return true;
        }

        if (exception is SayyitException sayyitException)
        {
            var statusCode = sayyitException.StatusCode;
            var eventId = statusCode < 500 ? CustomClientErrorEvent : CustomServerErrorEvent;

            if (statusCode < 500)
            {
                _logger.LogWarning(
                    eventId,
                    exception,
                    "EventName={EventName}; Handled SayyitException. StatusCode={StatusCode}; TraceId={TraceId}; RequestMethod={RequestMethod}; RequestPath={RequestPath}; ExceptionType={ExceptionType}; InternalMessage={InternalMessage}",
                    eventId.Name,
                    statusCode,
                    traceId,
                    requestMethod,
                    requestPath,
                    exceptionType,
                    sayyitException.InternalMessage);
            }
            else
            {
                _logger.LogError(
                    eventId,
                    exception,
                    "EventName={EventName}; Handled SayyitException. StatusCode={StatusCode}; TraceId={TraceId}; RequestMethod={RequestMethod}; RequestPath={RequestPath}; ExceptionType={ExceptionType}; InternalMessage={InternalMessage}",
                    eventId.Name,
                    statusCode,
                    traceId,
                    requestMethod,
                    requestPath,
                    exceptionType,
                    sayyitException.InternalMessage);
            }

            return await WriteProblemDetailsAsync(
                httpContext,
                statusCode,
                sayyitException.FriendlyMessage,
                cancellationToken);
        }

        _logger.LogError(
            UnexpectedServerErrorEvent,
            exception,
            "EventName={EventName}; Unhandled exception. StatusCode={StatusCode}; TraceId={TraceId}; RequestMethod={RequestMethod}; RequestPath={RequestPath}; ExceptionType={ExceptionType}; InternalMessage={InternalMessage}",
            UnexpectedServerErrorEvent.Name,
            StatusCodes.Status500InternalServerError,
            traceId,
            requestMethod,
            requestPath,
            exceptionType,
            UnexpectedInternalMessage);

        return await WriteProblemDetailsAsync(
            httpContext,
            StatusCodes.Status500InternalServerError,
            UnexpectedFriendlyMessage,
            cancellationToken);
    }

    private async ValueTask<bool> WriteProblemDetailsAsync(
        HttpContext httpContext,
        int statusCode,
        string detail,
        CancellationToken cancellationToken)
    {
        var title = ReasonPhrases.GetReasonPhrase(statusCode);
        if (string.IsNullOrWhiteSpace(title))
        {
            title = "Error";
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = "about:blank"
        };

        httpContext.Response.StatusCode = statusCode;

        var writeResult = await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = null
        });

        return writeResult;
    }

    private static bool IsRequestAbortedCancellation(HttpContext httpContext, Exception exception)
    {
        return exception is OperationCanceledException or TaskCanceledException
               && httpContext.RequestAborted.IsCancellationRequested;
    }
}
