using CitasMedicas.Web.Modules.CatalogoMedico.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CitasMedicas.Web.Modules.Reservas.Features.GetPatientAppointments;

public sealed class GetPatientAppointmentsQuery(CatalogoMedicoDbContext dbContext)
{
    public async Task<IReadOnlyList<PatientAppointmentViewModel>> ExecuteAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToUpper();
        var now = DateTime.Now;

        return await dbContext.AppointmentReservations.AsNoTracking()
            .Where(reservation => reservation.PatientEmail.ToUpper() == normalizedEmail
                && reservation.AppointmentSlot.StartsAt > now)
            .OrderBy(reservation => reservation.AppointmentSlot.StartsAt)
            .Select(reservation => new PatientAppointmentViewModel(
                reservation.AppointmentSlot.Doctor.Name,
                reservation.AppointmentSlot.Doctor.SpecialtyName,
                reservation.AppointmentSlot.StartsAt))
            .ToListAsync(cancellationToken);
    }
}
