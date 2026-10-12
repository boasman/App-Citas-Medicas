using CitasMedicas.Web.Modules.CatalogoMedico.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CitasMedicas.Web.Modules.AgendaMedica.Features.RegisterAvailability;

public sealed record RegisteredAvailabilityViewModel(DateTime StartsAt, bool IsOccupied);

public sealed class GetDoctorRegisteredAvailabilityQuery(CatalogoMedicoDbContext dbContext)
{
    public async Task<IReadOnlyList<RegisteredAvailabilityViewModel>?> ExecuteAsync(
        int doctorId,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Doctors.AsNoTracking().AnyAsync(doctor => doctor.Id == doctorId, cancellationToken))
        {
            return null;
        }

        return await dbContext.AppointmentSlots.AsNoTracking()
            .Where(slot => slot.DoctorId == doctorId && slot.StartsAt > DateTime.Now)
            .OrderBy(slot => slot.StartsAt)
            .Select(slot => new RegisteredAvailabilityViewModel(slot.StartsAt, slot.IsOccupied))
            .ToListAsync(cancellationToken);
    }
}
