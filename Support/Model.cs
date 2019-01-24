using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

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
                .HasOne(p => p.Manager)
                .WithMany(b => b.Operators)
                .HasForeignKey(p => p.ManagerId)
                .HasConstraintName("FK_employee_employee_manager");

            modelBuilder.Entity<Employee>()
                .HasOne(p => p.Director)
                .WithMany(b => b.Managers)
                .HasForeignKey(p => p.DirectorId)
                .HasConstraintName("FK_employee_employee_director");

            modelBuilder.Entity<Message>()
                .HasOne(p => p.Operator)
                .WithMany(b => b.Messages)
                .HasForeignKey(p => p.OperatorId)
                .HasConstraintName("FK_message_employee");
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
        public string ManagerId { get; set; }

        [Column("director")]
        public string DirectorId { get; set; }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Employee> Operators { get; set; }

        public virtual Employee Manager { get; set; }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Employee> Managers { get; set; }

        public virtual Employee Director { get; set; }

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
        public string OperatorId { get; set; }

        [Column("finished")]
        public DateTime? Finished { get; set; }

        [Column("cancelled")]
        public bool Cancelled { get; set; }

        [Column("answer")]
        public string Answer { get; set; }

        public virtual Employee Operator { get; set; }

        public Message Copy() => MemberwiseClone() as Message;
    }
}
