using CitasMedicas.Web.Modules.AgendaMedica.Features.RegisterAvailability;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CitasMedicas.Web.Controllers;

public class AgendaController(
    GetAvailabilityRegistrationOptionsQuery options,
    GetDoctorRegisteredAvailabilityQuery registeredAvailability,
    RegisterAvailabilityCommand registerAvailability) : Controller
{
    [HttpGet]
    public async Task<IActionResult> DoctorAvailability(int doctorId, CancellationToken cancellationToken)
    {
        if (doctorId < 1)
        {
            return BadRequest();
        }

        var slots = await registeredAvailability.ExecuteAsync(doctorId, cancellationToken);
        return slots is null ? NotFound() : Json(slots);
    }

    [HttpGet]
    public async Task<IActionResult> CreateAvailability(int? doctorId, CancellationToken cancellationToken)
    {
        var doctors = await options.ExecuteAsync(cancellationToken);
        var model = new RegisterAvailabilityViewModel
        {
            Doctors = doctors,
            DoctorId = doctorId.HasValue && doctors.Any(doctor => doctor.Id == doctorId.Value)
                ? doctorId.Value
                : 0,
            Slots = [new AvailabilitySlotInputViewModel()]
        };
        if (TempData["AvailabilityRegistrationMessage"] is string message)
        {
            model.SavedSlotsMessage = message;
        }
        if (TempData["AvailabilityRegistrationSlots"] is string savedSlotsJson)
        {
            model.SavedSlots = JsonSerializer.Deserialize<List<string>>(savedSlotsJson) ?? [];
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAvailability(
        RegisterAvailabilityViewModel model,
        CancellationToken cancellationToken)
    {
        var hasDoctorError = ModelState.TryGetValue(nameof(model.DoctorId), out var doctorState)
            && doctorState.Errors.Count > 0;
        if (hasDoctorError || model.DoctorId < 1)
        {
            if (!hasDoctorError)
            {
                ModelState.AddModelError(nameof(model.DoctorId), "Selecciona un médico.");
            }
            return await ReturnCreateAvailabilityViewAsync(model, cancellationToken);
        }

        model.Slots ??= [];
        if (model.Slots.Count == 0)
        {
            model.Slots.Add(new AvailabilitySlotInputViewModel());
            ModelState.AddModelError(nameof(model.Slots), "Añade al menos un horario antes de guardar.");
            return await ReturnCreateAvailabilityViewAsync(model, cancellationToken);
        }

        var validRows = new List<(AvailabilitySlotInputViewModel Slot, DateTime StartsAt)>();
        foreach (var slot in model.Slots)
        {
            slot.ErrorMessage = null;
            var key = $"{nameof(model.Slots)}[{slot.Key}].{nameof(slot.StartsAt)}";
            if (ModelState.TryGetValue(key, out var slotState) && slotState.Errors.Count > 0)
            {
                slot.ErrorMessage = slotState.Errors[0].ErrorMessage;
                continue;
            }

            if (slot.StartsAt is null)
            {
                slot.ErrorMessage = "Indica una fecha y hora.";
                continue;
            }

            validRows.Add((slot, slot.StartsAt.Value));
        }

        if (validRows.Count == 0)
        {
            return await ReturnCreateAvailabilityViewAsync(model, cancellationToken);
        }

        var results = await registerAvailability.ExecuteBatchAsync(
            model.DoctorId,
            validRows.Select(row => row.StartsAt).ToArray(),
            cancellationToken);
        if (results.Any(result => result.Result == RegisterAvailabilityResult.DoctorNotFound))
        {
            ModelState.AddModelError(nameof(model.DoctorId), "El médico seleccionado ya no existe.");
            return await ReturnCreateAvailabilityViewAsync(model, cancellationToken);
        }

        var savedSlots = new List<string>();
        var remainingSlots = model.Slots.Where(slot => slot.ErrorMessage is not null).ToList();
        for (var index = 0; index < validRows.Count; index++)
        {
            var (slot, startsAt) = validRows[index];
            switch (results[index].Result)
            {
                case RegisterAvailabilityResult.Success:
                    savedSlots.Add(startsAt.ToString("dd/MM/yyyy HH:mm"));
                    break;
                case RegisterAvailabilityResult.StartsAtMustBeFuture:
                    slot.ErrorMessage = "Selecciona una fecha y hora futuras.";
                    remainingSlots.Add(slot);
                    break;
                case RegisterAvailabilityResult.Duplicate:
                    slot.ErrorMessage = "Ese médico ya tiene un horario registrado en esa fecha y hora.";
                    remainingSlots.Add(slot);
                    break;
            }
        }

        if (savedSlots.Count > 0)
        {
            var doctor = (await options.ExecuteAsync(cancellationToken))
                .First(item => item.Id == model.DoctorId);
            var noun = savedSlots.Count == 1 ? "horario" : "horarios";
            var verb = savedSlots.Count == 1 ? "registró" : "registraron";
            model.SavedSlotsMessage = $"Se {verb} {savedSlots.Count} {noun} para {doctor.Name}.";
            model.SavedSlots = savedSlots;
        }

        model.Slots = remainingSlots;
        if (remainingSlots.Count == 0)
        {
            TempData["AvailabilityRegistrationMessage"] = model.SavedSlotsMessage;
            TempData["AvailabilityRegistrationSlots"] = JsonSerializer.Serialize(savedSlots);
            return RedirectToAction(nameof(CreateAvailability), new { doctorId = model.DoctorId });
        }

        return await ReturnCreateAvailabilityViewAsync(model, cancellationToken);
    }

    private async Task<IActionResult> ReturnCreateAvailabilityViewAsync(
        RegisterAvailabilityViewModel model,
        CancellationToken cancellationToken)
    {
        model.Doctors = await options.ExecuteAsync(cancellationToken);
        if (model.Slots.Count == 0)
        {
            model.Slots = [new AvailabilitySlotInputViewModel()];
        }
        return View("CreateAvailability", model);
    }
}
