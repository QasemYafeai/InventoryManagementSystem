using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace CAAMarketing.Models
{
    public class Location : Auditable
    {
        public int Id { get; set; }

        [Display(Name = "Location Name")]
        [Required(ErrorMessage = "You cannot leave the name blank.")]
        [StringLength(150, ErrorMessage = "name cannot be more than 150 characters long.")]
        public string Name { get; set; }


        [Display(Name = "Phone")]
        [Required(ErrorMessage = "You Need A Phone Number!")]
        [RegularExpression("^\\d{10}$", ErrorMessage = "Please enter a valid 10-digit phone number! No Spaces As Well. Please Try Again...")]
        [DataType(DataType.PhoneNumber)]
        [StringLength(10)]
        public string Phone { get; set; }

        public string PhoneNumber
        {
            get
            {
                if (String.IsNullOrEmpty(Phone))
                {
                    return "";
                }
                else
                {
                    return "(" + Phone.Substring(0, 3) + ") " + Phone.Substring(3, 3) + "-" + Phone.Substring(6, 4);
                }
            }
        }


        [Display(Name = "Address")]
        [Required(ErrorMessage = "You cannot leave the name blank.")]
        public string Address { get; set; }

        public ICollection<InventoryTransfer> InventoryTransfersFrom { get; set; }
        public ICollection<InventoryTransfer> InventoryTransfersTo { get; set; }

        public ICollection<ItemLocation> ItemLocations { get; set; } = new HashSet<ItemLocation>();

        public ICollection<Inventory> Inventories { get; set; }

    }
}
