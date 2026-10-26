using MediCubes.Web.Models;

namespace MediCubes.Tests;

public class MedicineTests
{
    [Theory]
    [InlineData(10, 20, true)]
    [InlineData(20, 20, true)]
    [InlineData(21, 20, false)]
    public void IsLowStock_compares_quantity_with_reorder_level(int quantity, int reorderLevel, bool expected)
    {
        var medicine = new Medicine { Quantity = quantity, ReorderLevel = reorderLevel };

        Assert.Equal(expected, medicine.IsLowStock);
    }

    [Fact]
    public void IsExpired_is_true_only_after_the_expiry_date()
    {
        var medicine = new Medicine { ExpiryDate = new DateTime(2026, 10, 1) };

        Assert.False(medicine.IsExpired(new DateTime(2026, 10, 1)));
        Assert.True(medicine.IsExpired(new DateTime(2026, 10, 2)));
    }
}
