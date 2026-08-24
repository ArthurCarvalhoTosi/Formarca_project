namespace Formarca.Domain.Brands;

public class BrandNiceClass
{
    private BrandNiceClass()
    {
    }

    public int Number { get; private set; }

    public static BrandNiceClass Create(int number)
    {
        if (number is < 1 or > 45)
        {
            throw new ArgumentOutOfRangeException(nameof(number), "INPI class must be between 1 and 45.");
        }

        return new BrandNiceClass { Number = number };
    }
}
