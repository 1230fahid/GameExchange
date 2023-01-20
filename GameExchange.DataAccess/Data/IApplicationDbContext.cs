using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameExchange.Models;
using Microsoft.EntityFrameworkCore;

namespace GameExchange.DataAccess.Data
{
    public interface IApplicationDbContext
    {
        public DbSet<Category> Categories { get; set; } //this is how you create a table in a database using Entity Framework. We create table 'Categories' that has all the properties of everything from GameExchangeWeb.Models.Category
                                                        //This is code first model because here we are writing the code of our model and based on that model we will be creating the database.

        public DbSet<CoverType> CoverTypes { get; set; }

        public DbSet<Product> Products { get; set; }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }

        public DbSet<Company> Companies { get; set; }

        public DbSet<ShoppingCart> ShoppingCarts { get; set; }

        public DbSet<OrderHeader> OrderHeaders { get; set; }

        public DbSet<OrderDetail> OrderDetails { get; set; }
    }
}
