using System.ComponentModel.DataAnnotations;

namespace CAAMarketing.Models
{
    public class InventoryTransfer : Auditable, IValidatableObject
    {
        public int Id { get; set; }

        [Required]
        public int ItemId { get; set; }
        public Item Item { get; set; }

        [Required]
        public int FromLocationId { get; set; }
        public Location FromLocation { get; set; }

        [Required]
        public int ToLocationId { get; set; }
        public Location ToLocation { get; set; }


        [Display(Name = "Transfer")]
        public int TransferId { get; set; }
        public Transfer Transfer { get; set; }


        [Required]
        public int Quantity { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? TransferDate { get; set; } = DateTime.Today;


        public bool IsComfirmed { get; set; } = false;

        public bool IsArchived { get; set; }


        public int ComfirmedQuantity { get; set; }

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
