using Microsoft.EntityFrameworkCore;
using HealthcareSystem.Models;

namespace HealthcareSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Users> Users { get; set; }
        public DbSet<Patients> Patients { get; set; }
        public DbSet<Doctors> Doctors { get; set; }
        public DbSet<Radiologists> Radiologists { get; set; }
        public DbSet<Diagnoses> Diagnoses { get; set; }
        public DbSet<MedicalImages> MedicalImages { get; set; }
        public DbSet<PatientTasks> PatientTasks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Patients>()
                .HasOne(p => p.AssignedDoctor)
                .WithMany(d => d.PatientIds)
                .HasForeignKey(p => p.AssignedDoctorId)
                .OnDelete(DeleteBehavior.SetNull); 

            modelBuilder.Entity<Patients>()
                .HasOne(p => p.AssignedRadiologist)
                .WithMany(r => r.PatientIds)
                .HasForeignKey(p => p.AssignedRadiologistId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Diagnoses>()
                .HasOne(d => d.Patient)
                .WithMany(p => p.Diagnoses) 
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Diagnoses>()
                .HasOne(d => d.DiagnosedByDoctor)
                .WithMany(dr => dr.DiagnosesIds)
                .HasForeignKey(d => d.DiagnosedByDoctorId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<MedicalImages>()
                .HasOne(m => m.Patient)
                .WithMany(p => p.MedicalImages) 
                .HasForeignKey(m => m.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MedicalImages>()
                .HasOne(m => m.UploadedByRadiologist)
                .WithMany(r => r.MedicalImagesIds)
                .HasForeignKey(m => m.UploadedByRadiologistId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PatientTasks>()
                .HasOne(pt => pt.Patient)
                .WithMany()
                .HasForeignKey(pt => pt.PatientId);
        }
    }
}
