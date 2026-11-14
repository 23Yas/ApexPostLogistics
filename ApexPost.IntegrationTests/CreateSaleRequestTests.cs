namespace ApexPost.Tests;

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using ApexPost.Web.Models.Requests;

public sealed class CreateSaleRequestTests
{
    [Theory]
    [InlineData("Id")]
    [InlineData("Status")]
    [InlineData("ClerkId")]
    [InlineData("SubTotal")]
    [InlineData("TaxTotal")]
    [InlineData("Total")]
    [InlineData("CreatedAtUtc")]
    [InlineData("ModifiedAtUtc")]
    [InlineData("CreatedBy")]
    [InlineData("ModifiedBy")]
    public void Sale_request_has_no_property_for_protected_field(string name)
    {
        var property = typeof(CreateSaleRequest).GetProperty(
            name,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        Assert.Null(property);
    }

    [Fact]
    public void Quantity_below_one_fails_validation()
    {
        var request = new CreateSaleRequest { ProductSku = "SKU-1001", Quantity = 0, PaymentMethod = "Cash" };

        var errors = Validate(request);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CreateSaleRequest.Quantity)));
    }

    [Fact]
    public void Missing_product_fails_validation()
    {
        var request = new CreateSaleRequest { ProductSku = "", Quantity = 1, PaymentMethod = "Cash" };

        var errors = Validate(request);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CreateSaleRequest.ProductSku)));
    }

    private static List<ValidationResult> Validate(CreateSaleRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }
}