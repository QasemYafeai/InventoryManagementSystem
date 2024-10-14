using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace CAAMarketing.Models
{
    public class Event : Auditable
    {
        public int ID { get; set; }

        [Display(Name = "Event Name")]
        [Required(ErrorMessage = "You cannot leave the Event name blank.")]
        [StringLength(150, ErrorMessage = "name cannot be more than 150 characters long.")]
        public string Name { get; set; }

        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Display(Name = "Location")]
        [Required(ErrorMessage = "You cannot leave it blank.")]
        [StringLength(50, ErrorMessage = "cannot be more than 50 characters long.")]
        public string location { get; set; }


        [Required]
        [Display(Name = "Reserved Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? ReservedEventDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Return Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? ReturnEventDate { get; set; } = DateTime.Today;

        public ICollection<ItemReservation> ItemReservations { get; set; } = new HashSet<ItemReservation>();

        //public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        //{
        //    //Create a string array containing the one element-the field where our error message should show up.
        //    //then you pass this to the ValidaitonResult This is only so the mesasge displays beside the field
        //    //instead of just in the validaiton summary.
        //    //var field = new[] { "DOB" };

        //    //if (ReturnEventDate.GetValueOrDefault() < DateTime.Today)
        //    //{
        //    //    yield return new ValidationResult("Return Date cannot be in the Past.", new[] { "ReturnEventDate" });
        //    //}
        //}

        public static implicit operator Event(ItemReservation v)
        {
            throw new NotImplementedException();
        }
    }
}
