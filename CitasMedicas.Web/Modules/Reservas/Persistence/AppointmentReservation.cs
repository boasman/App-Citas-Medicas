using CitasMedicas.Web.Modules.AgendaMedica.Persistence;

namespace CitasMedicas.Web.Modules.Reservas.Persistence;

public sealed class AppointmentReservation
{
    public int Id { get; set; }
    public int AppointmentSlotId { get; set; }
    public AppointmentSlot AppointmentSlot { get; set; } = null!;
    public string PatientName { get; set; } = string.Empty;
    public string PatientEmail { get; set; } = string.Empty;
    public DateTime BookedAt { get; set; }
}
