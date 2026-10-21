using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SkuPriceApi.Domain.SeedWork.Exceptions;

namespace SkuPriceApi.Web;

public class DomainExceptionHandler(ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is DomainException domainException)
        {
            logger.LogInformation(exception, "A domain exception occurred: {Message}", exception.Message);

            var code = StatusCodes.Status400BadRequest;

            if (domainException is NotFoundDomainException)
            {
                code = StatusCodes.Status404NotFound;
            }

            var problemDetails = new ProblemDetails
            {
                Status = code,
                Title = "Domain Error",
                Detail = domainException.Message
            };

            httpContext.Response.StatusCode = problemDetails.Status.Value;

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }

        return false;
    }
}