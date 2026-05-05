using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Nhs.Appointments.Core.Bookings;
using Nhs.Appointments.Core.Concurrency;
using Nhs.Appointments.Persistance.Models;
using Nhs.Appointments.Persistance.ReferenceGroups;

namespace Nhs.Appointments.Persistance.UnitTests.ReferenceGroups;

public class ReferenceGroupCosmosDocumentStoreTests
{
    private readonly Mock<ITypedDocumentCosmosStore<BookingReferenceGroupDocument>> _cosmosStore = new();
    private readonly Mock<ICoreReferenceGroupMigrationDocumentStore> _migrationStore = new();
    private readonly Mock<ILeaseManager> _leaseManager = new();
    private readonly Mock<IReferenceGroupSelector> _referenceGroupSelector = new();
    private readonly Mock<ILogger<ReferenceGroupCosmosDocumentStore>> _logger = new();
    private readonly string _randomDocumentType = Guid.NewGuid().ToString();
    private readonly Random _random = new();

    private readonly ReferenceGroupCosmosDocumentStore _sut;

    public ReferenceGroupCosmosDocumentStoreTests()
    {
        _cosmosStore.Setup(x => x.GetDocumentType())
            .Returns(_randomDocumentType);

        _sut = new ReferenceGroupCosmosDocumentStore(
            _cosmosStore.Object,
            _migrationStore.Object,
            _leaseManager.Object,
            _referenceGroupSelector.Object,
            _logger.Object
            );
    }

    [Fact]
    public void ImplementsIReferenceNumberDocumentStore()
    {
        // Arrange - not required.

        // Act - not required.

        // Assert.
        typeof(IReferenceNumberDocumentStore).IsAssignableFrom(typeof(ReferenceGroupCosmosDocumentStore))
            .Should().BeTrue();
    }

    [Fact]
    public async Task AssignReferenceGroup_ReturnsReferenceGroupAndUpdatesSiteCount_WhenGroupsAreAlreadyMigrated()
    {
        // Arrange.
        var expectedReferenceGroup = _random.Next(1, ReferenceNumberProvider.NumberOfReferenceGroups + 1);
        var fullListOfReferenceGroupDocuments = GetFakeReferenceGroupDocuments();
        _referenceGroupSelector.Setup(x => x.Select(fullListOfReferenceGroupDocuments))
            .Returns(expectedReferenceGroup.ToString());
        _cosmosStore.Setup(x => x.RunQueryAsync(It.IsAny<Expression<Func<BookingReferenceGroupDocument, bool>>>()))
            .ReturnsAsync(fullListOfReferenceGroupDocuments);

        // Act.
        var referenceGroup = await _sut.AssignReferenceGroup();

        // Assert.
        referenceGroup.Should().Be(expectedReferenceGroup);
        _cosmosStore.Verify(x => x.PatchDocument(
                _randomDocumentType,
                expectedReferenceGroup.ToString(),
                It.Is<PatchOperation>(p => p.Path == BookingReferenceGroupDocument.SiteCountPath)
                ),
            Times.Once()
        );
    }

