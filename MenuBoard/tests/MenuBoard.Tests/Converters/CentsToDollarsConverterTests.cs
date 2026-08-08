using System.Globalization;
using MenuBoard.Converters;

namespace MenuBoard.Tests.Converters;

[TestClass]
public class CentsToDollarsConverterTests
{
    private readonly CentsToDollarsConverter _converter = new();

    [TestMethod]
    public void Convert_599_ReturnsDollarString()
    {
        var result = _converter.Convert(599, typeof(string), null!, CultureInfo.InvariantCulture);
        Assert.AreEqual("$5.99", result);
    }

    [TestMethod]
    public void Convert_0_ReturnsZero()
    {
        var result = _converter.Convert(0, typeof(string), null!, CultureInfo.InvariantCulture);
        Assert.AreEqual("$0.00", result);
    }

    [TestMethod]
    public void Convert_1000_ReturnsTenDollars()
    {
        var result = _converter.Convert(1000, typeof(string), null!, CultureInfo.InvariantCulture);
        Assert.AreEqual("$10.00", result);
    }

    [TestMethod]
    public void ConvertBack_DollarString_ReturnsCents()
    {
        var result = _converter.ConvertBack("$5.99", typeof(int), null!, CultureInfo.InvariantCulture);
        Assert.AreEqual(599, result);
    }

    [TestMethod]
    public void ConvertBack_PlainNumber_ReturnsCents()
    {
        var result = _converter.ConvertBack("5.99", typeof(int), null!, CultureInfo.InvariantCulture);
        Assert.AreEqual(599, result);
    }
}
