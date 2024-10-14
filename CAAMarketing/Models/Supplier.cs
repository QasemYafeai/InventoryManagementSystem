using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CAAMarketing.Models
{
    public class Supplier : Auditable
    {
        //PROPERTY FIELDS
        public int ID { get; set; }


        [Display(Name = "Name")]
        [Required(ErrorMessage = "You Need A Supplier Name!")]
        [StringLength(30, ErrorMessage = "First name cannot be more than 30 characters long! Please Try Again...")]
        public string Name { get; set; }

        [Required(ErrorMessage = "You Need A Supplier Email!")]
        [StringLength(70, ErrorMessage = "Email cannot be more than 70 characters long! Please Try Again...")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Display(Name = "Phone")]
        [Required(ErrorMessage = "You Need A Phone Number!")]
        [RegularExpression("^\\d{10}$", ErrorMessage = "Please enter a valid 10-digit phone number! No Spaces As Well. Please Try Again...")]
        [DataType(DataType.PhoneNumber)]
        [StringLength(10)]
        public string Phone { get; set; }

        [Required(ErrorMessage = "You Need A Supplier Address!")]
        public string Address { get; set; }

        public ICollection<Item> Items { get; set; } = new HashSet<Item>();
    }
}