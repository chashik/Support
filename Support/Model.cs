using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

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

    // DBContext extensions as business logic
    public static class SupportExtentions
    {
        /// <summary>
        /// Complex async method for both emloyees and clients. Contains another check wether login belongs to
        /// employee.
        /// </summary>
        /// <param name="login">login</param>
        /// <param name="num">time offset for employee, id for client</param>
        /// <returns></returns>
        public static async Task<Message> MessageAsync(this SupportContext context, string login, int num)
        {
            Message message = null; // using null assignment while object? still unavailable

            if (await context.Employees.AnyAsync(p => p.Login == login)) // num as time offset if employee
            {
                var messages = context.Messages // unfinished messages for current employee first
                    .Where(p => p.OperatorId == login && p.Finished == null)
                    .ToArray();

                if (messages.Length > 0)
                    message = messages.OrderBy(p => p.Id).First();
                else
                {
                    var created = DateTime.Now.AddSeconds(-num);
                    messages = context.Messages // messages suitable with time offset
                        .Where(p => p.OperatorId == null && p.Finished == null && p.Created < created)
                        .ToArray();

                    if (messages.Length > 0)
                        message = messages.OrderBy(p => p.Id).First();
                }
            }
            else // num as id if client
            {
                var m = await context.Messages.FindAsync(num);
                if (m.Client == login) // additional ownership check
                    message = m;
            }

            return message;
        }

        public static bool DeleteMessages(this SupportContext context, out int rowscount)
        {
            try
            {
                rowscount = context.Database
                    .ExecuteSqlCommand("DELETE FROM [support].[dbo].[message]");
                var r = context.Database // using T-SQL instead of recreating the table as there is no alternative in EF for reseed
                    .ExecuteSqlCommand("DBCC CHECKIDENT ('[support].[dbo].[message]', RESEED, 0)");
                return true;
            }
            catch (Exception ex)
            {
                rowscount = 0;
                return false;
            }
        }
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

        public Employee ShallowCopy() => (Employee)MemberwiseClone();
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

        public Message ShallowCopy() => (Message)MemberwiseClone();
    }
}
