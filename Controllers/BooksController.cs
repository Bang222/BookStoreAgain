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
using BookStore.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace BookStore.Controllers
{
    public class BooksController : Controller
    {
        private readonly UserContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<HomeController> _logger;
        private readonly IEmailSender _emailSender;
        private int _recordsPerPage = 9;

        public BooksController(UserContext context, UserManager<AppUser> userManager, IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }
        public async Task<IActionResult> list(string searchString, int id = 0)
        {
            var books = from b in _context.Book
                        select b;
            if (searchString == null)
            {
                books = books.Where(b => b.Title.Contains(searchString)
                                       && b.Desc.Contains(searchString));
            }


            int numberOfRecords = await books.CountAsync();     //Count SQL
            int numberOfPages = (int)Math.Ceiling((double)numberOfRecords / _recordsPerPage);
            ViewBag.numberOfPages = numberOfPages;
            ViewBag.currentPage = id;
            ViewData["CurrentFilter"] = searchString;
            List<Book> booksList = await books
                .Skip(id * _recordsPerPage)  //Offset SQL
                .Take(_recordsPerPage)       //Top SQL
                .ToListAsync();
            return View(booksList);


        }
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> AddToCart(string isbn,int quantity = 1)
        {
            string thisUserId = _userManager.GetUserId(HttpContext.User);
            Cart myCart = new Cart()
            {
                UId = thisUserId,
                BookIsbn = isbn,
                Quantity = quantity 
            };

            Cart fromDb = _context.Cart.FirstOrDefault(c => c.UId == thisUserId && c.BookIsbn == isbn);
            //if not existing (or null), add it to cart. If already added to Cart before, ignore it.
            if (fromDb == null )
            {
                _context.Add(myCart);
            }
            else if(fromDb != null)
            {
                fromDb.Quantity++;
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("list");
        }
        public async Task<IActionResult> Checkout()
        {
            string thisUserId = _userManager.GetUserId(HttpContext.User);
            List<Cart> myDetailsInCart = await _context.Cart
                .Where(c => c.UId == thisUserId)
                .Include(c => c.Book)
                .ToListAsync();
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    //Step 1: create an order
                    Order myOrder = new Order();
                    myOrder.UId = thisUserId;
                    myOrder.OrderDate = DateTime.Now;
                    myOrder.Total =  myDetailsInCart.Select(c => c.Book.Price * c.Quantity)
                        .Aggregate((c1, c2) => Math.Round((c1 + c2), 1));
                    _context.Add(myOrder);
                    await _context.SaveChangesAsync();
                    //Step 2: insert all order details by var "myDetailsInCart"
                    foreach (var item in myDetailsInCart)
                    {
                        OrderDetail detail = new OrderDetail()
                        {
                            OrderId = myOrder.Id,
                            BookIsbn = item.BookIsbn,
                            Quantity = item.Quantity,
                        };
                        _context.Add(detail);
                    }
                    await _context.SaveChangesAsync();
                    //Step 3: empty/delete the cart we just done for thisUser
                    _context.Cart.RemoveRange(myDetailsInCart);
                    await _context.SaveChangesAsync();
                    transaction.Commit();
                }
                catch (DbUpdateException ex)
                {
                    transaction.Rollback();
                    Console.WriteLine("Error occurred in Checkout" + ex);
                }
            }
            return RedirectToAction("Index", "Carts");
        }
        // GET: Books
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> Index(string searchString, int id = 0)
        {
            AppUser ThisUser = await _userManager.GetUserAsync(HttpContext.User);
            Store ThisStore = await _context.Store.FirstOrDefaultAsync(s => s.UId == ThisUser.Id);
            var books = _context.Book.Where(b => b.StoreId == ThisStore.Id).Include(b => b.Store);
            var BookContext = from b in books select b;
            if(searchString !=null)
             {
                BookContext = BookContext.Where(b => b.Title.Contains(searchString)
                                       || b.Category.Contains(searchString));
            }
            int numberOfRecords = await books.CountAsync();     //Count SQL
            int numberOfPages = (int)Math.Ceiling((double)numberOfRecords / _recordsPerPage);
            ViewBag.numberOfPages = numberOfPages;
            ViewBag.currentPage = id;
            ViewData["CurrentFilter"] = searchString;
            List<Book> Books = await BookContext
                .Skip(id * _recordsPerPage)  //Offset SQL
                .Take(_recordsPerPage)       //Top SQL
                .ToListAsync();
            return View(Books);

        }

        // GET: Books/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Book
                .Include(b => b.Store)
                .FirstOrDefaultAsync(m => m.Isbn == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }
        public async Task<IActionResult> DetailsCustomer(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Book
                .Include(b => b.Store)
                .FirstOrDefaultAsync(m => m.Isbn == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // GET: Books/Create
        [Authorize(Roles = "Seller")]
        public IActionResult Create()
        {
            ViewData["StoreId"] = new SelectList(_context.Store, "Id", "Id");
            return View();
        }

        // POST: Books/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> Create([Bind("Isbn,Title,Pages,Author,Category,Price,Desc,StoreId")] Book book, IFormFile image)
        {

            if (image != null)
            {
                string imgName = book.Isbn + Path.GetExtension(image.FileName);
                string savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", imgName);
                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    image.CopyTo(stream);
                }
                book.ImgUrl = "img/" + imgName;
            }
            else
            {
                return View(book);
            }

            AppUser ThisUres = await _userManager.GetUserAsync(HttpContext.User);
            Store ThisStore = await _context.Store.FirstOrDefaultAsync(s => s.UId == ThisUres.Id);
            book.StoreId = ThisStore.Id;
            try
            {
                //async bất đồng bộ khi kết nối với data base không thuận lợi async đi chung với Task
                //bỏ qua thời gian chờ đợi thêm lệnh thành công //ModelState kiểm trả dữ liệu khách hàng trả về có thoả mãn k 
                _context.Add(book); // ở trạng thái theo giỏi 
                await _context.SaveChangesAsync(); // lưu sinh viên xuông database
                return RedirectToAction(nameof(Index));//RedirectToAction(nameof(Index)) là lệnh chỉ đến tên hoàn toàn bị thay thế
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Unable to save changes. Error is: " + ex.Message);
            }
            return View();
        }

        // GET: Books/Edit/5
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Book.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }
            ViewData["StoreId"] = new SelectList(_context.Store, "Id", "Id", book.StoreId);
            return View(book);
        }

        // POST: Books/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> Edit(IFormFile image, string id, [Bind("Isbn,Title,Pages,Author,Category,Price,Desc,ImgUrl,StoreId")] Book book)
        {
            if (id != book.Isbn)
            {
                return NotFound();
            }
            var bookUpdate = await _context.Book.FirstOrDefaultAsync(s => s.Isbn == id);
            if (bookUpdate == null)
            {
                return NotFound();
            }
            bookUpdate.Isbn = book.Isbn;
            book.Title = book.Title;
            book.Pages = book.Pages;
            bookUpdate.Author = book.Author;
            bookUpdate.Category = book.Category;
            bookUpdate.Price = book.Price;
            bookUpdate.Desc = book.Desc;
            bookUpdate.ImgUrl = book.ImgUrl;

            if (image != null)
            {
                string imgName = book.Isbn + Path.GetExtension(image.FileName);
                string savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", imgName);
                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }
                book.ImgUrl = "img/" + imgName;
            }
            try
            {
                _context.Update(bookUpdate);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                ModelState.AddModelError("", "Unable to update the change. Error is: " + ex.Message);
            }
            return RedirectToAction(nameof(Index));


            ViewData["StoreId"] = new SelectList(_context.Store, "Id", "Id", book.StoreId);
            return View(book);
        }

        // GET: Books/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Book
                .Include(b => b.Store)
                .FirstOrDefaultAsync(m => m.Isbn == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var book = await _context.Book.FindAsync(id);
            if (book == null)
            {
                return RedirectToAction(nameof(Index));
            }
            try
            {
                _context.Book.Remove(book);
                await _context.SaveChangesAsync();
                return View(book);
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Unable to delete student " + id + ". Error is: " + ex.Message);
                return NotFound();
            }
        }

        private bool BookExists(string id)
        {
            return _context.Book.Any(e => e.Isbn == id);
        }
    }
}
