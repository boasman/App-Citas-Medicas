using CitasMedicas.Web.Modules.AgendaMedica.Persistence;
using CitasMedicas.Web.Modules.CatalogoMedico.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CitasMedicas.Web.Modules.CatalogoMedico.Features.RegisterDoctor;

public sealed class RegisterDoctorCommand(CatalogoMedicoDbContext dbContext)
{
    public async Task<bool> ExecuteAsync(string name, int specialtyId, CancellationToken cancellationToken = default)
    {
        var specialty = await dbContext.Specialties.AsNoTracking()
            .Where(item => item.Id == specialtyId)
            .Select(item => item.Name)
            .FirstOrDefaultAsync(cancellationToken);
        if (specialty is null)
        {
            return false;
        }

        dbContext.Doctors.Add(new Doctor
        {
            Name = name.Trim(),
            SpecialtyName = specialty
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
