using CitasMedicas.Web.Modules.AgendaMedica.Persistence;
using CitasMedicas.Web.Modules.CatalogoMedico.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CitasMedicas.Web.Modules.CatalogoMedico.Features.RegisterDoctor;

public sealed class RegisterDoctorCommand(CatalogoMedicoDbContext dbContext)
{
    public async Task<int?> ExecuteAsync(string name, int specialtyId, CancellationToken cancellationToken = default)
    {
        var specialty = await dbContext.Specialties.AsNoTracking()
            .Where(item => item.Id == specialtyId)
            .Select(item => item.Name)
            .FirstOrDefaultAsync(cancellationToken);
        if (specialty is null)
        {
            return null;
        }

        var doctor = new Doctor
        {
            Name = name.Trim(),
            SpecialtyName = specialty
        };
        dbContext.Doctors.Add(doctor);

        await dbContext.SaveChangesAsync(cancellationToken);
        return doctor.Id;
    }
}
