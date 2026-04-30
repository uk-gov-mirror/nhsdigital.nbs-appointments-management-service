using Microsoft.Extensions.Options;
using Nhs.Appointments.Core.Concurrency;

namespace Nhs.Appointments.Core.UnitTests.Concurrency;

public class InMemoryLeaseManagerTests
{
    [Fact]
    public void ModeIsCorrect()
    {
        var slmo = new LeaseManagerOptions { Timeout = new TimeSpan(1, 0, 0), Realm = "Test" };
        var options = new Mock<IOptions<LeaseManagerOptions>>();
        options.Setup(o => o.Value).Returns(slmo);

        var sut = new InMemoryLeaseManager(options.Object);

        sut.Mode.Should().Be(LeaseManagerMode.InMemory);
    }
    
    [Fact]
    public void LeaseKeySupplied_Acquire_ReturnsLeaseContext()
    {
        // Arrange.
        var options = new Mock<IOptions<LeaseManagerOptions>>();
        var slmo = new LeaseManagerOptions { Timeout = new TimeSpan(1, 0, 0), Realm = "test"};
        options.Setup(o => o.Value).Returns(slmo);

        var leaseKey = Guid.NewGuid().ToString();
        var expectedLeaseKey = $"{slmo.Realm}_{leaseKey}";

        var sut = new InMemoryLeaseManager(options.Object);

        // Act.
        var slc = sut.Acquire(leaseKey);

        // Assert.
        slc.Should().NotBeNull();
        slc.LeaseKey.Should()
            .Be(expectedLeaseKey);
    }
    
    [Fact]
    public async Task LeaseKeySupplied_AcquireAsync_ReturnsLeaseContext()
    {
        // Arrange.
        var options = new Mock<IOptions<LeaseManagerOptions>>();
        var slmo = new LeaseManagerOptions { Timeout = new TimeSpan(1, 0, 0), Realm = "test"};
        options.Setup(o => o.Value).Returns(slmo);

        var leaseKey = Guid.NewGuid().ToString();
        var expectedLeaseKey = $"{slmo.Realm}_{leaseKey}";

        var sut = new InMemoryLeaseManager(options.Object);

        // Act.
        var slc = await sut.AcquireAsync(leaseKey);

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
        var slmo = new LeaseManagerOptions { Timeout = new TimeSpan(0, 0, 0, 0, 0, 1), Realm = "test" };
        options.Setup(o => o.Value).Returns(slmo);

        var leaseKey = Guid.NewGuid().ToString();
        var expectedLeaseKey = $"{slmo.Realm}_{leaseKey}";

        var sut = new InMemoryLeaseManager(options.Object);

        // Act.
        var slc1 = sut.Acquire(leaseKey);
        Action action = () => sut.Acquire(leaseKey);

        // Assert.
        slc1.LeaseKey.Should()
            .Be(expectedLeaseKey);
        action.Should()
            .Throw<AbandonedMutexException>()
            .WithMessage($"Abandoned attempt to acquire lock for lease key {expectedLeaseKey}");
    }
    
    [Fact]
    public async Task SameLeaseKeyCallsConcurrent_AcquireAsync_AttemptToAcquireTheSameLock()
    {
        // Arrange.
        var options = new Mock<IOptions<LeaseManagerOptions>>();
        var slmo = new LeaseManagerOptions { Timeout = new TimeSpan(0, 0, 0, 0, 0, 1), Realm = "test" };
        options.Setup(o => o.Value).Returns(slmo);

        var leaseKey = Guid.NewGuid().ToString();
        var expectedLeaseKey = $"{slmo.Realm}_{leaseKey}";

        var sut = new InMemoryLeaseManager(options.Object);

        // Act.
        var slc1 = await sut.AcquireAsync(leaseKey);
        Task action = sut.AcquireAsync(leaseKey);

        // Assert.
        slc1.LeaseKey.Should()
            .Be(expectedLeaseKey);
        var exception = await Assert.ThrowsAsync<AbandonedMutexException>(() => action);
        exception.Message.Should().BeEquivalentTo($"Abandoned attempt to acquire lock for lease key {expectedLeaseKey}");
    }

