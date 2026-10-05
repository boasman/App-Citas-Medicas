using CitasMedicas.Web.Modules.CatalogoMedico.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CitasMedicas.Web.Modules.Reservas.Features.BookAppointment;

public sealed class GetAppointmentConfirmationQuery(CatalogoMedicoDbContext dbContext)
{
    public Task<AppointmentConfirmationViewModel?> ExecuteAsync(int reservationId, CancellationToken cancellationToken = default)
    {
        return dbContext.AppointmentReservations.AsNoTracking()
            .Where(reservation => reservation.Id == reservationId)
            .Select(reservation => new AppointmentConfirmationViewModel(
                reservation.PatientName,
                reservation.PatientEmail,
                reservation.AppointmentSlot.Doctor.Name,
                reservation.AppointmentSlot.Doctor.SpecialtyName,
                reservation.AppointmentSlot.StartsAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
