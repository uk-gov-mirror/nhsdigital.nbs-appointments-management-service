using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Nhs.Appointments.Core.Bookings;
using Nhs.Appointments.Persistance.Models;
using Nhs.Appointments.Persistance.ReferenceGroups;

namespace Nhs.Appointments.Persistance.UnitTests.ReferenceGroups;

public class CoreReferenceGroupCosmosDocumentStoreTests
{
    private readonly Mock<ITypedDocumentCosmosStore<CoreReferenceGroupDocument>> _cosmosStore = new();
    private readonly Mock<IOptions<ReferenceGroupOptions>> _options = new();
    private readonly string _randomDocumentType = Guid.NewGuid().ToString();
    private readonly Random _random = new();

    private readonly CoreReferenceGroupCosmosDocumentStore _sut;

    public CoreReferenceGroupCosmosDocumentStoreTests()
    {
        _cosmosStore.Setup(x => x.GetDocumentType())
            .Returns(_randomDocumentType);

        _sut = new CoreReferenceGroupCosmosDocumentStore(
            _cosmosStore.Object,
            _options.Object
            );
    }

    [Fact]
    public void ImplementsIReferenceNumberDocumentStore()
    {
        // Arrange - not required.

        // Act - not required.

        // Assert.
        typeof(IReferenceNumberDocumentStore).IsAssignableFrom(typeof(CoreReferenceGroupCosmosDocumentStore))
            .Should().BeTrue();
    }

    [Fact]
    public void ImplementsICoreReferenceGroupMigrationDocumentStore()
    {
        // Arrange - not required.

        // Act - not required.

        // Assert.
        typeof(ICoreReferenceGroupMigrationDocumentStore).IsAssignableFrom(typeof(CoreReferenceGroupCosmosDocumentStore))
            .Should().BeTrue();
    }

    [Fact]
    public async Task AssignReferenceGroup_ReturnsReferenceGroup()
    {
        // Arrange.
        var expectedReferenceGroup = _random.Next(1, ReferenceNumberProvider.NumberOfReferenceGroups + 1);
        var coreReferenceGroupDocument = GetFakeCoreReferenceGroupDocument(expectedReferenceGroup);
        _cosmosStore.Setup(x => x.GetByIdAsync(CoreReferenceGroupCosmosDocumentStore.DocumentId))
            .ReturnsAsync(coreReferenceGroupDocument);

        // Act.
        var referenceGroup = await _sut.AssignReferenceGroup();

        // Assert.
        referenceGroup.Should().Be(expectedReferenceGroup);
    }

    [Fact]
    public async Task AssignReferenceGroup_UpdatesSiteCountForReferenceGroup()
    {
        // Arrange.
        var expectedReferenceGroup = _random.Next(1, ReferenceNumberProvider.NumberOfReferenceGroups + 1);
        var coreReferenceGroupDocument = GetFakeCoreReferenceGroupDocument(expectedReferenceGroup);
        _cosmosStore.Setup(x => x.GetByIdAsync(CoreReferenceGroupCosmosDocumentStore.DocumentId))
            .ReturnsAsync(coreReferenceGroupDocument);

        // Act.
        var referenceGroup = await _sut.AssignReferenceGroup();

        // Assert.
        _cosmosStore.Verify(x => x.PatchDocument(
                _randomDocumentType, 
                CoreReferenceGroupCosmosDocumentStore.DocumentId, 
                It.Is<PatchOperation>(p => p.Path == CoreReferenceGroupDocument.SiteCountPath(expectedReferenceGroup))
                ),
            Times.Once()
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-99)]
    [InlineData(-100)]
    [InlineData(100)]
    [InlineData(101)]
    public async Task GetNextSequenceNumber_ThrowsArgumentOutOfRangeException_WhenReferenceGroupIsInvalid(int referenceGroup)
    {
        // Arrange.
        Func<Task> action = async () => await _sut.GetNextSequenceNumber(referenceGroup);

        // Act - not required.

        // Assert.
        await action.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task GetNextSequenceNumber_ReturnsNextSequenceForReferenceGroup()
    {
        // Arrange.
        var referenceGroup = _random.Next(1, ReferenceNumberProvider.NumberOfReferenceGroups + 1);
        var nextSequence = _random.Next(1, ReferenceNumberProvider.MaxSequenceNumber + 1);
        var coreReferenceGroupDocument = GetFakeCoreReferenceGroupDocument(referenceGroup, nextSequence);
        _cosmosStore.Setup(x => x.PatchDocument(
                _randomDocumentType,
                CoreReferenceGroupCosmosDocumentStore.DocumentId,
                It.Is<PatchOperation>(p => p.Path == CoreReferenceGroupDocument.SequencePath(referenceGroup))
            )
        ).ReturnsAsync(coreReferenceGroupDocument);

        // Act.
        var sequence = await _sut.GetNextSequenceNumber(referenceGroup);

        // Assert.
        sequence.Should().Be(nextSequence);
    }

    [Fact]
    public async Task GetNextSequenceNumber_UpdatesSequenceForReferenceGroup()
    {
        // Arrange.
        var referenceGroup = _random.Next(1, ReferenceNumberProvider.NumberOfReferenceGroups + 1);
        var nextSequence = _random.Next(1, ReferenceNumberProvider.MaxSequenceNumber + 1);
        var coreReferenceGroupDocument = GetFakeCoreReferenceGroupDocument(referenceGroup, nextSequence);
        _cosmosStore.Setup(x => x.PatchDocument(
                _randomDocumentType,
                CoreReferenceGroupCosmosDocumentStore.DocumentId,
                It.Is<PatchOperation>(p => p.Path == CoreReferenceGroupDocument.SequencePath(referenceGroup))
            )
        ).ReturnsAsync(coreReferenceGroupDocument);

        // Act.
        var sequence = await _sut.GetNextSequenceNumber(referenceGroup);

        // Assert.
        _cosmosStore.Verify(x => x.PatchDocument(
                _randomDocumentType,
                CoreReferenceGroupCosmosDocumentStore.DocumentId,
                It.Is<PatchOperation>(p => p.Path == CoreReferenceGroupDocument.SequencePath(referenceGroup))
                ),
            Times.Once()
        );
    }

    private static CoreReferenceGroupDocument GetFakeCoreReferenceGroupDocument(int referenceGroup, int sequence = 0)
    {
        var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var text = File.ReadAllText(Path.Combine(currentDirectory, "ReferenceGroups", "core_reference_group_document.json"));

        var coreReferenceGroupDocument = JsonSerializer.Deserialize<CoreReferenceGroupDocument>(text);

        foreach (var group in coreReferenceGroupDocument.Groups)
        {
            group.SiteCount = 50;
        }
        coreReferenceGroupDocument.Groups[referenceGroup].SiteCount = 1;
        if (sequence > 0)
        {
            coreReferenceGroupDocument.Groups[referenceGroup].Sequence = sequence;
        }

        return coreReferenceGroupDocument;
    }
}
