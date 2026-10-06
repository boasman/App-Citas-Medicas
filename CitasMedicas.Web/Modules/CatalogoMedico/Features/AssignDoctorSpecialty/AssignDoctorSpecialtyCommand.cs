using CitasMedicas.Web.Modules.CatalogoMedico.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CitasMedicas.Web.Modules.CatalogoMedico.Features.AssignDoctorSpecialty;

public sealed class AssignDoctorSpecialtyCommand(CatalogoMedicoDbContext dbContext)
{
    public async Task<bool> ExecuteAsync(int doctorId, int specialtyId, CancellationToken cancellationToken = default)
    {
        var doctor = await dbContext.Doctors.FirstOrDefaultAsync(item => item.Id == doctorId, cancellationToken);
        if (doctor is null)
        {
            return false;
        }

        var specialtyName = await dbContext.Specialties.AsNoTracking()
            .Where(item => item.Id == specialtyId)
            .Select(item => item.Name)
            .FirstOrDefaultAsync(cancellationToken);
        if (specialtyName is null)
        {
            return false;
        }

        doctor.SpecialtyName = specialtyName;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
