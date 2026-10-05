using CitasMedicas.Web.Modules.AgendaMedica.Persistence;
using CitasMedicas.Web.Modules.CatalogoMedico.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CitasMedicas.Web.Modules.AgendaMedica.Features.GetDoctorAvailability;

public sealed class GetDoctorAvailabilityQuery(CatalogoMedicoDbContext dbContext)
{
    public async Task<IReadOnlyList<DoctorViewModel>> ListDoctorsAsync(string specialtyName, CancellationToken cancellationToken = default)
    {
        return await dbContext.Doctors.AsNoTracking()
            .Where(doctor => doctor.SpecialtyName == specialtyName)
            .OrderBy(doctor => doctor.Name)
            .Select(doctor => new DoctorViewModel(doctor.Id, doctor.Name, doctor.SpecialtyName))
            .ToListAsync(cancellationToken);
    }

    public async Task<DoctorAvailabilityViewModel?> GetAsync(int doctorId, CancellationToken cancellationToken = default)
    {
        var doctor = await dbContext.Doctors.AsNoTracking()
            .Where(item => item.Id == doctorId)
            .Select(item => new DoctorViewModel(item.Id, item.Name, item.SpecialtyName))
            .FirstOrDefaultAsync(cancellationToken);
        if (doctor is null)
        {
            return null;
        }

        var slots = await dbContext.AppointmentSlots.AsNoTracking()
            .Where(slot => slot.DoctorId == doctorId && !slot.IsOccupied && slot.StartsAt >= DateTime.Now)
            .OrderBy(slot => slot.StartsAt)
            .Select(slot => new AvailableSlotViewModel(slot.Id, slot.StartsAt))
            .ToListAsync(cancellationToken);

        return new DoctorAvailabilityViewModel
        {
            Doctor = doctor,
            Dates = slots.GroupBy(slot => slot.StartsAt.Date)
                .Select(group => new AvailableDateViewModel(group.Key, group.ToList()))
                .ToList()
        };
    }

    public Task<AvailableSlotViewModel?> FindAvailableSlotAsync(int doctorId, int slotId, CancellationToken cancellationToken = default)
    {
        return dbContext.AppointmentSlots.AsNoTracking()
            .Where(slot => slot.Id == slotId && slot.DoctorId == doctorId && !slot.IsOccupied && slot.StartsAt >= DateTime.Now)
            .Select(slot => new AvailableSlotViewModel(slot.Id, slot.StartsAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
