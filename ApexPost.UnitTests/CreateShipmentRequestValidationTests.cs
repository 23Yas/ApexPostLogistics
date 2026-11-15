namespace ApexPost.UnitTests;

using System.ComponentModel.DataAnnotations;
using ApexPost.Api.DTOs;

/// <summary>
/// Parameterized validation tests for the create-shipment DTO contract
/// (tracking format APX-XXXXXXXX, weight range 0.1-70 kg).
/// </summary>
public sealed class CreateShipmentRequestValidationTests
{
    private static CreateShipmentRequest Valid() => new()
    {
        TrackingNumber = "APX-12345678",
        WeightKg = 2.5m,
        RecipientName = "Jane Doe",
        RecipientPhone = "+15551234567",
        RecipientAddress = "123 Main St",
        RecipientCity = "Springfield",
        Price = 15.99m
    };

    private static bool IsValid(CreateShipmentRequest request)
        => Validator.TryValidateObject(request, new ValidationContext(request), null, true);

    [Theory]
    [InlineData("APX-12345678", true)]
    [InlineData("APX-00000000", true)]
    [InlineData("APX-1234567", false)]   // 7 digits
    [InlineData("APX-123456789", false)] // 9 digits
    [InlineData("apx-12345678", false)]  // lowercase prefix
    [InlineData("XYZ-12345678", false)]  // wrong prefix
    [InlineData("APX-1234567A", false)]  // non-digit
    [InlineData("", false)]
    public void TrackingNumber_FormatRule(string tracking, bool expected)
    {
        var request = Valid();
        // TrackingNumber is init-only; rebuild via object initializer on a copy.
        request = new CreateShipmentRequest
        {
            TrackingNumber = tracking,
            WeightKg = request.WeightKg,
            RecipientName = request.RecipientName,
            RecipientPhone = request.RecipientPhone,
            RecipientAddress = request.RecipientAddress,
            RecipientCity = request.RecipientCity,
            Price = request.Price
        };

        Assert.Equal(expected, IsValid(request));
    }

    [Theory]
    [InlineData(0.1, true)]
    [InlineData(1.0, true)]
    [InlineData(70.0, true)]
    [InlineData(0.0, false)]
    [InlineData(0.09, false)]
    [InlineData(70.01, false)]
    [InlineData(100.0, false)]
    public void WeightKg_RangeRule(decimal weight, bool expected)
    {
        var request = new CreateShipmentRequest
        {
            TrackingNumber = "APX-12345678",
            WeightKg = weight,
            RecipientName = "Jane Doe",
            RecipientPhone = "+15551234567",
            RecipientAddress = "123 Main St",
            RecipientCity = "Springfield",
            Price = 15.99m
        };

        Assert.Equal(expected, IsValid(request));
    }
}
