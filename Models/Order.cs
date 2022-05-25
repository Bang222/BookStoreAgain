using BookStore.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookStore.Models
{
    public class Order
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string UId { get; set; }
        public DateTime OrderDate { get; set; }
        public double Total { get; set; }
        public AppUser? User { get; set; }
        public virtual ICollection<OrderDetail>? OrderDetails { get; set; }

    }
}
