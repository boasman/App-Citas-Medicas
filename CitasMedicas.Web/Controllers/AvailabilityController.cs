using CitasMedicas.Web.Modules.AgendaMedica.Features.GetDoctorAvailability;
using Microsoft.AspNetCore.Mvc;

namespace CitasMedicas.Web.Controllers;

public class AvailabilityController(GetDoctorAvailabilityQuery availability) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int doctorId, CancellationToken cancellationToken)
    {
        var model = await availability.GetAsync(doctorId, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Select(int doctorId, int slotId, CancellationToken cancellationToken)
    {
        var model = await availability.GetAsync(doctorId, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        var slot = await availability.FindAvailableSlotAsync(doctorId, slotId, cancellationToken);
        if (slot is null)
        {
            model = new DoctorAvailabilityViewModel
            {
                Doctor = model.Doctor,
                Dates = model.Dates,
                Message = "Ese horario ya no está disponible. Elige otra opción."
            };
            return View("Index", model);
        }

        return View("Index", new DoctorAvailabilityViewModel
        {
            Doctor = model.Doctor,
            Dates = model.Dates,
            SelectedAt = slot.StartsAt,
            Message = "Has seleccionado este horario. Aún no se ha reservado la cita."
        });
    }
}
