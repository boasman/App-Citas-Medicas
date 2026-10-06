using System.ComponentModel.DataAnnotations;
using CitasMedicas.Web.Modules.AgendaMedica.Features.GetDoctorAvailability;
using CitasMedicas.Web.Modules.CatalogoMedico.Features.ListSpecialties;

namespace CitasMedicas.Web.Modules.CatalogoMedico.Features.AssignDoctorSpecialty;

public sealed class AssignDoctorSpecialtyViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un médico.")]
    [Display(Name = "Médico")]
    public int DoctorId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una especialidad.")]
    [Display(Name = "Especialidad")]
    public int SpecialtyId { get; set; }

    public IReadOnlyList<DoctorViewModel> Doctors { get; set; } = [];
    public IReadOnlyList<SpecialtyViewModel> Specialties { get; set; } = [];
}
