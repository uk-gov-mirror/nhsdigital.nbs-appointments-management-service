using Nhs.Appointments.Core.Concurrency;

namespace Nhs.Appointments.Core.UnitTests.Concurrency;

public class LeaseManagerFactoryTests
{
    private readonly Mock<ILeaseManager> _leaseManagerInMemory = new();
    private readonly Mock<ILeaseManager> _leaseManagerDistributed = new();
    private readonly Mock<ILeaseManager> _leaseManagerRandom = new();
    
    public LeaseManagerFactoryTests()
    {
        _leaseManagerInMemory.Setup(x => x.Mode).Returns(LeaseManagerMode.InMemory);
        _leaseManagerDistributed.Setup(x => x.Mode).Returns(LeaseManagerMode.DistributedAzureBlob);
        _leaseManagerRandom.Setup(x => x.Mode).Returns("Random");
    }
    
    [Fact]
    public void Default_Create_InMemoryLeaseManager_WhenNoDistributed()
    {
        var sut = new LeaseManagerFactory(
            new List<ILeaseManager>
            {
                _leaseManagerInMemory.Object
            });
        
        sut.Create().Mode.Should().Be(LeaseManagerMode.InMemory);   
    }
    
    [Fact]
    public void Default_Create_Distributed_WhenAllRegistered()
    {
        var sut = new LeaseManagerFactory(
            new List<ILeaseManager>
            {
                _leaseManagerInMemory.Object,
                _leaseManagerDistributed.Object,
            });
        
        sut.Create().Mode.Should().Be(LeaseManagerMode.DistributedAzureBlob);   
    }
    
    [Fact]
    public void Default_Create_Distributed_WhenJustDistributed()
    {
        var sut = new LeaseManagerFactory(
            new List<ILeaseManager>
            {
                _leaseManagerDistributed.Object,
            });
        
        sut.Create().Mode.Should().Be(LeaseManagerMode.DistributedAzureBlob);   
    }
    
    [Fact]
    public void Default_Create_Throws_WhenEmpty()
    {
        var sut = new LeaseManagerFactory(
            new List<ILeaseManager>());
        
        var exception = Assert.Throws<ArgumentException>(() => sut.Create());   
        exception.Message.Should().Be("No default lease manager found");   
    }
    
    [Fact]
    public void Default_Create_Throws_WhenNothingDefault()
    {
        var sut = new LeaseManagerFactory(
            new List<ILeaseManager>()
            {
                _leaseManagerRandom.Object,
            });
        
        var exception = Assert.Throws<ArgumentException>(() => sut.Create());   
        exception.Message.Should().Be("No default lease manager found");   
    }
    
    [Theory]
    [InlineData(LeaseManagerMode.InMemory)]
    [InlineData(LeaseManagerMode.DistributedAzureBlob)]
    [InlineData("Random")]
    public void Mode_Create_BringsBackRequest(string mode)
    {
        var sut = new LeaseManagerFactory(
            new List<ILeaseManager>()
            {
                _leaseManagerRandom.Object,
                _leaseManagerDistributed.Object,
                _leaseManagerInMemory.Object,
            });
        
        sut.Create(mode).Mode.Should().Be(mode);   
    }
    
    [Theory]
    [InlineData("anything")]
    [InlineData("*")]
    [InlineData("something")]
    public void Mode_Create_Throws_WhenNotFound(string mode)
    {
        var sut = new LeaseManagerFactory(
            new List<ILeaseManager>()
            {
                _leaseManagerRandom.Object,
                _leaseManagerDistributed.Object,
                _leaseManagerInMemory.Object,
            });
        
        var exception = Assert.Throws<ArgumentException>(() => sut.Create(mode));   
        exception.Message.Should().Be($"No lease manager found for mode: {mode}");   
    }
}
