using Nhs.Appointments.Persistance.Models;

namespace Nhs.Appointments.Persistance;

public interface ICoreReferenceNumberMigrationDocumentStore
{
    Task<CoreReferenceGroupDocument> Get();
}
