using CitasMedicas.Web.Modules.AgendaMedica.Features.GetDoctorAvailability;
using CitasMedicas.Web.Modules.CatalogoMedico.Features.ListSpecialties;
using CitasMedicas.Web.Modules.CatalogoMedico.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CitasMedicas.Web.Modules.CatalogoMedico.Features.AssignDoctorSpecialty;

public sealed record DoctorSpecialtyAssignmentOptions(
    IReadOnlyList<DoctorViewModel> Doctors,
    IReadOnlyList<SpecialtyViewModel> Specialties);

public sealed class GetDoctorSpecialtyAssignmentOptionsQuery(CatalogoMedicoDbContext dbContext)
{
    public async Task<DoctorSpecialtyAssignmentOptions> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var doctors = await dbContext.Doctors.AsNoTracking()
            .OrderBy(doctor => doctor.Name)
            .Select(doctor => new DoctorViewModel(doctor.Id, doctor.Name, doctor.SpecialtyName))
            .ToListAsync(cancellationToken);
        var specialties = await dbContext.Specialties.AsNoTracking()
            .OrderBy(specialty => specialty.Name)
            .Select(specialty => new SpecialtyViewModel(specialty.Id, specialty.Name, specialty.Description))
            .ToListAsync(cancellationToken);

        return new DoctorSpecialtyAssignmentOptions(doctors, specialties);
    }
}
