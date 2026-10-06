using System.ComponentModel.DataAnnotations;
using CitasMedicas.Web.Modules.AgendaMedica.Features.GetDoctorAvailability;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CitasMedicas.Web.Modules.Reservas.Features.BookAppointment;

public sealed class BookAppointmentViewModel
{
    public int DoctorId { get; init; }
    public int SlotId { get; init; }
    [ValidateNever]
    public DoctorViewModel Doctor { get; init; } = null!;
    public DateTime StartsAt { get; init; }

    [Required(ErrorMessage = "Indica tu nombre.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    [Display(Name = "Nombre completo")]
    public string PatientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indica tu email.")]
    [EmailAddress(ErrorMessage = "Introduce un email válido.")]
    [StringLength(254, ErrorMessage = "El email no puede superar los 254 caracteres.")]
    [Display(Name = "Email")]
    public string PatientEmail { get; set; } = string.Empty;
}

public sealed record AppointmentConfirmationViewModel(
    string PatientName,
    string PatientEmail,
    string DoctorName,
    string SpecialtyName,
    DateTime StartsAt);
