using CitasMedicas.Web.Modules.CatalogoMedico.Features.ListSpecialties;

namespace CitasMedicas.Web.Modules.AgendaMedica.Features.GetDoctorAvailability;

public sealed class DoctorListViewModel
{
    public SpecialtyViewModel Specialty { get; init; } = null!;
    public IReadOnlyList<DoctorViewModel> Doctors { get; init; } = [];
}
