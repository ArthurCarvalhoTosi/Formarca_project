using Formarca.Domain.Tenancy;

namespace Formarca.Domain.Tests;

public class BrazilianDocumentsTests
{
    [Theory]
    [InlineData("390.533.447-05")]
    [InlineData("39053344705")]
    public void Accepts_Valid_Cpf(string value)
    {
        Assert.True(BrazilianDocuments.IsValidCpf(value));
        Assert.True(BrazilianDocuments.IsValidCpfOrCnpj(value));
    }

    [Theory]
    [InlineData("11.222.333/0001-81")]
    [InlineData("11222333000181")]
    public void Accepts_Valid_Cnpj(string value)
    {
        Assert.True(BrazilianDocuments.IsValidCnpj(value));
        Assert.True(BrazilianDocuments.IsValidCpfOrCnpj(value));
    }

    [Theory]
    [InlineData("11111111111")]
    [InlineData("39053344700")]
    [InlineData("123")]
    [InlineData("")]
    public void Rejects_Invalid_Cpf(string value) => Assert.False(BrazilianDocuments.IsValidCpf(value));

    [Theory]
    [InlineData("00000000000000")]
    [InlineData("11222333000180")]
    [InlineData("123")]
    public void Rejects_Invalid_Cnpj(string value) => Assert.False(BrazilianDocuments.IsValidCnpj(value));
}

public class BrazilianGeographicTests
{
    [Theory]
    [InlineData("SP")]
    [InlineData("sp")]
    [InlineData("TO")]
    public void Accepts_Valid_Uf(string value) => Assert.True(BrazilianGeographic.IsValidState(value));

    [Theory]
    [InlineData("XX")]
    [InlineData("")]
    [InlineData("SAO PAULO")]
    public void Rejects_Invalid_Uf(string value) => Assert.False(BrazilianGeographic.IsValidState(value));

    [Fact]
    public void Accepts_Eight_Digit_Zip()
    {
        Assert.True(BrazilianGeographic.IsValidZipCode("01310-100"));
        Assert.False(BrazilianGeographic.IsValidZipCode("01310"));
    }

    [Theory]
    [InlineData("(11) 98765-4321")]
    [InlineData("1134567890")]
    [InlineData("5511987654321")]
    public void Accepts_Valid_Phone(string value) => Assert.True(BrazilianGeographic.IsValidPhone(value));

    [Theory]
    [InlineData("123")]
    [InlineData("")]
    public void Rejects_Invalid_Phone(string value) => Assert.False(BrazilianGeographic.IsValidPhone(value));
}

public class TenantSlugTests
{
    [Fact]
    public void FromName_Strips_Accents_And_Lowercases()
    {
        Assert.Equal("jose-silva", TenantSlug.FromName("José Silva"));
        Assert.Equal("acme-ltd", TenantSlug.FromName("Acme Ltd"));
    }
}
