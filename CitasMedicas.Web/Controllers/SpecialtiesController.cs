using CitasMedicas.Web.Modules.CatalogoMedico.Features.ListSpecialties;
using CitasMedicas.Web.Modules.CatalogoMedico.Features.CreateSpecialty;
using Microsoft.AspNetCore.Mvc;

namespace CitasMedicas.Web.Controllers;

public class SpecialtiesController(
    ListSpecialtiesQuery listSpecialties,
    CreateSpecialtyCommand createSpecialty) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new SpecialtiesViewModel
        {
            Specialties = await listSpecialties.ExecuteAsync(cancellationToken)
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateSpecialtyViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateSpecialtyViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var created = await createSpecialty.ExecuteAsync(model.Name, model.Description, cancellationToken);
        if (!created)
        {
            ModelState.AddModelError(nameof(model.Name), "Ya existe una especialidad con ese nombre.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"La especialidad {model.Name.Trim()} se creó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Select(int id, CancellationToken cancellationToken)
    {
        var specialty = await listSpecialties.FindByIdAsync(id, cancellationToken);
        if (specialty is null)
        {
            return NotFound();
        }

        return View("Index", new SpecialtiesViewModel
        {
            Specialties = await listSpecialties.ExecuteAsync(cancellationToken),
            SelectedSpecialty = specialty
        });
    }
}
