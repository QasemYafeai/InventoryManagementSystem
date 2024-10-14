using System.ComponentModel.DataAnnotations;

namespace CAAMarketing.Models
{
    public class Transfer: IValidatableObject
    {
        public int Id { get; set; }

        [Display(Name = "Transfer Title")]
        [Required(ErrorMessage = "You cannot leave the title blank.")]
        [StringLength(150, ErrorMessage = "name cannot be more than 150 characters long.")]
        public string Title { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? TransferDate { get; set; } = DateTime.Today;

        public Location ToLocation { get; set; }
        public int ToLocationID { get; set; }
        public bool Archived { get; set; } = false;

        public ICollection<InventoryTransfer> InventoryTransfers { get; set; }


        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            //Create a string array containing the one element-the field where our error message should show up.
            //then you pass this to the ValidaitonResult This is only so the mesasge displays beside the field
            //instead of just in the validaiton summary.
            //var field = new[] { "DOB" };

            if (TransferDate.GetValueOrDefault() < DateTime.Today)
            {
                yield return new ValidationResult("Transfer Date cannot be in the Past.", new[] { "TransferDate" });
            }
        }
    }
}
