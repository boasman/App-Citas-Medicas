using CitasMedicas.Web.Modules.AgendaMedica.Features.GetDoctorAvailability;
using CitasMedicas.Web.Modules.CatalogoMedico.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CitasMedicas.Web.Modules.AgendaMedica.Features.RegisterAvailability;

public sealed class GetAvailabilityRegistrationOptionsQuery(CatalogoMedicoDbContext dbContext)
{
    public async Task<IReadOnlyList<DoctorViewModel>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Doctors.AsNoTracking()
            .OrderBy(doctor => doctor.Name)
            .Select(doctor => new DoctorViewModel(doctor.Id, doctor.Name, doctor.SpecialtyName))
            .ToListAsync(cancellationToken);
    }
}
