using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Designer.Models;
using FluentValidation;

namespace Elsa.Studio.Workflows.Validators;

/// <summary>
/// A validator for <see cref="ExportGraphOptions"/> instances.
/// </summary>
public class ExportGraphOptionsValidator : AbstractValidator<ExportGraphOptions>
{
    /// <summary>
    /// The largest padding, in pixels, that the raster exporter is allowed to apply. The exporter grows the canvas
    /// by twice this value in each dimension, so an unbounded padding can exhaust the browser's memory.
    /// </summary>
    private const int MaxPadding = 1000;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportGraphOptionsValidator"/> class.
    /// </summary>
    /// <param name="localizer">The localizer.</param>
    public ExportGraphOptionsValidator(ILocalizer localizer)
    {
        RuleFor(x => x.FileName).NotEmpty().WithMessage(localizer["Please enter a file name for the export."]);

        // Scoped to the formats whose export actually uses the padding, which are exactly the formats for which the
        // dialog shows the field. Validating it for SVG too would block the dialog on a field the user cannot see.
        // The bound is enforced here, not via Min/Max on the MudNumericField, because MudBlazor's numeric field
        // clamps out-of-range values inside SetValueAsync instead of reporting them, which would silently coerce
        // the input instead of surfacing a validation message. This validator is the single source of truth.
        RuleFor(x => x.Padding)
            .InclusiveBetween(0, MaxPadding)
            .When(x => x.Format != ExportGraphFormat.Svg)
            .WithMessage(localizer["The padding must be between 0 and {0} pixels.", MaxPadding]);
    }
}
