using CitasMedicas.Web.Modules.AgendaMedica.Features.GetDoctorAvailability;
using CitasMedicas.Web.Modules.Reservas.Features.BookAppointment;
using Microsoft.AspNetCore.Mvc;

namespace CitasMedicas.Web.Controllers;

public class AvailabilityController(
    GetDoctorAvailabilityQuery availability,
    BookAppointmentCommand bookAppointment,
    GetAppointmentConfirmationQuery confirmation) : Controller
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

        return RedirectToAction(nameof(Book), new { doctorId, slotId });
    }

    [HttpGet]
    public async Task<IActionResult> Book(int doctorId, int slotId, CancellationToken cancellationToken)
    {
        var model = await availability.GetAsync(doctorId, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        var slot = await availability.FindAvailableSlotAsync(doctorId, slotId, cancellationToken);
        if (slot is null)
        {
            TempData["AvailabilityMessage"] = "Ese horario ya no está disponible. Elige otra opción.";
            return RedirectToAction(nameof(Index), new { doctorId });
        }

        return View(new BookAppointmentViewModel
        {
            DoctorId = doctorId,
            SlotId = slotId,
            Doctor = model.Doctor,
            StartsAt = slot.StartsAt
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(BookAppointmentViewModel model, CancellationToken cancellationToken)
    {
        var availabilityModel = await availability.GetAsync(model.DoctorId, cancellationToken);
        if (availabilityModel is null)
        {
            return NotFound();
        }

        var slot = await availability.FindAvailableSlotAsync(model.DoctorId, model.SlotId, cancellationToken);
        if (slot is null)
        {
            TempData["AvailabilityMessage"] = "Ese horario ya no está disponible. Elige otra opción.";
            return RedirectToAction(nameof(Index), new { doctorId = model.DoctorId });
        }

        model = new BookAppointmentViewModel
        {
            DoctorId = model.DoctorId,
            SlotId = model.SlotId,
            Doctor = availabilityModel.Doctor,
            StartsAt = slot.StartsAt,
            PatientName = model.PatientName,
            PatientEmail = model.PatientEmail
        };

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var reservationId = await bookAppointment.ExecuteAsync(
            model.DoctorId,
            model.SlotId,
            model.PatientName,
            model.PatientEmail,
            cancellationToken);

        if (reservationId is null)
        {
            TempData["AvailabilityMessage"] = "Ese horario acaba de ser reservado. Elige otra opción.";
            return RedirectToAction(nameof(Index), new { doctorId = model.DoctorId });
        }

        return RedirectToAction(nameof(Confirmation), new { id = reservationId.Value });
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(int id, CancellationToken cancellationToken)
    {
        var model = await confirmation.ExecuteAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }
}
