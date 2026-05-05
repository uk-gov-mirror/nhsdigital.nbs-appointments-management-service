using Nhs.Appointments.Core.Concurrency;

namespace Nhs.Appointments.Core.UnitTests.Concurrency;

public class LeaseContextTests
{
    [Fact]
    public void LeaseKeyNotSupplied_ConstructorCalled_ThrowsArgumentNullException()
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
        Action action = () => new LeaseContext("dummyLeaseKey", null);

        // Act - not required.

        // Assert.
        action.Should()
            .Throw<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'release')");
    }

    [Fact]
    public void LeaseContextCreated_LeaseKeyRequested_ReturnsCorrectValue()
    {
        // Arrange.
        Action releaseAction = () => { };
        var randomLeaseKey = Guid.NewGuid().ToString();
        var sut = new LeaseContext(randomLeaseKey, releaseAction);

        // Act - not required.

        // Assert.
        sut.LeaseKey.Should()
            .Be(randomLeaseKey);
    }

    [Fact]
    public void ReleaseActionSet_LeaseContextDisposed_ReleaseActionCalled()
    {
        // Arrange.
        var releaseAction = new Mock<Action>();
        var randomLeaseKey = Guid.NewGuid().ToString();

        // Act.
        using (new LeaseContext(randomLeaseKey, releaseAction.Object)) { };

        // Assert.
        releaseAction.Verify(action => action(), Times.Once());
    }
}
