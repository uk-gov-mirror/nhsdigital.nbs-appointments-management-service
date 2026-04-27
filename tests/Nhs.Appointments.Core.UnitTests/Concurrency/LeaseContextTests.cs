using Nhs.Appointments.Core.Concurrency;

namespace Nhs.Appointments.Core.UnitTests.Concurrency;

public class LeaseContextTests
{
    [Fact]
    public void SiteKeyNotSupplied_ConstructorCalled_ThrowsArgumentNullException()
    {
        // Arrange.
        Action releaseAction = () => { };

        Action action = () => new LeaseContext("", releaseAction);

        // Act - not required.

        // Assert.
        action.Should()
            .Throw<ArgumentException>()
            .WithMessage("The value cannot be an empty string. (Parameter 'leaseKey')");
    }

    [Fact]
    public void ReleaseActionNotSupplied_ConstructorCalled_ThrowsArgumentNullException()
    {
        // Arrange.
        Action action = () => new LeaseContext("siteKey", null);

        // Act - not required.

        // Assert.
        action.Should()
            .Throw<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'release')");
    }

    [Fact]
    public void SiteLeaseContextCreated_SiteKeyRequested_ReturnsCorrectValue()
    {
        // Arrange.
        Action releaseAction = () => { };
        var randomSiteKey = Guid.NewGuid().ToString();
        var sut = new LeaseContext(randomSiteKey, releaseAction);

        // Act - not required.

        // Assert.
        sut.LeaseKey.Should()
            .Be(randomSiteKey);
    }

    [Fact]
    public void ReleaseActionSet_SiteLeaseContextDisposed_ReleaseActionCalled()
    {
        // Arrange.
        var releaseAction = new Mock<Action>();
        var randomSiteKey = Guid.NewGuid().ToString();

        // Act.
        using (new LeaseContext(randomSiteKey, releaseAction.Object)) { };

        // Assert.
        releaseAction.Verify(action => action(), Times.Once());
    }
}
