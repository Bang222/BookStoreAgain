using BookStore.Areas.Identity.Data;

namespace BookStore.Models
{
    public class Cart
    {
        public string UId { get; set; }
        public string BookIsbn { get; set; }
        public int Quantity { get; set; }
        public AppUser? User { get; set; }
        public Book? Book { get; set; }
    }
}
