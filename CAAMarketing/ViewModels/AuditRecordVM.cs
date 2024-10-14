using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace CAAMarketing.ViewModels
{
    public class AuditRecordVM
    {
        public string Entity { get; set; }

        public string User { get; set; }

        [Display(Name = "Date Time")]
        public DateTime DateTime { get; set; }

        public string Type { get; set; }

        [Display(Name = "Change History")]
        public ICollection<AuditValueVM> AuditValues { get; set; } = new HashSet<AuditValueVM>();
    }
}
