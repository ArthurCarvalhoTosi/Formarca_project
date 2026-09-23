using Formarca.Application.Common.Models;
using Formarca.Application.Tenancy.Commands;

namespace Formarca.Application.Tests;

public class RegistrationValidatorTests
{
    private static readonly AddressDto ValidAddress = new(
        "01310100", "Av Paulista", "100", null, "Bela Vista", "São Paulo", "SP");

    [Fact]
    public void Submit_Accepts_Valid_Cpf_And_Uf()
    {
        var result = new SubmitRegistrationCommandValidator().Validate(ValidCommand());
        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void Submit_Rejects_Invalid_Cpf()
    {
        var result = new SubmitRegistrationCommandValidator().Validate(ValidCommand() with { Document = "11111111111" });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SubmitRegistrationCommand.Document));
    }

    [Fact]
    public void Submit_Rejects_Invalid_Cnpj()
    {
        var result = new SubmitRegistrationCommandValidator().Validate(ValidCommand() with { Document = "11222333000180" });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SubmitRegistrationCommand.Document));
    }

    [Fact]
    public void Submit_Accepts_Cnpj_As_Legal_Representative_Document()
    {
        var result = new SubmitRegistrationCommandValidator().Validate(
            ValidCommand() with { LegalRepresentativeDocument = "11222333000181" });
        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void Submit_Rejects_Invalid_Legal_Representative_Document()
    {
        var result = new SubmitRegistrationCommandValidator().Validate(
            ValidCommand() with { LegalRepresentativeDocument = "11222333000180" });
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(SubmitRegistrationCommand.LegalRepresentativeDocument));
    }

    [Fact]
    public void Submit_Rejects_Invalid_Uf()
    {
        var result = new SubmitRegistrationCommandValidator().Validate(
            ValidCommand() with { Address = ValidAddress with { State = "XX" } });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Address.State");
    }

    [Fact]
    public void Approve_Requires_Password_Min_Length()
    {
        var result = new ApproveTenantCommandValidator().Validate(new ApproveTenantCommand(Guid.NewGuid(), "short"));
        Assert.False(result.IsValid);
    }

    private static SubmitRegistrationCommand ValidCommand() =>
        new(
            "Acme Ltd",
            "maria@acme.com",
            "11987654321",
            "39053344705",
            ValidAddress);
}
