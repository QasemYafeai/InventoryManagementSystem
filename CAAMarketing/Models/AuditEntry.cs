using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;

namespace CAAMarketing.Models
{
    public class AuditEntry
    {
        public AuditEntry(EntityEntry entry)
        {
            Entry = entry;
        }
        public EntityEntry Entry { get; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string EntityName { get; set; }
        public Dictionary<string, object> KeyValues { get; } = new Dictionary<string, object>();
        public Dictionary<string, object> FKeyValues { get; } = new Dictionary<string, object>();
        public Dictionary<string, object> OldValues { get; } = new Dictionary<string, object>();
        public Dictionary<string, object> NewValues { get; } = new Dictionary<string, object>();

        public string AuditType { get; set; }
        public Audit ToAudit(DateTime When)
        {
            var audit = new Audit();
            audit.UserId = UserId;
            audit.Type = AuditType;
            audit.EntityName = EntityName;
            audit.DateTime = When;
            audit.PrimaryKey = JsonConvert.SerializeObject(KeyValues);
            audit.OldValues = OldValues.Count == 0 ? null : JsonConvert.SerializeObject(OldValues);
            audit.NewValues = NewValues.Count == 0 ? null : JsonConvert.SerializeObject(NewValues);
            audit.ForeignKeys = FKeyValues.Count == 0 ? null : JsonConvert.SerializeObject(FKeyValues);
            return audit;
        }
    }
}
