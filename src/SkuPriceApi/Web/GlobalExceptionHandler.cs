using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SkuPriceApi.Domain.SeedWork.Exceptions;

namespace SkuPriceApi.Web;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException &&
            httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        var (status, title, detail) = exception switch
        {
            Polly.Timeout.TimeoutRejectedException =>
                (504, "Gateway Timeout", "The upstream service timed out."),

            OperationCanceledException { InnerException: TimeoutException }
                when !httpContext.RequestAborted.IsCancellationRequested =>
                (504, "Gateway Timeout", "The upstream service timed out."),

            Polly.CircuitBreaker.BrokenCircuitException =>
                (503, "Service Unavailable", "The upstream service is currently unavailable."),

            HttpRequestException or ERPClientInvalidDataException =>
                (502, "Bad Gateway", "The upstream service returned an invalid response."),

            _ =>
                (500, "Server Error", "An unexpected error occurred.")
        };

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true; 
    }
}