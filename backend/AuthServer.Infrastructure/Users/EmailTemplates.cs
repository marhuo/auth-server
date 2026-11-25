using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using AuthServer.Domain.Users;

namespace AuthServer.Infrastructure.Users;

public sealed record EmailMessage(string Subject, string Html);

// Fills the email templates (Users/EmailTemplates/<name>.<fi|en>.html) in. A template is read once at
// first use. {{Name}} placeholders get HTML-encoded values; the <title> is the subject.
public static partial class EmailTemplates
{
    private static readonly ConcurrentDictionary<string, string> Templates = new();

    public static EmailMessage SignInCodeEmail(string code, string link, string appName, EmailLanguage language) =>
        Render("sign-in-code", language, new()
        {
            ["AppName"] = appName,
            ["Code"] = code,
            ["Link"] = link,
            ["Minutes"] = ((int)SignInCode.Lifetime.TotalMinutes).ToString()
        });

    public static EmailMessage InviteEmail(
        string toEmail, string link, string appName, string tenantName, EmailLanguage language) =>
        Render("invite", language, new()
        {
            ["AppName"] = appName,
            ["TenantName"] = tenantName,
            ["Email"] = toEmail,
            ["Link"] = link
        });

    private static EmailMessage Render(string name, EmailLanguage language, Dictionary<string, string> values)
    {
        var html = Load($"{name}.{(language == EmailLanguage.En ? "en" : "fi")}.html");
        foreach (var (key, value) in values)
            html = html.Replace($"{{{{{key}}}}}", WebUtility.HtmlEncode(value));

        var subject = WebUtility.HtmlDecode(TitleRegex().Match(html).Groups[1].Value);
        return new EmailMessage(subject, html);
    }

    private static string Load(string fileName) =>
        Templates.GetOrAdd(fileName, _ =>
        {
            using var stream = typeof(EmailTemplates).Assembly.GetManifestResourceStream($"EmailTemplates/{fileName}")
                ?? throw new InvalidOperationException($"Email template {fileName} was not found.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        });

    [GeneratedRegex("<title>(.*?)</title>", RegexOptions.Singleline)]
    private static partial Regex TitleRegex();}