    [Fact]
    public async Task AssignReferenceGroup_DoesNotMigrateData_WhenGroupsAreAlreadyMigrated()
    {
        // Arrange.
        var expectedReferenceGroup = _random.Next(1, ReferenceNumberProvider.NumberOfReferenceGroups + 1);
        var fullListOfReferenceGroupDocuments = GetFakeReferenceGroupDocuments();
        _referenceGroupSelector.Setup(x => x.Select(fullListOfReferenceGroupDocuments))
            .Returns(expectedReferenceGroup.ToString());
        _cosmosStore.Setup(x => x.RunQueryAsync(It.IsAny<Expression<Func<BookingReferenceGroupDocument, bool>>>()))
            .ReturnsAsync(fullListOfReferenceGroupDocuments);

        // Act.
        await _sut.AssignReferenceGroup();

        // Assert.
        _leaseManager.Verify(lm => lm.Acquire(LeaseKeys.ReferenceGroupKey), Times.Never());
        _cosmosStore.Verify(x => x.WriteAsync(It.IsAny<BookingReferenceGroupDocument>()), Times.Never());
        _logger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, t) =>
                    state.ToString().Contains("Starting migration of reference groups from core container")
                ),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Never()
        );
    }

    [Fact]
    public async Task AssignReferenceGroup_ReturnsReferenceGroupAndUpdatesSiteCount_WhenGroupsAreMigratedDuringTheProcessByAnotherUser()
    {
        // Arrange.
        var expectedReferenceGroup = _random.Next(1, ReferenceNumberProvider.NumberOfReferenceGroups + 1);
        var numberOfCompletedRecords = _random.Next(1, ReferenceNumberProvider.NumberOfReferenceGroups); // Exclusive upper bound means this is never all of them.
        var coreReferenceGroupDocument = GetFakeCoreReferenceGroupDocument(expectedReferenceGroup);
        var migratedListOfReferenceGroupDocuments = GetFakeReferenceGroupDocumentsFromCore(coreReferenceGroupDocument);
        var incompleteListOfReferenceGroupDocuments = migratedListOfReferenceGroupDocuments.Take(numberOfCompletedRecords);

        _referenceGroupSelector
            .Setup(x => x.Select(migratedListOfReferenceGroupDocuments))
            .Returns(expectedReferenceGroup.ToString());
        _cosmosStore
            .SetupSequence
            (x => x.RunQueryAsync(It.IsAny<Expression<Func<BookingReferenceGroupDocument, bool>>>()))
            .ReturnsAsync(incompleteListOfReferenceGroupDocuments) // Denotes the groups are in the process of being migrated
            .ReturnsAsync(migratedListOfReferenceGroupDocuments); // Denotes that the groups are now migrated after the application-wide lock
        _migrationStore
            .Setup(x => x.Get())
            .ReturnsAsync(coreReferenceGroupDocument);

        // Act.
        var referenceGroup = await _sut.AssignReferenceGroup();

        // Assert.
        referenceGroup.Should().Be(expectedReferenceGroup);
        _cosmosStore.Verify(x => x.PatchDocument(
                _randomDocumentType,
                expectedReferenceGroup.ToString(),
                It.Is<PatchOperation>(p => p.Path == BookingReferenceGroupDocument.SiteCountPath)
                ),
            Times.Once()
        );
    }

    [Fact]
    public async Task AssignReferenceGroup_AcquiresApplicationWideLockButDoesNotMigrateData_WhenGroupsAreMigratedDuringTheProcessByAnotherUser()
    {
        // Arrange.
        var expectedReferenceGroup = _random.Next(1, ReferenceNumberProvider.NumberOfReferenceGroups + 1);
        var numberOfCompletedRecords = _random.Next(1, ReferenceNumberProvider.NumberOfReferenceGroups); // Exclusive upper bound means this is never all of them.
        var coreReferenceGroupDocument = GetFakeCoreReferenceGroupDocument(expectedReferenceGroup);
        var migratedListOfReferenceGroupDocuments = GetFakeReferenceGroupDocumentsFromCore(coreReferenceGroupDocument);
        var incompleteListOfReferenceGroupDocuments = migratedListOfReferenceGroupDocuments.Take(numberOfCompletedRecords);

        _referenceGroupSelector
            .Setup(x => x.Select(migratedListOfReferenceGroupDocuments))
            .Returns(expectedReferenceGroup.ToString());
        _cosmosStore
            .SetupSequence
            (x => x.RunQueryAsync(It.IsAny<Expression<Func<BookingReferenceGroupDocument, bool>>>()))
            .ReturnsAsync(incompleteListOfReferenceGroupDocuments) // Denotes the groups are in the process of being migrated
            .ReturnsAsync(migratedListOfReferenceGroupDocuments); // Denotes that the groups are now migrated after the application-wide lock
        _migrationStore
            .Setup(x => x.Get())
            .ReturnsAsync(coreReferenceGroupDocument);

        // Act.
        await _sut.AssignReferenceGroup();

        // Assert.
        _leaseManager.Verify(lm => lm.Acquire(LeaseKeys.ReferenceGroupKey), Times.Once());
        _cosmosStore.Verify(x => x.WriteAsync(It.IsAny<BookingReferenceGroupDocument>()), Times.Never());
    }

    [Fact]
    public async Task AssignReferenceGroup_ReturnsReferenceGroupAndUpdateSiteCount_WhenGroupsAreToBeMigratedByThisUser()
    {
        // Arrange.
        var expectedReferenceGroup = _random.Next(1, ReferenceNumberProvider.NumberOfReferenceGroups + 1);
        var coreReferenceGroupDocument = GetFakeCoreReferenceGroupDocument(expectedReferenceGroup);
        var emptyListOfReferenceGroupDocuments = new List<BookingReferenceGroupDocument>();

        _referenceGroupSelector
            .Setup(x => x.Select(It.IsAny<IEnumerable<BookingReferenceGroupDocument>>())) // Not the migratedList explicitly because it won't be the exact same list.
            .Returns(expectedReferenceGroup.ToString());
        _cosmosStore
            .Setup
            (x => x.RunQueryAsync(It.IsAny<Expression<Func<BookingReferenceGroupDocument, bool>>>()))
            .ReturnsAsync(emptyListOfReferenceGroupDocuments); // Denotes the groups have not yet started being migrated
        _migrationStore
            .Setup(x => x.Get())
            .ReturnsAsync(coreReferenceGroupDocument);

        // Act.
        var referenceGroup = await _sut.AssignReferenceGroup();

        // Assert.
        referenceGroup.Should().Be(expectedReferenceGroup);
        _cosmosStore.Verify(x => x.PatchDocument(
                _randomDocumentType,
                expectedReferenceGroup.ToString(),
                It.Is<PatchOperation>(p => p.Path == BookingReferenceGroupDocument.SiteCountPath)
                ),
            Times.Once()
        );
    }

    [Fact]
    public async Task AssignReferenceGroup_MigratesTheData_WhenGroupsAreToBeMigratedByThisUser()
    {
        // Arrange.
        var expectedReferenceGroup = _random.Next(1, ReferenceNumberProvider.NumberOfReferenceGroups + 1);
        var coreReferenceGroupDocument = GetFakeCoreReferenceGroupDocument(expectedReferenceGroup);
        var emptyListOfReferenceGroupDocuments = new List<BookingReferenceGroupDocument>();

        _referenceGroupSelector
            .Setup(x => x.Select(It.IsAny<IEnumerable<BookingReferenceGroupDocument>>())) // Not the migratedList explicitly because it won't be the exact same list.
            .Returns(expectedReferenceGroup.ToString());
        _cosmosStore
            .Setup
            (x => x.RunQueryAsync(It.IsAny<Expression<Func<BookingReferenceGroupDocument, bool>>>()))
            .ReturnsAsync(emptyListOfReferenceGroupDocuments); // Denotes the groups have not yet started being migrated
        _migrationStore
            .Setup(x => x.Get())
            .ReturnsAsync(coreReferenceGroupDocument);

        // Act.
        await _sut.AssignReferenceGroup();

        // Assert.
        _leaseManager.Verify(lm => lm.Acquire(LeaseKeys.ReferenceGroupKey), Times.Once());
        _cosmosStore.Verify(x => x.WriteAsync(It.IsAny<BookingReferenceGroupDocument>()), Times.Exactly(ReferenceNumberProvider.NumberOfReferenceGroups));
        _logger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, t) =>
                    state.ToString().Contains("Starting migration of reference groups from core container")
                ),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Once()
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
    public async Task GetNextSequenceNumber_ReturnsNextSequenceAndUpdatesSequence_WhenGroupsAreAlreadyMigrated()
    {
        // Arrange.
        var referenceGroup = _random.Next(1, ReferenceNumberProvider.NumberOfReferenceGroups + 1);
        var nextSequence = _random.Next(1, ReferenceNumberProvider.MaxSequenceNumber + 1);
        var referenceGroupDocument = new BookingReferenceGroupDocument 
        {
            Id = referenceGroup.ToString(),
            DocumentType = _randomDocumentType, 
            Sequence = nextSequence, 
            SiteCount = 24 
        };
        _cosmosStore.Setup(x => x.PatchDocument(
                _randomDocumentType,
                referenceGroup.ToString(),
                It.Is<PatchOperation>(p => p.Path == BookingReferenceGroupDocument.SequencePath)
            )
        ).ReturnsAsync(referenceGroupDocument);

        // Act.
        var sequence = await _sut.GetNextSequenceNumber(referenceGroup);

        // Assert.
        sequence.Should().Be(nextSequence);
        _cosmosStore.Verify(x => x.PatchDocument(
                _randomDocumentType,
                referenceGroup.ToString(),
                It.Is<PatchOperation>(p => p.Path == BookingReferenceGroupDocument.SequencePath)
                ),
            Times.Once()
        );
        _logger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, t) =>
                    state.ToString().Contains($"Booking sequence number {nextSequence} requested for {referenceGroup}")
                 ),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Once()
        );
    }

    [Fact]
    public async Task GetNextSequenceNumber_ReturnsNextSequenceAndUpdatesSequence_WhenGroupsAreToBeMigratedByThisUser()
    {
        // Arrange.
        var referenceGroup = _random.Next(1, ReferenceNumberProvider.NumberOfReferenceGroups + 1);
        var nextSequence = _random.Next(1, ReferenceNumberProvider.MaxSequenceNumber + 1);
        var cosmosNotFoundException = new CosmosException("BOOM", System.Net.HttpStatusCode.NotFound, 0, "", 3);
        var emptyListOfReferenceGroupDocuments = new List<BookingReferenceGroupDocument>();
        var coreReferenceGroupDocument = GetFakeCoreReferenceGroupDocument(referenceGroup);
        var referenceGroupDocument = new BookingReferenceGroupDocument
        {
            Id = referenceGroup.ToString(),
            DocumentType = _randomDocumentType,
            Sequence = nextSequence,
            SiteCount = 24
        };
        _cosmosStore
            .SetupSequence(x => x.PatchDocument(
                    _randomDocumentType,
                    referenceGroup.ToString(),
                    It.Is<PatchOperation>(p => p.Path == BookingReferenceGroupDocument.SequencePath)
                )
            )
            .ThrowsAsync(cosmosNotFoundException) // Denotes the groups have not yet started being migrated
            .ReturnsAsync(referenceGroupDocument);
        _cosmosStore
            .Setup(x => x.RunQueryAsync(It.IsAny<Expression<Func<BookingReferenceGroupDocument, bool>>>()))
            .ReturnsAsync(emptyListOfReferenceGroupDocuments); // Denotes the groups have not yet started being migrated
        _migrationStore
            .Setup(x => x.Get())
            .ReturnsAsync(coreReferenceGroupDocument);

        // Act.
        var sequence = await _sut.GetNextSequenceNumber(referenceGroup);

        // Assert.
        sequence.Should().Be(nextSequence);
        _cosmosStore.Verify(x => x.PatchDocument(
                _randomDocumentType,
                referenceGroup.ToString(),
                It.Is<PatchOperation>(p => p.Path == BookingReferenceGroupDocument.SequencePath)
                ),
            Times.Exactly(2) // Before the exception and after the exception.
        );
        _logger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, t) =>
                    state.ToString().Contains($"Booking sequence number {nextSequence} requested for {referenceGroup}")
                 ),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Once()
        );
    }

    [Fact]
    public async Task GetNextSequenceNumber_ReturnsNextSequenceAndUpdatesSequence_WhenGroupsAreMigratedDuringTheProcessByAnotherUser()
    {
        // Arrange.
        var referenceGroup = _random.Next(1, ReferenceNumberProvider.NumberOfReferenceGroups + 1);
        var nextSequence = _random.Next(1, ReferenceNumberProvider.MaxSequenceNumber + 1);
        var cosmosNotFoundException = new CosmosException("BOOM", System.Net.HttpStatusCode.NotFound, 0, "", 3);
        var coreReferenceGroupDocument = GetFakeCoreReferenceGroupDocument(referenceGroup);
        var migratedListOfReferenceGroupDocuments = GetFakeReferenceGroupDocumentsFromCore(coreReferenceGroupDocument);
        var referenceGroupDocument = new BookingReferenceGroupDocument
        {
            Id = referenceGroup.ToString(),
            DocumentType = _randomDocumentType,
            Sequence = nextSequence,
            SiteCount = 24
        };
        _cosmosStore
            .SetupSequence(x => x.PatchDocument(
                    _randomDocumentType,
                    referenceGroup.ToString(),
                    It.Is<PatchOperation>(p => p.Path == BookingReferenceGroupDocument.SequencePath)
                )
            )
            .ThrowsAsync(cosmosNotFoundException) // Denotes the groups have not yet started being migrated
            .ReturnsAsync(referenceGroupDocument);
        _cosmosStore
            .Setup(x => x.RunQueryAsync(It.IsAny<Expression<Func<BookingReferenceGroupDocument, bool>>>()))
            .ReturnsAsync(migratedListOfReferenceGroupDocuments); // Denotes the groups have finished being migrated by the other user after the application-wide lock.

        // Act.
        var sequence = await _sut.GetNextSequenceNumber(referenceGroup);

        // Assert.
        sequence.Should().Be(nextSequence);
        _cosmosStore.Verify(x => x.PatchDocument(
                _randomDocumentType,
                referenceGroup.ToString(),
                It.Is<PatchOperation>(p => p.Path == BookingReferenceGroupDocument.SequencePath)
                ),
            Times.Exactly(2) // Before the exception and after the exception.
        );
        _logger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, t) =>
                    state.ToString().Contains($"Booking sequence number {nextSequence} requested for {referenceGroup}")
                 ),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Once()
        );
    }

    [Fact]
    public async Task GetNextSequenceNumber_AcquiresApplicationWideLockButDoesNotMigrateData_WhenGroupsAreMigratedDuringTheProcessByAnotherUser()
    {
        // Arrange.
        var referenceGroup = _random.Next(1, ReferenceNumberProvider.NumberOfReferenceGroups + 1);
        var nextSequence = _random.Next(1, ReferenceNumberProvider.MaxSequenceNumber + 1);
        var cosmosNotFoundException = new CosmosException("BOOM", System.Net.HttpStatusCode.NotFound, 0, "", 3);
        var coreReferenceGroupDocument = GetFakeCoreReferenceGroupDocument(referenceGroup);
        var migratedListOfReferenceGroupDocuments = GetFakeReferenceGroupDocumentsFromCore(coreReferenceGroupDocument);
        var referenceGroupDocument = new BookingReferenceGroupDocument
        {
            Id = referenceGroup.ToString(),
            DocumentType = _randomDocumentType,
            Sequence = nextSequence,
            SiteCount = 24
        };
        _cosmosStore
            .SetupSequence(x => x.PatchDocument(
                    _randomDocumentType,
                    referenceGroup.ToString(),
                    It.Is<PatchOperation>(p => p.Path == BookingReferenceGroupDocument.SequencePath)
                )
            )
            .ThrowsAsync(cosmosNotFoundException) // Denotes the groups have not yet started being migrated
            .ReturnsAsync(referenceGroupDocument);
        _cosmosStore
            .Setup(x => x.RunQueryAsync(It.IsAny<Expression<Func<BookingReferenceGroupDocument, bool>>>()))
            .ReturnsAsync(migratedListOfReferenceGroupDocuments); // Denotes the groups have finished being migrated by the other user after the application-wide lock.

        // Act.
        var sequence = await _sut.GetNextSequenceNumber(referenceGroup);

        // Assert.
        _leaseManager.Verify(lm => lm.Acquire(LeaseKeys.ReferenceGroupKey), Times.Once());
        _cosmosStore.Verify(x => x.WriteAsync(It.IsAny<BookingReferenceGroupDocument>()), Times.Never());
    }

    [Fact]
    public async Task GetNextSequenceNumber_DoesNotMigrateData_WhenGroupsAreAlreadyMigrated()
    {
        // Arrange.
        var referenceGroup = _random.Next(1, ReferenceNumberProvider.NumberOfReferenceGroups + 1);
        var nextSequence = _random.Next(1, ReferenceNumberProvider.MaxSequenceNumber + 1);
        var referenceGroupDocument = new BookingReferenceGroupDocument
        {
            Id = referenceGroup.ToString(),
            DocumentType = _randomDocumentType,
            Sequence = nextSequence,
            SiteCount = 24
        };
        _cosmosStore.Setup(x => x.PatchDocument(
                _randomDocumentType,
                referenceGroup.ToString(),
                It.Is<PatchOperation>(p => p.Path == BookingReferenceGroupDocument.SequencePath)
            )
        ).ReturnsAsync(referenceGroupDocument);

        // Act.
        var sequence = await _sut.GetNextSequenceNumber(referenceGroup);

        // Assert.
        _leaseManager.Verify(lm => lm.Acquire(LeaseKeys.ReferenceGroupKey), Times.Never());
        _cosmosStore.Verify(x => x.WriteAsync(It.IsAny<BookingReferenceGroupDocument>()), Times.Never());
        _logger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, t) =>
                    state.ToString().Contains("Starting migration of reference groups from core container")
                ),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Never()
        );
    }

    [Fact]
    public async Task GetNextSequenceNumber_MigratesTheData_WhenGroupsAreToBeMigratedByThisUser()
    {
        // Arrange.
        var referenceGroup = _random.Next(1, ReferenceNumberProvider.NumberOfReferenceGroups + 1);
        var nextSequence = _random.Next(1, ReferenceNumberProvider.MaxSequenceNumber + 1);
        var cosmosNotFoundException = new CosmosException("BOOM", System.Net.HttpStatusCode.NotFound, 0, "", 3);
        var emptyListOfReferenceGroupDocuments = new List<BookingReferenceGroupDocument>();
        var coreReferenceGroupDocument = GetFakeCoreReferenceGroupDocument(referenceGroup);
        var referenceGroupDocument = new BookingReferenceGroupDocument
        {
            Id = referenceGroup.ToString(),
            DocumentType = _randomDocumentType,
            Sequence = nextSequence,
            SiteCount = 24
        };
        _cosmosStore
            .SetupSequence(x => x.PatchDocument(
                    _randomDocumentType,
                    referenceGroup.ToString(),
                    It.Is<PatchOperation>(p => p.Path == BookingReferenceGroupDocument.SequencePath)
                )
            )
            .ThrowsAsync(cosmosNotFoundException) // Denotes the groups have not yet started being migrated
            .ReturnsAsync(referenceGroupDocument);
        _cosmosStore
            .Setup(x => x.RunQueryAsync(It.IsAny<Expression<Func<BookingReferenceGroupDocument, bool>>>()))
            .ReturnsAsync(emptyListOfReferenceGroupDocuments); // Denotes the groups have not yet started being migrated
        _migrationStore
            .Setup(x => x.Get())
            .ReturnsAsync(coreReferenceGroupDocument);

        // Act.
        var sequence = await _sut.GetNextSequenceNumber(referenceGroup);

        // Assert.
        _leaseManager.Verify(lm => lm.Acquire(LeaseKeys.ReferenceGroupKey), Times.Once());
        _cosmosStore.Verify(x => x.WriteAsync(It.IsAny<BookingReferenceGroupDocument>()), Times.Exactly(ReferenceNumberProvider.NumberOfReferenceGroups));
        _logger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, t) =>
                    state.ToString().Contains("Starting migration of reference groups from core container")
                ),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Once()
        );
    }

    private static IEnumerable<BookingReferenceGroupDocument> GetFakeReferenceGroupDocuments() 
    {
        var rgd = new List<BookingReferenceGroupDocument>();

        for (var i = 1; i <= ReferenceNumberProvider.NumberOfReferenceGroups; i++)
        {
            rgd.Add(new BookingReferenceGroupDocument
                {
                    Id = i.ToString(),
                    SiteCount = i
                }
            );
        }

        return rgd;
    }

    private static IEnumerable<BookingReferenceGroupDocument> GetFakeReferenceGroupDocumentsFromCore(CoreReferenceGroupDocument coreReferenceGroupDocument)
    {
        var rgd = new List<BookingReferenceGroupDocument>();

        for (var i = 1; i <= ReferenceNumberProvider.NumberOfReferenceGroups; i++)
        {
            rgd.Add(new BookingReferenceGroupDocument
                {
                    Id = i.ToString(),
                    SiteCount = coreReferenceGroupDocument.Groups[i].SiteCount,
                    Sequence = coreReferenceGroupDocument.Groups[i].Sequence
                }
            );
        }

        return rgd;
    }

    private static CoreReferenceGroupDocument GetFakeCoreReferenceGroupDocument(int referenceGroup)
    {
        var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var text = File.ReadAllText(Path.Combine(currentDirectory, "ReferenceGroups", "core_reference_group_document.json"));

        var coreReferenceGroupDocument = JsonSerializer.Deserialize<CoreReferenceGroupDocument>(text);

        foreach (var group in coreReferenceGroupDocument.Groups)
        {
            group.SiteCount = 100;
        }
        coreReferenceGroupDocument.Groups[referenceGroup].SiteCount = 1;

        return coreReferenceGroupDocument;
    }
}
