using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CAAMarketing.Data;
using CAAMarketing.Models;
using Microsoft.AspNetCore.Authorization;
using CAAMarketing.Utilities;
using Microsoft.AspNetCore.Identity;
using CAAMarketing.ViewModels;
using System.Text.Encodings.Web;
using NToastNotify;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;
using System.Diagnostics;

namespace CAAMarketing.Controllers
{

    [Authorize(Roles = "Admin")]
    public class EmployeesController : Controller
    {
        private readonly CAAContext _context;
        private readonly ApplicationDbContext _identityContext;
        private readonly IMyEmailSender _emailSender;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IToastNotification _toastNotification;
        private readonly IEmailConfiguration _emailConfig;
        private readonly SignInManager<IdentityUser> _signInManager;



        public EmployeesController(CAAContext context, IToastNotification toastNotification,
            ApplicationDbContext identityContext, IMyEmailSender emailSender,
            UserManager<IdentityUser> userManager, IOptions<EmailConfiguration> emailConfig, SignInManager<IdentityUser> signInManager)
        {
            _context = context;
            _toastNotification = toastNotification;
            _identityContext = identityContext;
            _emailSender = emailSender;
            _userManager = userManager;
            _emailConfig = emailConfig.Value;
            _signInManager = signInManager;
        }

        // GET: Employees
        public async Task<IActionResult> Index()
        {
            // Get the list of employees
            var employees = await _context.Employees.ToListAsync();

            // Get the list of initial passwords
            var initialPasswords = await _identityContext.InitialPasswords.ToListAsync();

            var employeeVMs = employees.Select(e => new EmployeeAdminVM
            {
                ID = e.ID,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Email = e.Email,
                Phone = e.Phone,
                Active = e.Active,
                Password = initialPasswords.Any(p => p.UserId == e.UserId) ? initialPasswords.FirstOrDefault(p => p.UserId == e.UserId).Password : e.Password
            }).ToList();

            foreach (var e in employeeVMs)
            {
                var user = await _userManager.FindByEmailAsync(e.Email);
                if (user != null)
                {
                    e.UserRoles = (List<string>)await _userManager.GetRolesAsync(user);
                }
            }

            return View(employeeVMs);
        }





        // GET: Employee/Create
        public IActionResult Create()
        {
            _toastNotification.AddAlertToastMessage($"Please Start By Entering the Information Of The Employee, You Can Cancel By Clicking The Exit Button.");

            EmployeeAdminVM employee = new EmployeeAdminVM();
            PopulateAssignedRoleData(employee);
            return View(employee);
        }

