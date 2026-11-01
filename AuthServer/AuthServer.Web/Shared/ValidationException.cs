namespace AuthServer.Web.Shared;


public sealed class ValidationException(IReadOnlyList<ValidationError> errors) : Exception("Validation failed.")
{
    public ValidationException(ValidationError error) : this([error]) { }

    public ValidationException(string key, ErrorCode code, string messageEn, string messageFi) : this(new ValidationError(key, code, messageEn, messageFi))
    {

    }

    public IReadOnlyList<ValidationError> Errors { get; } = errors;
}

