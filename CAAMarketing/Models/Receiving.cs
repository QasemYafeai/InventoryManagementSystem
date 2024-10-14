using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CAAMarketing.Models
{
    public class Receiving : Auditable, IValidatableObject
    {
        //PROPERTY FIELDS
        public int ID { get; set; }

        [Required(ErrorMessage = "You Need An Order Quantity!")]
        public int Quantity { get; set; }

        [Display(Name = "Date Made")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? DateMade { get; set; } = DateTime.Today;

        [Display(Name = "Delivery Date")]
        [Required(ErrorMessage = "You Need A Order Delivery Date!")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? DeliveryDate { get; set; }

        [Display(Name = "Cost Per Item")]
        [Required(ErrorMessage = "You must enter a cost.")]
        [DataType(DataType.Currency)]
        public decimal Cost { get; set; }


        //Calling the Item to connect its table into this class
        [Display(Name = "Type of Item")]
        [Required(ErrorMessage = "You Must Select A Item")]
        public int ItemID { get; set; }

        [Display(Name = "Item Name")]
        public Item Item { get; set; }


        [Display(Name = "Location")]
        public int LocationID { get; set; }
        public Location Location { get; set; }

        public int Progress
        {
            get
            {
                if (DeliveryDate == null)
                    return 0;

                var totalDays = (DeliveryDate.Value - DateMade.Value).TotalDays;
                var elapsedDays = (DateTime.Now - DateMade.Value).TotalDays;
                var progress = (int)Math.Round(elapsedDays / totalDays * 100);
                return progress;
            }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            //Create a string array containing the one element-the field where our error message should show up.
            //then you pass this to the ValidaitonResult This is only so the mesasge displays beside the field
            //instead of just in the validaiton summary.
            //var field = new[] { "DOB" };

            if ((DateMade.GetValueOrDefault() > DateTime.Today) || (DateMade.GetValueOrDefault() < DateTime.Today.AddYears(-15)))
            {
                yield return new ValidationResult("Date Made cannot be in the Future or 15 years in the past.", new[] { "DateMade" });
            }

            if (DeliveryDate.GetValueOrDefault() < DateTime.Today)
            {
                yield return new ValidationResult("Delivery Date cannot be in the past.", new[] { "DeliveryDate" });
            }
        }
    }
}