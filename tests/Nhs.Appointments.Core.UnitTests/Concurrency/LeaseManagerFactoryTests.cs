using Nhs.Appointments.Core.Concurrency;

namespace Nhs.Appointments.Core.UnitTests.Concurrency;

public class LeaseManagerFactoryTests
{
    private readonly Mock<ILeaseManager> _leaseManagerInMemory = new();
    private readonly Mock<ILeaseManager> _leaseManagerDistributed = new();
    
    public LeaseManagerFactoryTests()
    {
        _leaseManagerInMemory.Setup(x => x.Mode).Returns(LeaseManagerMode.InMemory);
        _leaseManagerDistributed.Setup(x => x.Mode).Returns(LeaseManagerMode.DistributedAzureBlob);
    }
    
    [Fact]
    public void Default_Create_InMemoryLeaseManager_WhenNoDistributed()
    {
        var sut = new LeaseManagerFactory(
        [
            _leaseManagerInMemory.Object
        ]);
        
        sut.Create().Mode.Should().Be(LeaseManagerMode.InMemory);   
    }
    
    [Fact]
    public void Default_Create_Distributed_WhenAllRegistered()
    {
        var sut = new LeaseManagerFactory(
            [
                _leaseManagerInMemory.Object,
                _leaseManagerDistributed.Object,
            ]);
        
        sut.Create().Mode.Should().Be(LeaseManagerMode.DistributedAzureBlob);   
    }
    
    [Fact]
    public void Default_Create_Distributed_WhenJustDistributed()
    {
        var sut = new LeaseManagerFactory(
        [
                _leaseManagerDistributed.Object,
            ]);
        
        sut.Create().Mode.Should().Be(LeaseManagerMode.DistributedAzureBlob);   
    }
    
    [Fact]
    public void Default_Create_Throws_WhenEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(() => new LeaseManagerFactory([]));   
        exception.Message.Should().Be("No lease managers have been registered");  
    }
    
    [Theory]
    [InlineData(LeaseManagerMode.InMemory)]
    [InlineData(LeaseManagerMode.DistributedAzureBlob)]
    public void Mode_Create_BringsBackRequest(LeaseManagerMode mode)
    {
        var sut = new LeaseManagerFactory(
        [
                _leaseManagerDistributed.Object,
                _leaseManagerInMemory.Object,
            ]);
        
        sut.Create(mode).Mode.Should().Be(mode);   
    }
    
    [Fact]
    public void Mode_Create_Throws_WhenNotFound()
    {
        var sut = new LeaseManagerFactory(
            [
                _leaseManagerInMemory.Object,
            ]);
        
        var exception = Assert.Throws<ArgumentException>(() => sut.Create(LeaseManagerMode.DistributedAzureBlob));   
        exception.Message.Should().Be($"No lease manager found for mode: {LeaseManagerMode.DistributedAzureBlob}");   
    }
}
