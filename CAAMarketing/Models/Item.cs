using System.ComponentModel.DataAnnotations;

namespace CAAMarketing.Models
{
    public class Item : Auditable, IValidatableObject
    {
        public int ID { get; set; }

        [Display(Name = "Item Name")]
        [Required(ErrorMessage = "You cannot leave the item name blank.")]
        [StringLength(150, ErrorMessage = "name cannot be more than 150 characters long.")]
        public string Name { get; set; }

        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }


        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        [Display(Name = "UPC")]
        [Required(ErrorMessage = "You cannot leave blank.")]
        [RegularExpression("^[0-9]+$", ErrorMessage = "UPC must be numeric.")]
        public long UPC { get; set; }

        [Display(Name = "Date Received")]
        [Required(ErrorMessage = "You cannot leave blank.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? DateReceived { get; set; }

        public ItemImages ItemImages { get; set; }

        public ItemThumbNail ItemThumbNail { get; set; }


        //Calling the Supplier to connect its table into this class
        [Display(Name = "Type of Supplier")]
        [Required(ErrorMessage = "You Must Select A Supplier Name")]
        public int SupplierID { get; set; }

        [Display(Name = "Supplier Name")]
        public Supplier Supplier { get; set; }

        [Display(Name = "Category")]
        [Required(ErrorMessage = "You cannot leave it blank.")]

        public int CategoryID { get; set; }
        public Category Category { get; set; }

        public ICollection<InventoryTransfer> InventoryTransfers { get; set; }

        public ICollection<Inventory> Inventories { get; set; }

        public int EmployeeID { get; set; }
        public Employee Employee { get; set; }

        public ICollection<ItemReservation> ItemReservations { get; set; }

        public ICollection<MissingItemLog> MissingItemLogs { get; set; }

        public ICollection<MissingTransitItem> MissingTransitItems { get; set; }

        public bool Archived { get; set; } = false;

        public ICollection<Receiving> Orders { get; set; }

        [Display(Name = "Cost Per Item")]
        [Required(ErrorMessage = "You must enter a cost.")]
        [DataType(DataType.Currency)]
        public decimal Cost { get; set; }

        [Display(Name = "Quantity")]
        [Required(ErrorMessage = "You must enter a quantity.")]
        public int Quantity { get; set; }

        public bool ItemInvCreated { get; set; } = false;

        public bool isSlectedForEvent { get; set; } = false;
        public ICollection<ItemLocation> ItemLocations { get; set; } = new HashSet<ItemLocation>();

        public string BarcodeSvg { get; set; }




        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            //Create a string array containing the one element-the field where our error message should show up.
            //then you pass this to the ValidaitonResult This is only so the mesasge displays beside the field
            //instead of just in the validaiton summary.
            //var field = new[] { "DOB" };

            if (DateReceived.GetValueOrDefault() > DateTime.Today)
            {
                yield return new ValidationResult("Date Received cannot be in the future.", new[] { "DateReceived" });
            }
        }
    }
}
