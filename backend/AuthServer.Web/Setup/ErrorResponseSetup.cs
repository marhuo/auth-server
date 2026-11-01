using AuthServer.Application.Common.Messages;
using AuthServer.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AuthServer.Web.Setup;

// Both kinds of invalid input are returned as 400 with ErrorResponseDto:
// - input that cannot be bound to the request (wrong type, malformed JSON): InvalidModelStateResponseFactory
// - ValidationException thrown by the Application or Domain layer: ValidationExceptionHandler
// Other unhandled exceptions are returned as 500 ProblemDetails.
public static class ErrorResponseSetup
{
    public static IServiceCollection AddErrorResponses(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(e => e.Key != "request" && e.Value?.Errors.Count > 0)
                    .SelectMany(e => e.Value!.Errors.Select(_ => new ErrorDto(
                        JsonNamingPolicy.CamelCase.ConvertName(e.Key.TrimStart('$', '.')),
                        ErrorCode.InvalidFormat.ToString(),
                        "Value is missing or invalid.",
                        "Arvo puuttuu tai on virheellinen.")))
                    .ToList();

                return new BadRequestObjectResult(new ErrorResponseDto(errors));
            };
        });

        services.AddExceptionHandler<ValidationExceptionHandler>();

        // UseExceptionHandler() without options needs IProblemDetailsService for the 500 response.
        services.AddProblemDetails();

        return services;
    }

    // Call first in the pipeline, so that exceptions from all later middleware are handled.
    public static WebApplication UseErrorResponses(this WebApplication app)
    {
        app.UseExceptionHandler();
        return app;
    }
}
