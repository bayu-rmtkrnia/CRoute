using CRoute.Core.Models;

namespace CRoute.Tests.Core;

public class TransportModeTests
{
    private static TransportMode CreateMode(double factor, string name = "Mobil", string icon = "car") => new()
    {
        Name = name,
        EmissionFactor = factor,
        IconName = icon,
    };

    [Theory]
    [InlineData(0.171, 1.71)]
    [InlineData(0.103, 1.03)]
    [InlineData(0.089, 0.89)]
    [InlineData(0.041, 0.41)]
    [InlineData(0.000, 0.00)]
    public void CalculateEmission_Untuk10Km_MenghasilkanFaktorDikaliJarak(double factor, double expected)
    {
        TransportMode mode = CreateMode(factor);

        double actual = mode.CalculateEmission(10);

        Assert.Equal(expected, actual, 10);
    }

    [Fact]
    public void CalculateEmission_JarakNol_MenghasilkanNol()
    {
        TransportMode mode = CreateMode(0.171);

        Assert.Equal(0, mode.CalculateEmission(0));
    }

    [Fact]
    public void CalculateEmission_JarakNegatif_MelemparArgumentOutOfRange()
    {
        TransportMode mode = CreateMode(0.171);

        Assert.Throws<ArgumentOutOfRangeException>(() => mode.CalculateEmission(-1));
    }

    [Fact]
    public void CalculateEmission_JarakNaN_MelemparArgumentException()
    {
        TransportMode mode = CreateMode(0.171);

        Assert.Throws<ArgumentException>(() => mode.CalculateEmission(double.NaN));
    }

    [Fact]
    public void EmissionFactor_Negatif_MelemparArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateMode(-0.1));
    }

    [Fact]
    public void Name_Kosong_MelemparArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CreateMode(0.171, name: " "));
    }

    [Fact]
    public void IconName_Kosong_MelemparArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CreateMode(0.171, icon: ""));
    }
}
