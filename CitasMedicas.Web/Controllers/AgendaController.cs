using CitasMedicas.Web.Modules.AgendaMedica.Features.RegisterAvailability;
using Microsoft.AspNetCore.Mvc;

namespace CitasMedicas.Web.Controllers;

public class AgendaController(
    GetAvailabilityRegistrationOptionsQuery options,
    RegisterAvailabilityCommand registerAvailability) : Controller
{
    [HttpGet]
    public async Task<IActionResult> CreateAvailability(int? doctorId, CancellationToken cancellationToken)
    {
        var doctors = await options.ExecuteAsync(cancellationToken);
        return View(new RegisterAvailabilityViewModel
        {
            Doctors = doctors,
            DoctorId = doctorId.HasValue && doctors.Any(doctor => doctor.Id == doctorId.Value)
                ? doctorId.Value
                : 0
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAvailability(
        RegisterAvailabilityViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await ReturnCreateAvailabilityViewAsync(model, cancellationToken);
        }

        var result = await registerAvailability.ExecuteAsync(
            model.DoctorId,
            model.StartsAt!.Value,
            cancellationToken);

        switch (result)
        {
            case RegisterAvailabilityResult.Success:
                var doctor = (await options.ExecuteAsync(cancellationToken))
                    .First(item => item.Id == model.DoctorId);
                TempData["AvailabilityRegistrationMessage"] =
                    $"Se registró un horario para {doctor.Name}: {model.StartsAt.Value:dd/MM/yyyy 'a las' HH:mm}.";
                return RedirectToAction(nameof(CreateAvailability), new { doctorId = model.DoctorId });
            case RegisterAvailabilityResult.DoctorNotFound:
                ModelState.AddModelError(nameof(model.DoctorId), "El médico seleccionado ya no existe.");
                break;
            case RegisterAvailabilityResult.StartsAtMustBeFuture:
                ModelState.AddModelError(nameof(model.StartsAt), "Selecciona una fecha y hora futuras.");
                break;
            case RegisterAvailabilityResult.Duplicate:
                ModelState.AddModelError(nameof(model.StartsAt), "Ese médico ya tiene un horario registrado en esa fecha y hora.");
                break;
        }

        return await ReturnCreateAvailabilityViewAsync(model, cancellationToken);
    }

    private async Task<IActionResult> ReturnCreateAvailabilityViewAsync(
        RegisterAvailabilityViewModel model,
        CancellationToken cancellationToken)
    {
        model.Doctors = await options.ExecuteAsync(cancellationToken);
        return View("CreateAvailability", model);
    }
}
