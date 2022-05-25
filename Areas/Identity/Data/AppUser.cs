using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookStore.Models;
using Microsoft.AspNetCore.Identity;

namespace BookStore.Areas.Identity.Data;

// Add profile data for application users by adding properties to the AppUser class

    public class AppUser : IdentityUser
    {
        public DateTime? DoB { get; set; }
        public string? Address { get; set; }
        public Store? Store { get; set; }
        public virtual ICollection<Order>? Orders { get; set; }
        public virtual ICollection<Cart>? Carts { get; set; }
    }


