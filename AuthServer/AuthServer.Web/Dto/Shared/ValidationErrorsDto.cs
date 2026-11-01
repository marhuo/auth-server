using System.Collections.ObjectModel;

namespace AuthServer.Web.Dto.Shared;

public class ValidationErrorsDto
{
    public ReadOnlyCollection<ValidationErrorDto> Errors { get; private set; } = new([]);
}

public record class ValidationErrorDto(string Key, string Error);
