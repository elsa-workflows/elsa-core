using Elsa.Studio.Labels.Client;
using Elsa.Studio.Labels.Models;
using FluentValidation;

namespace Elsa.Studio.Labels.UI.Validators;

/// <summary>
/// A validator for <see cref="LabelInputModel"/> instances.
/// </summary>
public class LabelInputModelValidator : AbstractValidator<LabelInputModel>
{
    /// <inheritdoc />
    public LabelInputModelValidator(ILabelsApi api)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please enter a name for the label.");
    }
}