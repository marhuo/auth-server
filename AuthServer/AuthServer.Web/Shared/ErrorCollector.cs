namespace AuthServer.Web.Shared;

public sealed class ErrorCollector
{
    private readonly List<ValidationError> _errors = [];

    public bool Has(string key) => _errors.Any(e => e.Key == key);

    public void Add(string key, ErrorCode code, string messageEn, string messageFi) =>
        _errors.Add(new ValidationError(key, code, messageEn, messageFi));

    public void ThrowIfAny()
    {
        if (_errors.Count > 0)
            throw new ValidationException(_errors);
    }
}