    [Fact]
    public void SameLeaseKeyCallsButNotConcurrent_Acquire_ReturnsMatchingLeaseContexts()
    {
        // Arrange.
        var options = new Mock<IOptions<LeaseManagerOptions>>();
        var slmo = new LeaseManagerOptions { Timeout = new TimeSpan(0, 0, 0, 0, 0, 1), Realm = "test" };
        options.Setup(o => o.Value).Returns(slmo);

        var leaseKey = Guid.NewGuid().ToString();
        var expectedLeaseKey = $"{slmo.Realm}_{leaseKey}";

        var sut = new InMemoryLeaseManager(options.Object);

        // Act.
        var originalLeaseKey = "";
        using (var slc1 = sut.Acquire(leaseKey))
        {
            originalLeaseKey = slc1.LeaseKey;
        }
        var slc2 = sut.Acquire(leaseKey);

        // Assert.
        originalLeaseKey.Should()
            .Be(expectedLeaseKey);
        slc2.LeaseKey.Should()
            .Be(expectedLeaseKey);
    }
    
    [Fact]
    public async Task SameLeaseKeyCallsButNotConcurrent_AcquireAsync_ReturnsMatchingLeaseContexts()
    {
        // Arrange.
        var options = new Mock<IOptions<LeaseManagerOptions>>();
        var slmo = new LeaseManagerOptions { Timeout = new TimeSpan(0, 0, 0, 0, 0, 1), Realm = "test" };
        options.Setup(o => o.Value).Returns(slmo);

        var leaseKey = Guid.NewGuid().ToString();
        var expectedLeaseKey = $"{slmo.Realm}_{leaseKey}";

        var sut = new InMemoryLeaseManager(options.Object);

        // Act.
        var originalLeaseKey = "";
        using (var slc1 = await sut.AcquireAsync(leaseKey))
        {
            originalLeaseKey = slc1.LeaseKey;
        }
        var slc2 = await sut.AcquireAsync(leaseKey);

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
        var slmo = new LeaseManagerOptions { Timeout = new TimeSpan(1, 0, 0), Realm = "test"};
        options.Setup(o => o.Value).Returns(slmo);

        var leaseKey1 = Guid.NewGuid().ToString();
        var leaseKey2 = Guid.NewGuid().ToString();

        var expectedLeaseKey1 = $"{slmo.Realm}_{leaseKey1}";
        var expectedLeaseKey2 = $"{slmo.Realm}_{leaseKey2}";

        var sut = new InMemoryLeaseManager(options.Object);

        // Act.
        var slc1 = sut.Acquire(leaseKey1);
        var slc2 = sut.Acquire(leaseKey2);

        // Assert.
        slc1.LeaseKey.Should()
            .Be(expectedLeaseKey1);
        slc2.LeaseKey.Should()
            .Be(expectedLeaseKey2);
        expectedLeaseKey1.Should()
            .NotBe(expectedLeaseKey2);
    }

    [Fact]
    public async Task DifferentLeaseKeyCalls_AcquireAsync_ReturnsSeparateLeaseContexts()
    {
        // Arrange.
        var options = new Mock<IOptions<LeaseManagerOptions>>();
        var slmo = new LeaseManagerOptions { Timeout = new TimeSpan(1, 0, 0), Realm = "test"};
        options.Setup(o => o.Value).Returns(slmo);

        var leaseKey1 = Guid.NewGuid().ToString();
        var leaseKey2 = Guid.NewGuid().ToString();

        var expectedLeaseKey1 = $"{slmo.Realm}_{leaseKey1}";
        var expectedLeaseKey2 = $"{slmo.Realm}_{leaseKey2}";

        var sut = new InMemoryLeaseManager(options.Object);

        // Act.
        var slc1 = await sut.AcquireAsync(leaseKey1);
        var slc2 = await sut.AcquireAsync(leaseKey2);

        // Assert.
        slc1.LeaseKey.Should()
            .Be(expectedLeaseKey1);
        slc2.LeaseKey.Should()
            .Be(expectedLeaseKey2);
        expectedLeaseKey1.Should()
            .NotBe(expectedLeaseKey2);
    }
}
