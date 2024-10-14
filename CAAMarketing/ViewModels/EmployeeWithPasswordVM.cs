using CAAMarketing.Models;

namespace CAAMarketing.ViewModels
{
    public class EmployeeWithPasswordVM
    {
        public int ID { get; set; }
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public bool Active { get; set; }
        public string Password { get; set; }
        public bool IsFirstLogin { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public string PasswordHash { get; set; }

        public EmployeeWithPasswordVM(Employee employee, InitialPassword password)
        {
            ID = employee.ID;
            UserId = employee.UserId;
            FirstName = employee.FirstName;
            LastName = employee.LastName;
            Phone = employee.Phone;
            Email = employee.Email;
            Active = employee.Active;
            IsFirstLogin = employee.IsFirstLogin;
            TwoFactorEnabled = employee.TwoFactorEnabled;
            PasswordHash = employee.PasswordHash;

            if (password != null)
            {
                Password = password.Password;
            }
        }
    }

}
