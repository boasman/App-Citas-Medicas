using CitasMedicas.Web.Modules.CatalogoMedico.Persistence;
using CitasMedicas.Web.Modules.Reservas.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CitasMedicas.Web.Modules.Reservas.Features.BookAppointment;

public sealed class BookAppointmentCommand(CatalogoMedicoDbContext dbContext)
{
    public async Task<int?> ExecuteAsync(int doctorId, int slotId, string patientName, string patientEmail, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var now = DateTime.Now;
        var claimedSlots = await dbContext.AppointmentSlots
            .Where(slot => slot.Id == slotId && slot.DoctorId == doctorId && !slot.IsOccupied && slot.StartsAt >= now)
            .ExecuteUpdateAsync(update => update.SetProperty(slot => slot.IsOccupied, true), cancellationToken);

        if (claimedSlots != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var reservation = new AppointmentReservation
        {
            AppointmentSlotId = slotId,
            PatientName = patientName.Trim(),
            PatientEmail = patientEmail.Trim(),
            BookedAt = now
        };
        dbContext.AppointmentReservations.Add(reservation);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return reservation.Id;
    }
}
