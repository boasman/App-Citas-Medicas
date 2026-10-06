using CitasMedicas.Web.Modules.AgendaMedica.Persistence;
using CitasMedicas.Web.Modules.CatalogoMedico.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CitasMedicas.Web.Modules.AgendaMedica.Features.RegisterAvailability;

public enum RegisterAvailabilityResult
{
    Success,
    DoctorNotFound,
    StartsAtMustBeFuture,
    Duplicate
}

public sealed class RegisterAvailabilityCommand(CatalogoMedicoDbContext dbContext)
{
    public async Task<RegisterAvailabilityResult> ExecuteAsync(
        int doctorId,
        DateTime startsAt,
        CancellationToken cancellationToken = default)
    {
        if (startsAt <= DateTime.Now)
        {
            return RegisterAvailabilityResult.StartsAtMustBeFuture;
        }

        if (!await dbContext.Doctors.AnyAsync(doctor => doctor.Id == doctorId, cancellationToken))
        {
            return RegisterAvailabilityResult.DoctorNotFound;
        }

        if (await dbContext.AppointmentSlots.AnyAsync(
                slot => slot.DoctorId == doctorId && slot.StartsAt == startsAt,
                cancellationToken))
        {
            return RegisterAvailabilityResult.Duplicate;
        }

        var slot = new AppointmentSlot
        {
            DoctorId = doctorId,
            StartsAt = startsAt,
            IsOccupied = false
        };
        dbContext.AppointmentSlots.Add(slot);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return RegisterAvailabilityResult.Success;
        }
        catch (DbUpdateException)
        {
            dbContext.Entry(slot).State = EntityState.Detached;
            if (await dbContext.AppointmentSlots.AsNoTracking().AnyAsync(
                    item => item.DoctorId == doctorId && item.StartsAt == startsAt,
                    cancellationToken))
            {
                return RegisterAvailabilityResult.Duplicate;
            }

            throw;
        }
    }
}
