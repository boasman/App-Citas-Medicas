namespace CitasMedicas.Web.Modules.AgendaMedica.Persistence;

public sealed class AppointmentSlot
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;
    public DateTime StartsAt { get; set; }
    public bool IsOccupied { get; set; }
}
