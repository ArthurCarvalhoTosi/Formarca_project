using Formarca.Application.Common.Interfaces;

namespace Formarca.Infrastructure.Documents;

public sealed class EmbeddedContractTemplateProvider : IContractTemplateProvider
{
    public string GetTemplate(string templateKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateKey);

        var name = $"Formarca.Infrastructure.Documents.Templates.{templateKey.Trim().ToLowerInvariant()}.md";
        using var stream = typeof(EmbeddedContractTemplateProvider).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Contract template '{templateKey}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
