using AuthServer.Domain.Common;
using AuthServer.Application.Common.Messages;
using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json;

namespace AuthServer.Web.Setup;

public sealed class ValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {

        if (exception is not ValidationException validation)
        {
            return false;
        }

        var response = new ErrorResponseDto(validation.Errors
            .Select(e => new ErrorDto(JsonNamingPolicy.CamelCase.ConvertName(e.Key), e.Code.ToString(), e.MessageEn, e.MessageFi))
            .ToList());

        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        await context.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}