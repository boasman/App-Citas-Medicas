namespace CitasMedicas.Web.Modules.AgendaMedica.Features.GetDoctorAvailability;

public sealed record DoctorViewModel(int Id, string Name, string SpecialtyName);
public sealed record AvailableSlotViewModel(int Id, DateTime StartsAt);
public sealed record AvailableDateViewModel(DateTime Date, IReadOnlyList<AvailableSlotViewModel> Slots);

public sealed class DoctorAvailabilityViewModel
{
    public DoctorViewModel Doctor { get; init; } = null!;
    public IReadOnlyList<AvailableDateViewModel> Dates { get; init; } = [];
    public DateTime? SelectedAt { get; init; }
    public string? Message { get; init; }
}
