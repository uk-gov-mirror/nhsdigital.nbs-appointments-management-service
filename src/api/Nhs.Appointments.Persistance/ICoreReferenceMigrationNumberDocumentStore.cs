using Nhs.Appointments.Persistance.Models;

namespace Nhs.Appointments.Persistance;

public interface ICoreReferenceMigrationNumberDocumentStore
{
    Task<CoreReferenceGroupDocument> Get();
}
