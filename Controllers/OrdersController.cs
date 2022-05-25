#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BookStore.Data;
using BookStore.Models;
using Microsoft.AspNetCore.Identity;
using BookStore.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;

namespace BookStore.Controllers
{
    public class OrdersController : Controller
    {
        private readonly UserContext _context;
        protected readonly UserManager<AppUser> _userManager;

        public OrdersController(UserContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Index()
        {
            string thisUserId = _userManager.GetUserId(HttpContext.User);
            var cart = await _context.Order
            .Include(c => c.User)
            .ToListAsync();
            return View(_context.Order.Where(c => c.UId == thisUserId));
        }
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> Manager()
        {
            AppUser thisUser = await _userManager.GetUserAsync(HttpContext.User);
            Store thisStore = await _context.Store.FirstOrDefaultAsync(s => s.UId == thisUser.Id);
            OrderDetail thisOrderDetail = _context.OrderDetail.FirstOrDefault(o => o.Book.StoreId == thisStore.Id);
            var userContext = _context.Order.Where(o => o.Id == thisOrderDetail.OrderId).Include(o => o.User);
            return View(userContext);
        }
    }
}
