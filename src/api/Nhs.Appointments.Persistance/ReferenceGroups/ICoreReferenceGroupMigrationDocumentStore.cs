using Nhs.Appointments.Persistance.Models;

namespace Nhs.Appointments.Persistance.ReferenceGroups;

public interface ICoreReferenceGroupMigrationDocumentStore
{
    Task<CoreReferenceGroupDocument> Get();
}
