using System.ComponentModel.DataAnnotations;
using CitasMedicas.Web.Modules.AgendaMedica.Features.GetDoctorAvailability;

namespace CitasMedicas.Web.Modules.AgendaMedica.Features.RegisterAvailability;

public sealed class RegisterAvailabilityViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un médico.")]
    [Display(Name = "Médico")]
    public int DoctorId { get; set; }

    [Required(ErrorMessage = "La fecha y la hora son obligatorias.")]
    [Display(Name = "Fecha y hora")]
    public DateTime? StartsAt { get; set; }

    public IReadOnlyList<DoctorViewModel> Doctors { get; set; } = [];
}
