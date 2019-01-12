using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Support
{
    public class SupportContext : DbContext
    {
        public SupportContext(DbContextOptions<SupportContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>()
                .HasOne(p => p.ManagerObj)
                .WithMany(b => b.Operators)
                .HasForeignKey(p => p.Manager)
                .HasConstraintName("FK_employee_employee_manager");

            modelBuilder.Entity<Employee>()
                .HasOne(p => p.DirectorObj)
                .WithMany(b => b.Managers)
                .HasForeignKey(p => p.Director)
                .HasConstraintName("FK_employee_employee_director");
        }

        public virtual DbSet<Employee> Employees { get; set; }
        public virtual DbSet<Message> Messages { get; set; }
    }

    [Table("employee")]
    public class Employee
    {
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Employee()
        {
            Operators = new HashSet<Employee>();
            Managers = new HashSet<Employee>();
            Messages = new HashSet<Message>();
        }

        [Key]
        [Column("login")]
        public string Login { get; set; }

        [Column("manager")]
        public string Manager { get; set; }

        [Column("director")]
        public string Director { get; set; }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Employee> Operators { get; set; }

        public virtual Employee ManagerObj { get; set; }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Employee> Managers { get; set; }

        public virtual Employee DirectorObj { get; set; }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Message> Messages { get; set; }
    }

    [Table("message")]
    public class Message
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("client")]
        public string Client { get; set; }

        [Column("contents")]
        public string Contents { get; set; }

        [Column("created")]
        public DateTime Created { get; set; }

        [Column("operator")]
        public string Operator { get; set; }

        [Column("finished")]
        public DateTime? Finished { get; set; }

        [Column("cancelled")]
        public bool Cancelled { get; set; }

        public virtual Employee Employee { get; set; }
    }
}
