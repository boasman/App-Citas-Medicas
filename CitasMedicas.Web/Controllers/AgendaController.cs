using CitasMedicas.Web.Modules.AgendaMedica.Features.RegisterAvailability;
using Microsoft.AspNetCore.Mvc;

namespace CitasMedicas.Web.Controllers;

public class AgendaController(
    GetAvailabilityRegistrationOptionsQuery options,
    RegisterAvailabilityCommand registerAvailability) : Controller
{
    [HttpGet]
    public async Task<IActionResult> CreateAvailability(CancellationToken cancellationToken)
    {
        return View(new RegisterAvailabilityViewModel
        {
            Doctors = await options.ExecuteAsync(cancellationToken)
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
                TempData["AvailabilityRegistrationMessage"] = "El horario se registró correctamente.";
                return RedirectToAction(nameof(CreateAvailability));
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
