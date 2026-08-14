namespace Formarca.Domain.Tenancy;

public sealed class Address
{
    private Address()
    {
    }

    public string ZipCode { get; private set; } = string.Empty;

    public string Street { get; private set; } = string.Empty;

    public string Number { get; private set; } = string.Empty;

    public string? Complement { get; private set; }

    public string Neighborhood { get; private set; } = string.Empty;

    public string City { get; private set; } = string.Empty;

    public string State { get; private set; } = string.Empty;

    public static Address Create(
        string zipCode,
        string street,
        string number,
        string? complement,
        string neighborhood,
        string city,
        string state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zipCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(street);
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        ArgumentException.ThrowIfNullOrWhiteSpace(neighborhood);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(state);

        return new Address
        {
            ZipCode = BrazilianDocuments.DigitsOnly(zipCode),
            Street = street.Trim(),
            Number = number.Trim(),
            Complement = string.IsNullOrWhiteSpace(complement) ? null : complement.Trim(),
            Neighborhood = neighborhood.Trim(),
            City = city.Trim(),
            State = state.Trim().ToUpperInvariant()
        };
    }
}
