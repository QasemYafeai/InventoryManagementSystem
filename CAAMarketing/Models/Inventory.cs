using CAAMarketing.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace CAAMarketing.Models
{
    public class Inventory : Auditable
    {
        public int Id { get; set; }

        //Cost Summary Property
        [Display(Name = "Cost")]
        //[DataType(DataType.Currency)]
        public string CostString
        {
            get
            {
                //convert decimal cost to string for ordering to work
                string newCost = Cost.ToString("C");
                return newCost;
            }
        }

        [Display(Name = "Cost Per Item")]
        [Required(ErrorMessage = "You must enter a cost.")]
        [DataType(DataType.Currency)]
        public decimal Cost { get; set; }

        [Display(Name = "Quantity")]
        [Required(ErrorMessage = "You must enter a quantity.")]
        public int Quantity { get; set; }

        public int ItemID { get; set; }
        //Making a collection of item objects in this class
        [Display(Name = "Items")]
        public Item Item { get; set; }


        [Display(Name = "Location")]
        public int LocationID { get; set; }
        public Location Location { get; set; }

        public int BaseStockLevel { get; set; } = 100;

        public bool IsLowInventory { get; set; }

        public bool SubractedFromEventRecord { get; set; } = false;
        public int LowInventoryThreshold { get; set; } = 10;

        public DateTime? DismissNotification { get; set; } = DateTime.Today;
    }
}
