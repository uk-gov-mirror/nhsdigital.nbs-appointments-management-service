using Microsoft.Extensions.Options;
using Nhs.Appointments.Core.Concurrency;

namespace Nhs.Appointments.Core.UnitTests.Concurrency;

public class InMemoryLeaseManagerTests
{
    [Fact]
    public void LeaseKeySupplied_Acquire_ReturnsLeaseContext()
    {
        // Arrange.
        var options = new Mock<IOptions<LeaseManagerOptions>>();
        var slmo = new LeaseManagerOptions { Timeout = new TimeSpan(1, 0, 0) };
        options.Setup(o => o.Value).Returns(slmo);

        var expectedLeaseKey = Guid.NewGuid().ToString();

        var sut = new InMemoryLeaseManager(options.Object);

        // Act.
        var slc = sut.Acquire(expectedLeaseKey);

        // Assert.
        slc.Should().NotBeNull();
        slc.LeaseKey.Should()
            .Be(expectedLeaseKey);
    }

    [Fact]
    public void SameLeaseKeyCallsConcurrent_Acquire_AttemptToAcquireTheSameLock()
    {
        // Arrange.
        var options = new Mock<IOptions<LeaseManagerOptions>>();
        var slmo = new LeaseManagerOptions { Timeout = new TimeSpan(0, 0, 0, 0, 0, 1) };
        options.Setup(o => o.Value).Returns(slmo);

        var expectedLeaseKey = Guid.NewGuid().ToString();

        var sut = new InMemoryLeaseManager(options.Object);

        // Act.
        var slc1 = sut.Acquire(expectedLeaseKey);
        Action action = () => sut.Acquire(expectedLeaseKey);

        // Assert.
        slc1.LeaseKey.Should()
            .Be(expectedLeaseKey);
        action.Should()
            .Throw<AbandonedMutexException>()
            .WithMessage($"Abandoned attempt to acquire lock for lease key {expectedLeaseKey}");
    }

    [Fact]
    public void SameLeaseKeyCallsButNotConcurrent_Acquire_ReturnsMatchingLeaseContexts()
    {
        // Arrange.
        var options = new Mock<IOptions<LeaseManagerOptions>>();
        var slmo = new LeaseManagerOptions { Timeout = new TimeSpan(0, 0, 0, 0, 0, 1) };
        options.Setup(o => o.Value).Returns(slmo);

        var expectedLeaseKey = Guid.NewGuid().ToString();

        var sut = new InMemoryLeaseManager(options.Object);

        // Act.
        var originalLeaseKey = "";
        using (var slc1 = sut.Acquire(expectedLeaseKey))
        {
            originalLeaseKey = slc1.LeaseKey;
        }
        var slc2 = sut.Acquire(expectedLeaseKey);

        // Assert.
        originalLeaseKey.Should()
            .Be(expectedLeaseKey);
        slc2.LeaseKey.Should()
            .Be(expectedLeaseKey);
    }

    [Fact]
    public void DifferentLeaseKeyCalls_Acquire_ReturnsSeparateLeaseContexts()
    {
        // Arrange.
        var options = new Mock<IOptions<LeaseManagerOptions>>();
        var slmo = new LeaseManagerOptions { Timeout = new TimeSpan(1, 0, 0) };
        options.Setup(o => o.Value).Returns(slmo);

        var expectedLeaseKey1 = Guid.NewGuid().ToString();
        var expectedLeaseKey2 = Guid.NewGuid().ToString();

        var sut = new InMemoryLeaseManager(options.Object);

        // Act.
        var slc1 = sut.Acquire(expectedLeaseKey1);
        var slc2 = sut.Acquire(expectedLeaseKey2);

        // Assert.
        slc1.LeaseKey.Should()
            .Be(expectedLeaseKey1);
        slc2.LeaseKey.Should()
            .Be(expectedLeaseKey2);
        expectedLeaseKey1.Should()
            .NotBe(expectedLeaseKey2);
    }
}
