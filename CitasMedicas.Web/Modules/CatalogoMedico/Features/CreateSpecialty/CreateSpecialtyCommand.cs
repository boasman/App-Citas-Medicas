using CitasMedicas.Web.Modules.CatalogoMedico.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CitasMedicas.Web.Modules.CatalogoMedico.Features.CreateSpecialty;

public sealed class CreateSpecialtyCommand(CatalogoMedicoDbContext dbContext)
{
    public async Task<bool> ExecuteAsync(string name, string description, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        var exists = await dbContext.Specialties.AnyAsync(
            specialty => specialty.Name.ToUpper() == normalizedName.ToUpper(), cancellationToken);

        if (exists)
        {
            return false;
        }

        dbContext.Specialties.Add(new Specialty
        {
            Name = normalizedName,
            Description = description.Trim()
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
