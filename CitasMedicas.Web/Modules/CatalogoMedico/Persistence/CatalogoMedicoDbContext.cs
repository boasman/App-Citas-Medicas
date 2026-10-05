using Microsoft.EntityFrameworkCore;

namespace CitasMedicas.Web.Modules.CatalogoMedico.Persistence;

public sealed class CatalogoMedicoDbContext(DbContextOptions<CatalogoMedicoDbContext> options) : DbContext(options)
{
    public DbSet<Specialty> Specialties => Set<Specialty>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Specialty>(entity =>
        {
            entity.ToTable("Specialties", "CatalogoMedico");
            entity.HasKey(specialty => specialty.Id);
            entity.Property(specialty => specialty.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(specialty => specialty.Name).IsUnique();
            entity.Property(specialty => specialty.Description).HasMaxLength(300).IsRequired();
            entity.HasData(
                new Specialty { Id = 1, Name = "Cardiología", Description = "Prevención, diagnóstico y tratamiento de enfermedades del corazón." },
                new Specialty { Id = 2, Name = "Dermatología", Description = "Atención de la piel, el cabello y las uñas." },
                new Specialty { Id = 3, Name = "Medicina general", Description = "Atención integral y orientación para el cuidado de tu salud." },
                new Specialty { Id = 4, Name = "Pediatría", Description = "Atención médica para bebés, niños y adolescentes." },
                new Specialty { Id = 5, Name = "Traumatología", Description = "Diagnóstico y tratamiento de lesiones y afecciones del aparato locomotor." });
        });
    }
}
