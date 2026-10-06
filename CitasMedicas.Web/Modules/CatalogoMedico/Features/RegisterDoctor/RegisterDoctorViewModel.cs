using System.ComponentModel.DataAnnotations;
using CitasMedicas.Web.Modules.CatalogoMedico.Features.ListSpecialties;

namespace CitasMedicas.Web.Modules.CatalogoMedico.Features.RegisterDoctor;

public sealed class RegisterDoctorViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    [Display(Name = "Nombre del médico")]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una especialidad.")]
    [Display(Name = "Especialidad")]
    public int SpecialtyId { get; set; }

    public IReadOnlyList<SpecialtyViewModel> Specialties { get; set; } = [];
}
