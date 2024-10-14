using CAAMarketing.Models;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace CAAMarketing.ViewModels
{
    public class InventoryReportVM
    {
        public int ID { get; set; }

        [Display(Name = "Category")]
        public string Category { get; set; }
        public int CategoryID { get; set; }

        [Display(Name = "UPC")]
        public long UPC { get; set; }

        [Display(Name = "Item Name")]
        public int ItemID { get; set; }
        public string ItemName { get; set; }

        [Display(Name = "Cost")]
        [DataType(DataType.Currency)]
        public decimal Cost { get; set; }

        [Display(Name = "Quantity")]
        public int Quantity { get; set; }

        [Display(Name = "Location")]
        public string Location { get; set; }
        public int LocationID { get; set; }

        [Display(Name = "Type of Supplier")]
        public string Supplier { get; set; }
        public int SupplierID { get; set; }

        [Display(Name = "Date Received")]
        [DataType(DataType.Date)]
        public DateTime DateReceived { get; set; }

        public string Notes { get; set; }

        public ICollection<Inventory> Inventories { get; set; }

        public ICollection<ItemLocation> ItemLocations { get; set; }

        //for checkbox on location inventory min levels
        public bool FilterByBaseStockLevel { get; set; } = false;
        public int BaseStockLevel { get; set; }
        public int LowInventoryThreshold { get; set; } = 10;
        public List<Location> Locations { get; set; }
    }
}
