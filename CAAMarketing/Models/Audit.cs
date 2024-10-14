using CAAMarketing.ViewModels;
using Newtonsoft.Json;

namespace CAAMarketing.Models
{
    public class Audit
    {
        public int Id { get; set; }

        public string User
        {
            get
            {
                if (String.IsNullOrEmpty(UserName))
                {
                    return UserId;
                }
                else
                {

                    return UserName + " (" + UserId + ")";
                }
            }
        }

        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Type { get; set; }
        public string EntityName { get; set; }
        public DateTime DateTime { get; set; }
        public string OldValues { get; set; }
        public string NewValues { get; set; }
        public string PrimaryKey { get; set; }
        public string ForeignKeys { get; set; }

        public AuditRecordVM ToAuditRecord()
        {
            var auditRecord = new AuditRecordVM();
            auditRecord.Entity = EntityName;
            auditRecord.DateTime = DateTime;
            auditRecord.User = User;
            auditRecord.Type = Type;
            var oldValues = String.IsNullOrEmpty(OldValues) ? null : JsonConvert.DeserializeObject<Dictionary<string, string>>(OldValues);
            var newValues = String.IsNullOrEmpty(NewValues) ? null : JsonConvert.DeserializeObject<Dictionary<string, string>>(NewValues);
            if (newValues?.Count > 0)
            {
                for (int i = 0; i < newValues.Count; i++)
                {
                    AuditValueVM auditValue = new AuditValueVM();
                    auditValue.PropertyName = newValues?.ElementAt(i).Key;
                    auditValue.OldValue = oldValues?.ElementAt(i).Value;
                    auditValue.NewValue = newValues?.ElementAt(i).Value;
                    auditRecord.AuditValues.Add(auditValue);
                }
            }
            return auditRecord;
        }
    }
}
