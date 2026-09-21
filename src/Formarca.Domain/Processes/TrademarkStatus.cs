using System.Globalization;
using System.Text;
using Formarca.Domain.Common;
using Formarca.Domain.Tenancy;

namespace Formarca.Domain.Processes;

public class TrademarkStatus : Entity, IPlatformEntity
{
    public const int CodeMaxLength = 80;
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    private TrademarkStatus()
    {
    }

    public string Code { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public TrademarkStatusPhase Phase { get; private set; }

    public TrademarkStatusTone Tone { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static TrademarkStatus Create(
        string code,
        string title,
        string description,
        int sortOrder,
        TrademarkStatusPhase phase,
        TrademarkStatusTone tone,
        bool isActive = true)
    {
        var status = new TrademarkStatus();
        status.Code = NormalizeCode(code);
        status.Apply(title, description, sortOrder, phase, tone, isActive);
        return status;
    }

    public void Update(
        string title,
        string description,
        int sortOrder,
        TrademarkStatusPhase phase,
        TrademarkStatusTone tone,
        bool isActive)
    {
        Apply(title, description, sortOrder, phase, tone, isActive);
        MarkUpdated();
    }

    public void SetSortOrder(int sortOrder)
    {
        SortOrder = EnsureSortOrder(sortOrder);
        MarkUpdated();
    }

    public static string NormalizeCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        var previousHyphen = false;

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var lower = char.ToLowerInvariant(character);
            if (lower is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                builder.Append(lower);
                previousHyphen = false;
                continue;
            }

            if (!previousHyphen && builder.Length > 0)
            {
                builder.Append('-');
                previousHyphen = true;
            }
        }

        var code = builder.ToString().Trim('-');
        if (code.Length > CodeMaxLength)
        {
            code = code[..CodeMaxLength].Trim('-');
        }

        if (code.Length == 0)
        {
            throw new ArgumentException("Status code is required.", nameof(value));
        }

        return code;
    }

    private void Apply(
        string title,
        string description,
        int sortOrder,
        TrademarkStatusPhase phase,
        TrademarkStatusTone tone,
        bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (!Enum.IsDefined(phase))
        {
            throw new ArgumentException("Status phase is invalid.", nameof(phase));
        }

        if (!Enum.IsDefined(tone))
        {
            throw new ArgumentException("Status tone is invalid.", nameof(tone));
        }

        var trimmedTitle = title.Trim();
        var trimmedDescription = description.Trim();
        if (trimmedTitle.Length > TitleMaxLength)
        {
            throw new ArgumentException("Status title is too long.", nameof(title));
        }

        if (trimmedDescription.Length > DescriptionMaxLength)
        {
            throw new ArgumentException("Status description is too long.", nameof(description));
        }

        Title = trimmedTitle;
        Description = trimmedDescription;
        SortOrder = EnsureSortOrder(sortOrder);
        Phase = phase;
        Tone = tone;
        IsActive = isActive;
    }

    private static int EnsureSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new ArgumentException("Status order cannot be negative.", nameof(sortOrder));
        }

        return sortOrder;
    }
}
