using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CAAMarketing.Data;
using CAAMarketing.Models;
using CAAMarketing.Utilities;
using Microsoft.AspNetCore.Authorization;
using NToastNotify;

namespace CAAMarketing.Controllers
{
    [Authorize]
    public class CategoriesController : Controller
    {

        private readonly IToastNotification _toastNotification;
        private readonly CAAContext _context;

        public CategoriesController(CAAContext context, IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
        }

        // GET: Categories
        public async Task<IActionResult> Index(string SearchString, int? page, int? pageSizeID
            , string actionButton, string sortDirection = "asc", string sortField = "Category")
        {
            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);



            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering
                                        //Then in each "test" for filtering, add ViewData["Filtering"] = " show" if true;

            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "Category", "LowThresholdAmount" };


            var categories = _context.Category
                .Include(i => i.Items)
                .AsNoTracking();


            if (!String.IsNullOrEmpty(SearchString))
            {
                categories = categories.Where(p => p.Name.ToUpper().Contains(SearchString.ToUpper()));

                ViewData["Filtering"] = "btn-danger";
            }

            //Before we sort, see if we have called for a change of filtering or sorting
            if (!String.IsNullOrEmpty(actionButton)) //Form Submitted!
            {
                if (sortOptions.Contains(actionButton))//Change of sort is requested
                {
                    if (actionButton == sortField) //Reverse order on same field
                    {
                        sortDirection = sortDirection == "asc" ? "desc" : "asc";
                    }
                    sortField = actionButton;//Sort by the button clicked
                }
            }

            //Now we know which field and direction to sort by
            if (sortField == "Category")
            {
                if (sortDirection == "asc")
                {
                    categories = categories
                        .OrderBy(p => p.Name);
                }
                else
                {
                    categories = categories
                        .OrderByDescending(p => p.Name);
                }
            }
            //Now we know which field and direction to sort by
            if (sortField == "LowThresholdAmount")
            {
                if (sortDirection == "asc")
                {
                    categories = categories
                        .OrderBy(p => p.LowCategoryThreshold);
                }
                else
                {
                    categories = categories
                        .OrderByDescending(p => p.LowCategoryThreshold);
                }
            }

            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            //Handle Paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "Categories");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);

            var pagedData = await PaginatedList<Category>.CreateAsync(categories.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
        }

        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Category == null)
            {
                return NotFound();
            }

            var category = await _context.Category
                .FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // GET: Categories/Create
        public IActionResult Create()
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            return View();
        }

        // POST: Categories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,LowCategoryThreshold")] Category category)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            try
            {
                if (ModelState.IsValid)
                {
                    _context.Add(category);
                    await _context.SaveChangesAsync();
                    //return RedirectToAction(nameof(Index));
                    //return RedirectToAction("Details", new { category.Name });
                    return Json(new { categoryID = category.Id, categoryName = category.Name });

                    //return Redirect(ViewData["returnURL"].ToString());

                }
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
            }
            return View(category);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Category == null)
            {
                return NotFound();
            }

            var category = await _context.Category.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: Categories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Byte[] RowVersion)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            //Go get the category to update
            var categToUpdate = await _context.Category.FirstOrDefaultAsync(p => p.Id == id);

            //Check that you got it or exit with a not found error
            if (categToUpdate == null)
            {
                return NotFound();

            }

            //Put the original RowVersion value in the OriginalValues collection for the entity
            _context.Entry(categToUpdate).Property("RowVersion").OriginalValue = RowVersion;

            //Try updating it with the values posted
            if (await TryUpdateModelAsync<Category>(categToUpdate, "",
                p => p.Name, p => p.LowCategoryThreshold))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    //return RedirectToAction(nameof(Index));
                    _toastNotification.AddSuccessToastMessage($"Category Record Updated. Low Item Threshold has been set to {categToUpdate.LowCategoryThreshold}");
                    return RedirectToAction("Index", "Items");
                    //return RedirectToAction("Index", "OrderItems", new { order.ItemID });
                    //return Redirect(ViewData["returnURL"].ToString());

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(categToUpdate.Id))
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
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");

                }

            }
            return View(categToUpdate);
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Category == null)
            {
                return NotFound();
            }

            var category = await _context.Category
                .FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (_context.Category == null)
            {
                return Problem("Entity set 'CAAContext.Category'  is null.");
            }
            var category = await _context.Category.FindAsync(id);
            if (category != null)
            {
                _context.Category.Remove(category);
            }

            await _context.SaveChangesAsync();
            // return RedirectToAction(nameof(Index));
            return Redirect(ViewData["returnURL"].ToString());

        }


        //CREATING CATEGORIES FROM ITEMS PAGE //
        // GET: Categories/Create
        public IActionResult CreateCategoriesFromItems()
        {

            return View();
        }

        // POST: Categories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategoriesFromItems([Bind("Id,Name,LowCategoryThreshold")] Category category)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            try
            {
                if (ModelState.IsValid)
                {
                    _context.Add(category);
                    await _context.SaveChangesAsync();
                    //return RedirectToAction(nameof(Index));
                    //return RedirectToAction("Details", new { category.Name });
                    //return Redirect(ViewData["returnURL"].ToString());

                    ViewData["CategoryID"] = new SelectList(_context.Category, "Id", "Name");

                }
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
            }
            return View(category);
        }
        private string ControllerName()
        {
            return this.ControllerContext.RouteData.Values["controller"].ToString();
        }
        private void ViewDataReturnURL()
        {
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, ControllerName());
        }
        private bool CategoryExists(int id)
        {
            return _context.Category.Any(e => e.Id == id);
        }



    }
}
