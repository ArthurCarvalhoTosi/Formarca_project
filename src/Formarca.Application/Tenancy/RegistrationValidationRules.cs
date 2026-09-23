using FluentValidation;
using Formarca.Application.Common.Models;
using Formarca.Domain.Tenancy;

namespace Formarca.Application.Tenancy;

public interface IRegistrationFields
{
    string Name { get; }

    string Email { get; }

    string Phone { get; }

    string Document { get; }

    AddressDto Address { get; }
}

public interface IRegistrationCommercialFields : IRegistrationFields
{
    string? LegalRepresentativeName { get; }

    string? LegalRepresentativeDocument { get; }

    decimal? QuotedAmount { get; }

    string? BudgetNotes { get; }
}

public static class RegistrationValidationRules
{
    public static void Apply<T>(AbstractValidator<T> validator)
        where T : IRegistrationFields
    {
        validator.RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        validator.RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        validator.RuleFor(x => x.Phone)
            .NotEmpty()
            .Must(BrazilianGeographic.IsValidPhone)
            .WithMessage("Phone must be a valid Brazilian number (10 or 11 digits).");
        validator.RuleFor(x => x.Document)
            .NotEmpty()
            .Must(BrazilianDocuments.IsValidCpfOrCnpj)
            .WithMessage("Document must be a valid CPF (11 digits) or CNPJ (14 digits).");
        validator.RuleFor(x => x.Address).NotNull();
        validator.When(x => x.Address is not null, () =>
        {
            validator.RuleFor(x => x.Address.ZipCode)
                .NotEmpty()
                .Must(BrazilianGeographic.IsValidZipCode)
                .WithMessage("ZIP code must contain 8 digits.");
            validator.RuleFor(x => x.Address.Street).NotEmpty().MaximumLength(200);
            validator.RuleFor(x => x.Address.Number).NotEmpty().MaximumLength(20);
            validator.RuleFor(x => x.Address.Complement).MaximumLength(100);
            validator.RuleFor(x => x.Address.Neighborhood).NotEmpty().MaximumLength(100);
            validator.RuleFor(x => x.Address.City).NotEmpty().MaximumLength(100);
            validator.RuleFor(x => x.Address.State)
                .NotEmpty()
                .Must(BrazilianGeographic.IsValidState)
                .WithMessage("State must be a valid Brazilian UF.");
        });
    }

    public static void ApplyCommercial<T>(AbstractValidator<T> validator)
        where T : IRegistrationCommercialFields
    {
        Apply(validator);
        validator.RuleFor(x => x.LegalRepresentativeName).MaximumLength(200);
        validator.RuleFor(x => x.LegalRepresentativeDocument)
            .Must(value => string.IsNullOrWhiteSpace(value) || BrazilianDocuments.IsValidCpfOrCnpj(value))
            .WithMessage("Legal representative document must be a valid CPF or CNPJ.");
        validator.RuleFor(x => x.QuotedAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.QuotedAmount.HasValue);
        validator.RuleFor(x => x.BudgetNotes).MaximumLength(4000);
    }
}
