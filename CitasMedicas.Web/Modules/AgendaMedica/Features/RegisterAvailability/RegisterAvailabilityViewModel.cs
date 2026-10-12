using System.ComponentModel.DataAnnotations;
using CitasMedicas.Web.Modules.AgendaMedica.Features.GetDoctorAvailability;

namespace CitasMedicas.Web.Modules.AgendaMedica.Features.RegisterAvailability;

public sealed class RegisterAvailabilityViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un médico.")]
    [Display(Name = "Médico")]
    public int DoctorId { get; set; }

    public List<AvailabilitySlotInputViewModel> Slots { get; set; } = [];

    public IReadOnlyList<DoctorViewModel> Doctors { get; set; } = [];

    public IReadOnlyList<string> SavedSlots { get; set; } = [];

    public string? SavedSlotsMessage { get; set; }
}

public sealed class AvailabilitySlotInputViewModel
{
    public string Key { get; set; } = Guid.NewGuid().ToString("N");

    [Required(ErrorMessage = "Indica una fecha y hora.")]
    [Display(Name = "Fecha y hora")]
    public DateTime? StartsAt { get; set; }

    public string? ErrorMessage { get; set; }
}
