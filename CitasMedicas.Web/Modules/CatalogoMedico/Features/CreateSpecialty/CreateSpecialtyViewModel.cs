using System.ComponentModel.DataAnnotations;

namespace CitasMedicas.Web.Modules.CatalogoMedico.Features.CreateSpecialty;

public sealed class CreateSpecialtyViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    [Display(Name = "Nombre de la especialidad")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(300, ErrorMessage = "La descripción no puede superar los 300 caracteres.")]
    [Display(Name = "Descripción")]
    public string Description { get; set; } = string.Empty;
}
