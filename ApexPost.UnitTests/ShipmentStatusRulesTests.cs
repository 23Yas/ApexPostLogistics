namespace ApexPost.Tests;

using ApexPost.Core.Application.Services;

public sealed class ShipmentStatusRulesTests
{
    [Theory]
    [InlineData("Created", "PickedUp", true)]
    [InlineData("PickedUp", "InTransit", true)]
    [InlineData("InTransit", "OutForDelivery", true)]
    [InlineData("OutForDelivery", "Delivered", true)]
    [InlineData("OutForDelivery", "Failed", true)]
    [InlineData("Created", "Delivered", false)]
    [InlineData("Delivered", "Created", false)]
    [InlineData("Failed", "InTransit", false)]
    [InlineData("Created", "Teleported", false)]
    public void CanTransition_OnlyAllowsDefinedEdges(string from, string to, bool expected)
    {
        Assert.Equal(expected, ShipmentStatusRules.CanTransition(from, to));
    }

    [Theory]
    [InlineData("Created", true)]
    [InlineData("PickedUp", false)]
    [InlineData("Delivered", false)]
    public void CanDelete_OnlyCreated(string status, bool expected)
    {
        Assert.Equal(expected, ShipmentStatusRules.CanDelete(status));
    }

    [Theory]
    [InlineData("Created")]
    [InlineData("OutForDelivery")]
    public void IsKnownStatus_AcceptsLifecycleValues(string status)
    {
        Assert.True(ShipmentStatusRules.IsKnownStatus(status));
    }

    [Theory]
    [InlineData("Teleported")]
    [InlineData("")]
    [InlineData(null)]
    public void IsKnownStatus_RejectsUnknown(string? status)
    {
        Assert.False(ShipmentStatusRules.IsKnownStatus(status));
    }
}
