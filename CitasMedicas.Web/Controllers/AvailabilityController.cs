using CitasMedicas.Web.Modules.AgendaMedica.Features.GetDoctorAvailability;
using CitasMedicas.Web.Modules.Reservas.Features.BookAppointment;
using CitasMedicas.Web.Modules.CatalogoMedico.Features.ListSpecialties;
using Microsoft.AspNetCore.Mvc;

namespace CitasMedicas.Web.Controllers;

public class AvailabilityController(
    GetDoctorAvailabilityQuery availability,
    ListSpecialtiesQuery specialties,
    BookAppointmentCommand bookAppointment,
    GetAppointmentConfirmationQuery confirmation) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int doctorId, CancellationToken cancellationToken)
    {
        var model = await availability.GetAsync(doctorId, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        var specialty = (await specialties.ExecuteAsync(cancellationToken))
            .FirstOrDefault(item => item.Name == model.Doctor.SpecialtyName);
        if (specialty is null)
        {
            return NotFound();
        }
        ViewData["SpecialtyId"] = specialty.Id;

        if (HttpContext.Session.GetInt32("ReservationDoctorId") != doctorId)
        {
            HttpContext.Session.SetInt32("ReservationSpecialtyId", specialty.Id);
            HttpContext.Session.SetInt32("ReservationDoctorId", doctorId);
            HttpContext.Session.Remove("ReservationSlotId");
        }
        var selectedSlotId = HttpContext.Session.GetInt32("ReservationSlotId");
        var selectedSlot = model.Dates.SelectMany(date => date.Slots)
            .FirstOrDefault(slot => slot.Id == selectedSlotId);
        if (selectedSlotId.HasValue && selectedSlot is null)
        {
            HttpContext.Session.Remove("ReservationSlotId");
        }
        if (selectedSlot is not null)
        {
            model = new DoctorAvailabilityViewModel
            {
                Doctor = model.Doctor,
                Dates = model.Dates,
                Message = model.Message,
                SelectedAt = selectedSlot.StartsAt
            };
        }
        ViewData["ReservationStep"] = 3;
        return View(model);
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
                Message = "No se reservó la cita. Ese horario ya no está disponible; elige otro horario."
            };
            ViewData["ReservationStep"] = 3;
            ViewData["SpecialtyId"] = HttpContext.Session.GetInt32("ReservationSpecialtyId");
            return View("Index", model);
        }

        HttpContext.Session.SetInt32("ReservationSlotId", slotId);
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
            HttpContext.Session.Remove("ReservationSlotId");
            TempData["AvailabilityMessage"] = "No se reservó la cita. Ese horario ya no está disponible; elige otro horario.";
            return RedirectToAction(nameof(Index), new { doctorId });
        }

        ViewData["ReservationStep"] = 4;
        return View(new BookAppointmentViewModel
        {
            DoctorId = doctorId,
            SlotId = slotId,
            Doctor = model.Doctor,
            StartsAt = slot.StartsAt,
            PatientName = HttpContext.Session.GetString("ReservationPatientName") ?? string.Empty,
            PatientEmail = HttpContext.Session.GetString("ReservationPatientEmail") ?? string.Empty
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
            HttpContext.Session.Remove("ReservationSlotId");
            PreservePatientDetails(model);
            TempData["AvailabilityMessage"] = "Ese horario ya no está disponible y la cita no se reservó. Elige otro horario.";
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
            HttpContext.Session.SetString("ReservationPatientName", model.PatientName ?? string.Empty);
            HttpContext.Session.SetString("ReservationPatientEmail", model.PatientEmail ?? string.Empty);
            ViewData["ReservationStep"] = 4;
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
            HttpContext.Session.Remove("ReservationSlotId");
            PreservePatientDetails(model);
            TempData["AvailabilityMessage"] = "Ese horario acaba de ocuparse y la cita no se reservó. Elige otro horario.";
            return RedirectToAction(nameof(Index), new { doctorId = model.DoctorId });
        }

        HttpContext.Session.Remove("ReservationSpecialtyId");
        HttpContext.Session.Remove("ReservationDoctorId");
        HttpContext.Session.Remove("ReservationSlotId");
        HttpContext.Session.Remove("ReservationPatientName");
        HttpContext.Session.Remove("ReservationPatientEmail");

        return RedirectToAction(nameof(Confirmation), new { id = reservationId.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult BackToAvailability(BookAppointmentViewModel model)
    {
        PreservePatientDetails(model);
        return RedirectToAction(nameof(Index), new { doctorId = model.DoctorId });
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(int id, CancellationToken cancellationToken)
    {
        var model = await confirmation.ExecuteAsync(id, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }
        ViewData["ReservationComplete"] = true;
        return View(model);
    }

    private void PreservePatientDetails(BookAppointmentViewModel model)
    {
        HttpContext.Session.SetString("ReservationPatientName", model.PatientName ?? string.Empty);
        HttpContext.Session.SetString("ReservationPatientEmail", model.PatientEmail ?? string.Empty);
    }
}
