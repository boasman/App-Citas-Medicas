namespace CitasMedicas.Web.Modules.AgendaMedica.Persistence;

public sealed class Doctor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SpecialtyName { get; set; } = string.Empty;
    public List<AppointmentSlot> Slots { get; set; } = [];
}