        // POST: Employee/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,Phone," +
            "Email,Password")] Employee employee, string[] selectedRoles)
        {

            try
            {
                if (ModelState.IsValid)
                {
                    employee.Password = GenerateDefaultPassword(selectedRoles);
                    employee.IsFirstLogin = true;
                    //employee.TwoFactorEnabled = true;
                    _context.Add(employee);
                    await _context.SaveChangesAsync();

                    InsertIdentityUser(employee.Email, selectedRoles);

                    // Send an email to the new employee to reset their password
                    //await SendResetPasswordEmail(employee);
                    await SendResetPasswordEmail(employee);


                    _toastNotification.AddSuccessToastMessage($"Employee Record Created!");


                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateException dex)
            {
                if (dex.GetBaseException().Message.Contains("UNIQUE constraint failed"))
                {
                    ModelState.AddModelError("Email", "Unable to save changes. Remember, you cannot have duplicate Email addresses.");
                }
                else
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
            }
            //We are here because something went wrong and need to redisplay
            EmployeeAdminVM employeeAdminVM = new EmployeeAdminVM
            {
                Email = employee.Email,
                Active = employee.Active,
                ID = employee.ID,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Phone = employee.Phone
            };
            foreach (var role in selectedRoles)
            {
                employeeAdminVM.UserRoles.Add(role);
            }
            PopulateAssignedRoleData(employeeAdminVM);
            return View(employeeAdminVM);
        }

        private string GenerateDefaultPassword(string[] selectedRoles)
        {
            if (selectedRoles.Contains("Admin"))
            {
                return "Admin@123";
            }
            else if (selectedRoles.Contains("Supervisor"))
            {
                return "Super@123";
            }
            else
            {
                return "Emp@123";
            }
        }


        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Where(e => e.ID == id)
                .Select(e => new EmployeeAdminVM
                {
                    Email = e.Email,
                    Active = e.Active,
                    ID = e.ID,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    Phone = e.Phone
                }).FirstOrDefaultAsync();

            if (employee == null)
            {
                return NotFound();
            }

            //Get the user from the Identity system
            var user = await _userManager.FindByEmailAsync(employee.Email);
            if (user != null)
            {
                //Add the current roles
                var r = await _userManager.GetRolesAsync(user);
                employee.UserRoles = (List<string>)r;
            }
            PopulateAssignedRoleData(employee);

            return View(employee);
        }

        // POST: Employees/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, bool Active, string[] selectedRoles, Byte[] RowVersion)
        {
            var employeeToUpdate = await _context.Employees
                .FirstOrDefaultAsync(m => m.ID == id);
            if (employeeToUpdate == null)
            {
                return NotFound();
            }

            //Note the current Email and Active Status
            bool ActiveStatus = employeeToUpdate.Active;
            string databaseEmail = employeeToUpdate.Email;


            if (await TryUpdateModelAsync<Employee>(employeeToUpdate, "",
                e => e.FirstName, e => e.LastName, e => e.Phone, e => e.Email, e => e.Active))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    //Save successful so go on to related changes

                    _toastNotification.AddSuccessToastMessage("Employee Record Updated.");


                    //Check for changes in the Active state
                    if (employeeToUpdate.Active == false && ActiveStatus == true)
                    {
                        //Deactivating them so delete the IdentityUser
                        //This deletes the user's login from the security system
                        await DeleteIdentityUser(employeeToUpdate.Email);

                    }
                    else if (employeeToUpdate.Active == true && ActiveStatus == false)
                    {
                        //You reactivating the user, create them and
                        //give them the selected roles
                        InsertIdentityUser(employeeToUpdate.Email, selectedRoles);
                    }
                    else if (employeeToUpdate.Active == true && ActiveStatus == true)
                    {
                        //No change to Active status so check for a change in Email
                        //If you Changed the email, Delete the old login and create a new one
                        //with the selected roles
                        if (employeeToUpdate.Email != databaseEmail)
                        {
                            //Add the new login with the selected roles
                            InsertIdentityUser(employeeToUpdate.Email, selectedRoles);

                            //This deletes the user's old login from the security system
                            await DeleteIdentityUser(databaseEmail);
                        }
                        else
                        {
                            //Finially, Still Active and no change to Email so just Update
                            await UpdateUserRoles(selectedRoles, employeeToUpdate.Email);
                        }
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employeeToUpdate.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "The record you attempted to edit "
                            + "was modified by another user. Please go back and refresh.");
                    }
                }
                catch (DbUpdateException dex)
                {
                    if (dex.GetBaseException().Message.Contains("UNIQUE constraint failed"))
                    {
                        ModelState.AddModelError("Email", "Unable to save changes. Remember, you cannot have duplicate Email addresses.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                    }
                }
            }
            //We are here because something went wrong and need to redisplay
            EmployeeAdminVM employeeAdminVM = new EmployeeAdminVM
            {
                Email = employeeToUpdate.Email,
                Active = employeeToUpdate.Active,
                ID = employeeToUpdate.ID,
                FirstName = employeeToUpdate.FirstName,
                LastName = employeeToUpdate.LastName,
                Phone = employeeToUpdate.Phone
            };
            foreach (var role in selectedRoles)
            {
                employeeAdminVM.UserRoles.Add(role);
            }
            PopulateAssignedRoleData(employeeAdminVM);
            return View(employeeAdminVM);
        }

        private void PopulateAssignedRoleData(EmployeeAdminVM employee)
        {//Prepare checkboxes for all Roles
            var allRoles = _identityContext.Roles;
            var currentRoles = employee.UserRoles;
            var viewModel = new List<RoleVM>();
            foreach (var r in allRoles)
            {
                viewModel.Add(new RoleVM
                {
                    RoleId = r.Id,
                    RoleName = r.Name,
                    Assigned = currentRoles.Contains(r.Name)
                });
            }
            ViewBag.Roles = viewModel;
        }

        private async Task UpdateUserRoles(string[] selectedRoles, string Email)
        {
            var _user = await _userManager.FindByEmailAsync(Email);//IdentityUser
            if (_user != null)
            {
                var UserRoles = (List<string>)await _userManager.GetRolesAsync(_user);//Current roles user is in

                if (selectedRoles == null)
                {
                    //No roles selected so just remove any currently assigned
                    foreach (var r in UserRoles)
                    {
                        await _userManager.RemoveFromRoleAsync(_user, r);
                    }
                }
                else
                {
                    //At least one role checked so loop through all the roles
                    //and add or remove as required

                    //We need to do this next line because foreach loops don't always work well
                    //for data returned by EF when working async.  Pulling it into an IList<>
                    //first means we can safely loop over the colleciton making async calls and avoid
                    //the error 'New transaction is not allowed because there are other threads running in the session'
                    IList<IdentityRole> allRoles = _identityContext.Roles.ToList<IdentityRole>();

                    foreach (var r in allRoles)
                    {
                        if (selectedRoles.Contains(r.Name))
                        {
                            if (!UserRoles.Contains(r.Name))
                            {
                                await _userManager.AddToRoleAsync(_user, r.Name);
                            }
                        }
                        else
                        {
                            if (UserRoles.Contains(r.Name))
                            {
                                await _userManager.RemoveFromRoleAsync(_user, r.Name);
                            }
                        }
                    }
                }
            }
        }

        private async Task SendResetPasswordEmail(Employee employee)
        {
            var user = await _userManager.FindByEmailAsync(employee.Email);
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                if (string.IsNullOrEmpty(token))
                {
                    TempData["message"] = $"Could not generate password reset token for {employee.FirstName} {employee.LastName} at {employee.Email}.";
                    return;
                }

                var callbackUrl = Url.Page("/Account/ResetPassword", pageHandler: null, values: new { area = "Identity", token = token }, protocol: Request.Scheme);

                var message = $"Hello {employee.FirstName} {employee.LastName}, Welcome to CAA Niagara!<br /><br />" +
                    $"Please reset your password by clicking <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>here</a>.<br /><br />" +
                    $"Thank you.";

                try
                {
                    await _emailSender.SendOneAsync(employee.FirstName, employee.Email, "Reset Password", message);
                    TempData["message"] = $"Reset password email sent to {employee.FirstName} {employee.LastName} at {employee.Email}.";
                    _toastNotification.AddSuccessToastMessage($"Pasword Reset Has Been Sent To {employee.FirstName} {employee.LastName} at {employee.Email}.");
                }
                catch (Exception ex)
                {
                    TempData["message"] = $"Could not send reset password email to {employee.FirstName} {employee.LastName} at {employee.Email}. Error: {ex.Message}";
                    _toastNotification.AddErrorToastMessage($"Could not send reset password email to {employee.FirstName} {employee.LastName} at {employee.Email}.");
                }
            }
        }



        public async Task<IActionResult> ResetPassword(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByEmailAsync(employee.Email);
            if (user == null)
            {
                return NotFound();
            }

            // Generate a password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Create the password reset link
            var callbackUrl = Url.Page("/Account/ResetPassword", null, new { area = "Identity", token }, HttpContext.Request.Scheme);

            var message = $"Hello {employee.FirstName} {employee.LastName},<br /><br />" +
                    $"Please reset your password by clicking <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>here</a>.<br /><br />" +
                    $"Thank you.";

            // Send the password reset email
            await _emailSender.SendOneAsync(employee.FirstName, employee.Email, "Reset Password", message);
            TempData["message"] = $"Reset password email sent to {employee.FirstName} {employee.LastName} at {employee.Email}.";
            _toastNotification.AddSuccessToastMessage($"Reset password email sent to {employee.FirstName} {employee.LastName} at {employee.Email}.");
            // Redirect to the Employee Index page
            return RedirectToAction("Index", "Employees");
        }





        private async Task SendWelcomeEmailWithTempPassword(Employee employee)
        {
            var user = await _userManager.FindByEmailAsync(employee.Email);
            if (user != null)
            {
                var tempPassword = employee.Password;
                if (string.IsNullOrEmpty(tempPassword))
                {
                    TempData["message"] = $"Could not generate temporary password for {employee.FirstName} {employee.LastName} at {employee.Email}.";
                    _toastNotification.AddErrorToastMessage("Oops, could not generate temperary password.");
                    return;
                }

                // Generate password reset token
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetPasswordLink = Url.Page("/Account/ResetPassword", pageHandler: null, values: new { area = "Identity", code = token }, protocol: Request.Scheme);

                var message = $"Hello {employee.FirstName} {employee.LastName}, Welcome to CAA Niagara!<br /><br />" +
                    $"Your temporary password is: {tempPassword}<br />" +
                    $"Please click <a href='{HtmlEncoder.Default.Encode(resetPasswordLink)}'>here</a> to reset your password and login.<br /><br />" +
                    $"Thank you.";

                try
                {
                    await _emailSender.SendOneAsync(employee.FirstName, employee.Email, "Welcome to CAA Niagara", message);
                    TempData["message"] = $"Welcome email with temporary password sent to {employee.FirstName} {employee.LastName} at {employee.Email}.";
                    _toastNotification.AddSuccessToastMessage($"Welcome email with temporary password sent to {employee.FirstName} {employee.LastName} at {employee.Email}.");
                }
                catch (Exception ex)
                {
                    TempData["message"] = $"Could not send welcome email with temporary password to {employee.FirstName} {employee.LastName} at {employee.Email}. Error: {ex.Message}";
                    _toastNotification.AddErrorToastMessage($"Could not send welcome email with temporary password.");
                }
            }
        }






        private async void InsertIdentityUser(string email, string[] selectedRoles)
        {
            // Retrieve the employee from the database using the email address
            var employee = _context.Employees.SingleOrDefault(e => e.Email == email);

            if (employee != null)
            {
                // Create the IdentityUser with the specified email and password
                var user = new IdentityUser { UserName = email, Email = email, TwoFactorEnabled = true };
                var result = await _userManager.CreateAsync(user, employee.Password);

                if (result.Succeeded)
                {
                    // Assign the selected roles to the user
                    await _userManager.AddToRolesAsync(user, selectedRoles);
                }
            }
        }


        private async Task DeleteIdentityUser(string Email)
        {
            var userToDelete = await _identityContext.Users.Where(u => u.Email == Email).FirstOrDefaultAsync();
            if (userToDelete != null)
            {
                _identityContext.Users.Remove(userToDelete);
                await _identityContext.SaveChangesAsync();
            }
        }

        private async Task InviteUserToResetPassword(Employee employee, string message)
        {
            message ??= "Hello " + employee.FirstName + "<br /><p>Please navigate to:<br />" +
                        "<a href='https://theapp.azurewebsites.net/' title='https://theapp.azurewebsites.net/' target='_blank' rel='noopener'>" +
                        "https://theapp.azurewebsites.net</a><br />" +
                        " and create a new password for " + employee.Email + " using Forgot Password.</p>";
            try
            {
                await _emailSender.SendOneAsync(employee.FullName, employee.Email,
                "Account Registration", message);
                TempData["message"] = "Invitation email sent to " + employee.FullName + " at " + employee.Email;
                _toastNotification.AddSuccessToastMessage("Invitation Sent");
            }
            catch (Exception)
            {
                TempData["message"] = "Could not send Invitation email to " + employee.FullName + " at " + employee.Email;
                _toastNotification.AddErrorToastMessage("Could not send invitation");
            }


        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.ID == id);
        }
    }
}
