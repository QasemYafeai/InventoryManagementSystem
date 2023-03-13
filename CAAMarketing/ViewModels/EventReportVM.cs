﻿using CAAMarketing.Models;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace CAAMarketing.ViewModels
{
    public class EventReportVM
    {
        public int Id { get; set; }

        [Display(Name = "Event Name")]
        public string EventName { get; set; }

        [Display(Name = "Item Name")]
        public string ItemName { get; set; }

        public int Quantity { get; set; }

        [Display(Name = "Log Date")]
        [DataType(DataType.Date)]
        public DateTime LogDate { get; set; }

        [Display(Name = "Location")]
        public string Location { get; set; }
        public int LocationID { get; set; }

        public int? ItemReservationId { get; set; } // Nullable foreign key

        public ItemReservation ItemReservation { get; set; }


        public ICollection<ItemLocation> ItemLocations { get; set; } = new HashSet<ItemLocation>();
    }
}
