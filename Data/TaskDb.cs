using Microsoft.EntityFrameworkCore;
using TaskManger.Models;



namespace TaskManger.Data
{
    public class TaskDb : DbContext
    {

        public TaskDb(DbContextOptions<TaskDb> options) : base(options)
        {
        }

        public DbSet<Users> Users { get; set; }
        public DbSet<TaskName> TaskNames { get; set; }


    }
}
