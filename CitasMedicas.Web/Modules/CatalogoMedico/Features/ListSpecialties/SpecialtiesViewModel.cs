namespace CitasMedicas.Web.Modules.CatalogoMedico.Features.ListSpecialties;

public sealed class SpecialtiesViewModel
{
    public IReadOnlyList<SpecialtyViewModel> Specialties { get; init; } = [];
    public SpecialtyViewModel? SelectedSpecialty { get; init; }
}
