using Microsoft.EntityFrameworkCore;
using BenhvienSmart.Models;
using System.Collections.Generic;
using System.Numerics;

namespace BenhvienSmart.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
        {
        }

        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<News> News { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<AISmartRule> AISmartRules { get; set; }
        public DbSet<DoctorSchedule> DoctorSchedules { get; set; }
    }
}