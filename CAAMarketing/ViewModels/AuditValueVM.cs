using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace CAAMarketing.ViewModels
{
    public class AuditValueVM
    {
        [Display(Name = "Property")]
        public string PropertyName { get; set; }

        [Display(Name = "Old Value")]
        public string OldValue { get; set; }

        [Display(Name = "New Value")]
        public string NewValue { get; set; }
    }
}
