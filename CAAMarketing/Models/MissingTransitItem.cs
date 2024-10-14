using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace CAAMarketing.Models
{
    public class MissingTransitItem : Auditable
    {
        public int ID { get; set; }


        [Display(Name = "Reason")]
        [DataType(DataType.MultilineText)]
        public string Reason { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Display(Name = "Missing Quantity")]
        public int Quantity { get; set; }


        [Required]
        [Display(Name = "Item")]
        public int ItemId { get; set; }
        public Item Item { get; set; }

        [Required]
        [Display(Name = "Location")]
        public int FromLocationID { get; set; }
        public Location FromLocation { get; set; }

        [Required]
        [Display(Name = "Location")]
        public int ToLocationID { get; set; }
        public Location ToLocation { get; set; }


        [Display(Name = "Employee")]
        public int EmployeeID { get; set; }
        public Employee Employee { get; set; }


    }
}
