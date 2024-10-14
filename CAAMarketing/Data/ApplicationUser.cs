using Microsoft.AspNetCore.Identity;

namespace CAAMarketing.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string TwoFactorAuthKey { get; set; }
    }
}
