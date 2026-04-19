using System.Globalization;
using System.Reflection;
using Scriban;
using Scriban.Runtime;

namespace Template.Infrastructure.Templating;

public sealed class EmbeddedScribanTemplateRenderer : ITemplateRenderer
{
    private const string TEMPLATE_ROOT = "Templates";
    private const string TEMPLATE_EXTENSION = "scriban";
    private const string DEFAULT_CULTURE = "hu_HU";
    private const string FALLBACK_CULTURE = "en_US";

    private readonly Assembly _assembly = typeof(EmbeddedScribanTemplateRenderer).Assembly;
    private readonly string _rootNamespace = typeof(EmbeddedScribanTemplateRenderer).Assembly.GetName().Name ?? throw new InvalidOperationException("Assembly name cannot be resolved.");
    private readonly HashSet<string> _resourceNames;

    public EmbeddedScribanTemplateRenderer()
    {
        _resourceNames = _assembly
            .GetManifestResourceNames()
            .ToHashSet(StringComparer.Ordinal);
    }

    public async Task<string> RenderAsync(string templateGroup, string templateName, string templatePart, object model, CultureInfo? culture = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var resourceName = ResolveResourceName(templateGroup, templateName, templatePart, culture);
        var templateText = await ReadEmbeddedResourceAsync(resourceName, ct);
        var template = Scriban.Template.Parse(templateText, resourceName);

        if (template.HasErrors)
        {
            var errors = string.Join(Environment.NewLine, template.Messages.Select(static x => x.Message));
            throw new InvalidOperationException($"Template parse error in '{resourceName}':{Environment.NewLine}{errors}");
        }

        var context = new TemplateContext
        {
            MemberRenamer = static member => member.Name
        };

        var globals = new ScriptObject();
        globals.SetValue("model", model, true);
        context.PushGlobal(globals);

        return template.Render(context);
    }

    private string ResolveResourceName(string templateGroup, string templateName, string templatePart, CultureInfo? culture)
    {
        foreach (var cultureName in GetCultureCandidates(culture))
        {
            var resourceName = $"{_rootNamespace}.{TEMPLATE_ROOT}.{templateGroup}.{cultureName}.{templateName}.{templatePart}.{TEMPLATE_EXTENSION}";
            if (_resourceNames.Contains(resourceName))
            {
                return resourceName;
            }
        }

        throw new FileNotFoundException($"No embedded template found for group '{templateGroup}', template '{templateName}', part '{templatePart}'.");
    }

    private static IEnumerable<string> GetCultureCandidates(CultureInfo? culture)
    {
        var yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (culture is not null)
        {
            var current = culture;
            while (current != CultureInfo.InvariantCulture)
            {
                if (yielded.Add(current.Name))
                {
                    yield return current.Name.Replace('-', '_');
                }

                current = current.Parent;
            }
        }

        if (yielded.Add(DEFAULT_CULTURE))
        {
            yield return DEFAULT_CULTURE;
        }

        if (yielded.Add(FALLBACK_CULTURE))
        {
            yield return FALLBACK_CULTURE;
        }
    }

    private async Task<string> ReadEmbeddedResourceAsync(string resourceName, CancellationToken ct)
    {
        await using var stream = _assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Embedded resource '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(ct);
    }
}
