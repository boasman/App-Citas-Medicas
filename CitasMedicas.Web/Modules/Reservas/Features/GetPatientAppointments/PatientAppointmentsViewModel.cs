using System.ComponentModel.DataAnnotations;

namespace CitasMedicas.Web.Modules.Reservas.Features.GetPatientAppointments;

public sealed class PatientAppointmentsViewModel
{
    [Required(ErrorMessage = "Indica el email usado al reservar.")]
    [EmailAddress(ErrorMessage = "Introduce un email válido.")]
    [StringLength(254, ErrorMessage = "El email no puede superar los 254 caracteres.")]
    [Display(Name = "Email usado al reservar")]
    public string Email { get; set; } = string.Empty;

    public IReadOnlyList<PatientAppointmentViewModel> Appointments { get; set; } = [];
    public bool HasSearched { get; set; }
}

public sealed record PatientAppointmentViewModel(
    string DoctorName,
    string SpecialtyName,
    DateTime StartsAt);
