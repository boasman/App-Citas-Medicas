using CitasMedicas.Web.Modules.CatalogoMedico.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CitasMedicas.Web.Modules.CatalogoMedico.Features.ListSpecialties;

public sealed class ListSpecialtiesQuery(CatalogoMedicoDbContext dbContext)
{
    public async Task<IReadOnlyList<SpecialtyViewModel>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Specialties
            .AsNoTracking()
            .OrderBy(specialty => specialty.Name)
            .Select(specialty => new SpecialtyViewModel(specialty.Id, specialty.Name, specialty.Description))
            .ToListAsync(cancellationToken);
    }

    public async Task<SpecialtyViewModel?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Specialties
            .AsNoTracking()
            .Where(specialty => specialty.Id == id)
            .Select(specialty => new SpecialtyViewModel(specialty.Id, specialty.Name, specialty.Description))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
