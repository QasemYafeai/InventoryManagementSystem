using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CAAMarketing.Data;
using CAAMarketing.Models;
using Microsoft.Extensions.Logging;
using CAAMarketing.Utilities;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using NToastNotify;
using Org.BouncyCastle.Utilities;
using CAAMarketing.ViewModels;
using AspNetCoreHero.ToastNotification.Abstractions;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using Syncfusion.Blazor.Buttons;
using System.ComponentModel;
using AspNetCore;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using Microsoft.AspNetCore.Authorization;
using System.Web.Helpers;
using Newtonsoft.Json;

namespace CAAMarketing.Controllers
{
    [Authorize]
    public class InventoriesController : Controller
    {
        private readonly CAAContext _context;
        private readonly IToastNotification _toastNotification;
        private readonly INotyfService _itoastNotify;
        private IQueryable<InventoryReportVM> _filteredList;

        public InventoriesController(CAAContext context, IToastNotification toastNotification, INotyfService itoastNotify)
        {
            _context = context;
            _toastNotification = toastNotification;
            _itoastNotify = itoastNotify;
        }

        // GET: Inventories
        public async Task<IActionResult> Index(string SearchString, int?[] LocationID, bool? LowQty,
           int? page, int? pageSizeID, string actionButton, string sortDirection = "asc", string sortField = "Item")
        {
            ViewDataReturnURL();

            //FOR THE SILENTMESSAGE BUTTON SHOWING HOW MANY NOTIF ARE INSIDE
            var invForSilent = _context.Inventories.Where(i => i.DismissNotification > DateTime.Now && i.Item.Archived != true).Count();
            var invnullsForSilent = _context.Inventories.Where(i => i.DismissNotification == null && i.Item.Archived != true).Count();
            ViewData["SilencedMessageCount"] = (invForSilent + invnullsForSilent).ToString();
            //--------------------------------------------------------------------

            // FOR THE ACTIVEMESSAGE BUTTON SHOWING HOW MANY NOTIF ARE INSIDE
            var invForActive = _context.Inventories.Include(i => i.Location).Include(i => i.Item).ThenInclude(i => i.Category)
                .Where(i => i.DismissNotification <= DateTime.Now && i.Quantity < i.Item.Category.LowCategoryThreshold && i.Item.Archived != true && i.DismissNotification != null).Count();

            ViewData["ActiveMessageCount"] = (invForActive).ToString();
            //--------------------------------------------------------------------

            // FOR THE RECOVERALLMESSAGE BUTTON SHOWING HOW MANY NOTIF ARE INSIDE
            var invForRecover = _context.Inventories.Where(i => i.DismissNotification > DateTime.Now).Count();
            var invnullsForRecover = _context.Inventories.Where(i => i.DismissNotification == null && i.Item.Archived != true).Count();
            ViewData["RecoverMessageCount"] = (invForRecover + invnullsForRecover).ToString();
            //--------------------------------------------------------------------

            if (TempData["RecoverNotifMessageBool"] != null)
            {
                _toastNotification.AddSuccessToastMessage(@$"Message Recovered!");
            }
            if (TempData["SilenceNotifMessageBool"] != null)
            {
                _toastNotification.AddSuccessToastMessage(@$"Message Silenced!");
            }
            if (TempData.ContainsKey("NotifFromPopupSuccess") && TempData["NotifFromPopupSuccess"] != null)
            {
                if (TempData["NotifFromPopupSuccess"].ToString() == "Silent")
                {
                    _toastNotification.AddSuccessToastMessage(@$"Message Silenced!");
                }
                if (TempData["NotifFromPopupSuccess"].ToString() == "Activate")
                {
                    _toastNotification.AddSuccessToastMessage(@$"Message Activated!");
                }
            }

            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering
            //Then in each "test" for filtering, add ViewData["Filtering"] = " show" if true;
            var inventories = _context.Inventories
                .Include(i => i.Item.ItemThumbNail)
                .Include(i => i.Item.Employee)
                .Include(i => i.Location)
                .Include(i => i.Item).ThenInclude(i => i.Category)
                .Include(i => i.Item.ItemLocations).ThenInclude(i => i.Location)
            .AsNoTracking();


            inventories = inventories.Where(p => p.Item.Archived == false);
            //CheckInventoryLevel(inventories.ToList());

            //Populating the DropDownLists for the Search/Filtering criteria, which are the Location
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name");

            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "Item", "Location", "UPC", "Quantity", "Cost" };

            //Add as many filters as needed
            if (LocationID.Length > 0)
            {
                inventories = inventories.Where(p => LocationID.Contains(p.LocationID));
                ViewData["Filtering"] = "btn-danger";
            }
            //if (LowQty.HasValue)
            //{
            //    inventories = inventories.Where(p => p.Quantity <= 10);
            //    ViewData["Filtering"] = " show";
            //}
            //if (!LowQty.HasValue)
            //{
            //    inventories = inventories.Where(p => p.Quantity >= 0);
            //    ViewData["Filtering"] = " show";
            //}
            if (!String.IsNullOrEmpty(SearchString))
            {
                long searchUPC;
                bool isNumeric = long.TryParse(SearchString, out searchUPC);

                inventories = inventories.Where(p => p.Item.Name.ToUpper().Contains(SearchString.ToUpper())
                                       || (isNumeric && p.Item.UPC == searchUPC));
                ViewData["Filtering"] = " show";
            }


            if (TempData["InventoryLow"] != null)
            {
                ViewBag.InventoryLow = TempData["InventoryLow"].ToString();
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
            //if (sortField == "Costs")
            //{
            //    if (sortDirection == "asc")
            //    {
            //        inventories = inventories
            //            .OrderBy(p => p.Cost.ToString());
            //    }
            //    else
            //    {
            //        inventories = inventories
            //            .OrderByDescending(p => p.Cost.ToString());
            //    }
            //}
            if (sortField == "Quantity")
            {
                if (sortDirection == "asc")
                {
                    inventories = inventories
                        .OrderByDescending(i => i.Quantity);
                }
                else
                {
                    inventories = inventories
                        .OrderBy(i => i.Quantity);
                }
            }
            else if (sortField == "UPC")
            {
                if (sortDirection == "asc")
                {
                    inventories = inventories
                        .OrderBy(p => p.Item.UPC);
                }
                else
                {
                    inventories = inventories
                        .OrderByDescending(p => p.Item.UPC);
                }
            }
            else if (sortField == "Location")
            {
                if (sortDirection == "asc")
                {
                    inventories = inventories
                        .OrderBy(p => p.Location.Name);
                }
                else
                {
                    inventories = inventories
                        .OrderByDescending(p => p.Location.Name);
                }
            }
            else //Sorting by Item Name
            {
                if (sortDirection == "asc")
                {
                    inventories = inventories
                        .OrderBy(p => p.Item.Name);
                }
                else
                {
                    inventories = inventories
                        .OrderByDescending(p => p.Item.Name);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;


            //Handle Paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "Inventories");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);

            var pagedData = await PaginatedList<Inventory>.CreateAsync(inventories.AsNoTracking(), page ?? 1, pageSize);

            return RedirectToAction("Index", "Items");
        }

        // GET: Inventories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Inventories == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventories
                .Include(i => i.Item)
                .Include(i => i.Item.ItemImages)
                .Include(i => i.Location)
                .Include(i => i.Item.Employee)
                .Include(i => i.Item.ItemLocations).ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (inventory == null)
            {
                return NotFound();
            }

            return View(inventory);
        }

        // GET: Inventories/Create
        public IActionResult Create()
        {

            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            ViewData["ItemID"] = new SelectList(_context.Items, "ID", "Name");
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name");
            return View();
        }

        // POST: Inventories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string selectedValue, string selectedItemId, string selectedItemId1, string TypeOfOperation)
        {
            string typeofoperation = HttpContext.Session.GetString("NotifOperation");




            if (TypeOfOperation == "Activate")
            {
                // HttpContext.Session.SetString("SelectedDDLValueForSilentNotif", selectedValue);
                HttpContext.Session.SetString("ItemIdForPartialNotif", selectedItemId1);

                int itemID = Convert.ToInt32(HttpContext.Session.GetString("ItemIdForPartialNotif"));

                var invEditNotif = _context.Inventories.FirstOrDefault(i => i.ItemID == itemID);

                try
                {
                    invEditNotif.DismissNotification = DateTime.Now;

                    _context.Update(invEditNotif);
                    _context.SaveChanges();
                }
                catch (Exception)
                {

                    throw;
                }
                TempData["NotifFromPopupSuccess"] = "Activate";
            }



            else if (TypeOfOperation == "Silent")
            {
                // HttpContext.Session.SetString("SelectedDDLValueForSilentNotif", selectedValue);
                HttpContext.Session.SetString("ItemIdForPartialNotif", selectedItemId);

                int itemID = Convert.ToInt32(HttpContext.Session.GetString("ItemIdForPartialNotif"));

                var invEditNotif = _context.Inventories.FirstOrDefault(i => i.ItemID == itemID);

                int days = 10;

                if (selectedValue == "1") { days = 1; }
                else if (selectedValue == "2") { days = 2; }
                else if (selectedValue == "3") { days = 3; }
                else if (selectedValue == "7") { days = 7; }
                else if (selectedValue == "0") { days = 0; }


                if (invEditNotif != null)
                {
                    if (days == 0)
                    {
                        invEditNotif.DismissNotification = null;
                        _context.Update(invEditNotif);
                        _context.SaveChanges();
                    }
                    else
                    {
                        int numofDays = Convert.ToInt32(HttpContext.Session.GetString("SelectedDDLValueForSilentNotif"));
                        invEditNotif.DismissNotification = DateTime.Now;
                        invEditNotif.DismissNotification = DateTime.Now.AddDays(days);
                        _context.Update(invEditNotif);
                        _context.SaveChanges();
                    }
                    TempData["NotifFromPopupSuccess"] = "Silent";
                }
            }





            return RedirectToAction(nameof(Index));
        }

        // GET: Inventories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Inventories == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventories.FindAsync(id);
            if (inventory == null)
            {
                return NotFound();
            }

            ViewData["ItemID"] = new SelectList(_context.Items, "ID", "Name", inventory.ItemID);
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name", inventory.LocationID);
            return View(inventory);
        }

        // POST: Inventories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Byte[] RowVersion)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            //Go get the Event to update
            var inventoryToUpdate = await _context.Inventories.FirstOrDefaultAsync(i => i.Id == id);

            if (inventoryToUpdate == null)
            {
                return NotFound();
            }

            //Put the original RowVersion value in the OriginalValues collection for the entity
            _context.Entry(inventoryToUpdate).Property("RowVersion").OriginalValue = RowVersion;

            //Try updating it with the values posted
            if (await TryUpdateModelAsync<Inventory>(inventoryToUpdate, "",
                i => i.Cost, i => i.Quantity, i => i.ItemID, i => i.LocationID))
            {
                try
                {

                    await _context.SaveChangesAudit();
                    return RedirectToAction(nameof(Index));
                    //return RedirectToAction("Details", new { inventoryToUpdate.ItemID });

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InventoryExists(inventoryToUpdate.Id))
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
            ViewData["ItemID"] = new SelectList(_context.Items, "ID", "Name", inventoryToUpdate.ItemID);
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name", inventoryToUpdate.LocationID);
            return View(inventoryToUpdate);
        }

        // GET: Inventories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Inventories == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventories
                .Include(i => i.Item)
                .Include(i => i.Location)
                .Include(i => i.Item.Employee)
                .Include(i => i.Item.ItemLocations).ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (inventory == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Supplier)
                .Include(i => i.Employee)
                .Include(i => i.ItemLocations).ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(m => m.ID == inventory.ItemID);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // POST: Inventories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (_context.Inventories == null)
            {
                return Problem("Entity set 'CAAContext.Inventories'  is null.");
            }
            var inventory = await _context.Inventories.FindAsync(id);
            if (inventory != null)
            {
                _context.Inventories.Remove(inventory);
            }

            await _context.SaveChangesAudit();
            // return RedirectToAction(nameof(Index));
            return Redirect(ViewData["returnURL"].ToString());

        }

        public JsonResult GetItemNames(string term)
        {
            var result = from d in _context.Inventories
                         where d.Item.Name.ToUpper().Contains(term.ToUpper())
                         orderby d.Item.Name
                         select new { value = d.Item.Name, };
            return Json(result);
        }

        public JsonResult GetItemUPC(string term)
        {
            var result = from d in _context.Inventories
                         where d.Item.UPC.ToString().Contains(term.ToUpper())
                         orderby d.Item.UPC
                         select new { value = d.Item.UPC };
            return Json(result);
        }

        public JsonResult GetItemEvent(string term)
        {
            var result = from d in _context.EventLogs
                         where d.EventName.ToUpper().Contains(term.ToUpper())
                         orderby d.EventName
                         select new { value = d.EventName };
            return Json(result);
        }

        private void CheckInventoryLevel(List<Inventory> inventories)
        {
            foreach (var inventory in inventories)
            {

                if (inventory.Quantity <= inventory.Item.Category.LowCategoryThreshold)
                {
                    if (inventory.DismissNotification <= DateTime.Now)
                    {
                        inventory.IsLowInventory = true;
                        _toastNotification.AddInfoToastMessage(
                            $@"Inventory for {inventory.Item.Name} at location {inventory.Location.Name} is running low. Current quantity: {inventory.Quantity}
                                    <a href='#' onclick='redirectToEdit({inventory.Item.ID}); return false;'>Edit</a>
                                    <br><br>Qiuck Actions:
                                    <button style='background:#3630a3;color:white;'>
                                    <a href='#' onclick='redirectToSilenceNotif({inventory.Item.ID}); return false;'>Silent This Notification?</a>
                                    
                                    <button class='btn btn-outline-secondary' id='nowEditNotifSilent1' data-bs-toggle='modal' data-bs-target='#addNotifModal' type='button'>
                                        <strong>Check Messages</strong>
                                    </button>
                                    ");
                    }



                }
                else
                {
                    inventory.IsLowInventory = false;
                }
            }
        }

        public IActionResult InventoryTransfer(int id, InventoryTransfer inventoryTransfer)
        {
            var inventory = _context.Inventories.Find(id);
            if (inventory == null)
            {
                return NotFound();
            }
            inventory.Location = _context.Locations.Find(inventoryTransfer.ToLocationId);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        //Method for Viewing Full Inventory Report
        public async Task<IActionResult> InventoryReport(string SearchString1, string SearchString2, string SearchString3, int?[] LocationID, int? SupplierID, int? CategoryID, DateTime? FromDate, DateTime? ToDate,
            decimal? MinCost, decimal? MaxCost, int? MinQuantity, int? MaxQuantity, bool filterByBaseStockLevel, bool filterByLowStockLevel, bool filterByOutOfStockLevel,
                int? page, int? pageSizeID, string actionButton, string sortDirection = "asc", string sortField = "Items")
        {
            //For the Report View
            var sumQ = from i in _context.Inventories
                        .Include(i => i.Item.Supplier)
                        .Include(i => i.Item.Category)
                        .Include(i => i.Item.ItemLocations).ThenInclude(i => i.Location)
                       orderby i.Item.Name ascending
                       select new InventoryReportVM
                       {
                           ID = i.ItemID,
                           Category = i.Item.Category.Name,
                           CategoryID = i.Item.Category.Id,
                           UPC = i.Item.UPC,
                           ItemID = i.Item.ID,
                           ItemName = i.Item.Name,
                           Cost = i.Cost,
                           Quantity = i.Quantity,
                           Location = i.Location.Name,
                           LocationID = i.LocationID,
                           Supplier = i.Item.Supplier.Name,
                           SupplierID = i.Item.Supplier.ID,
                           DateReceived = (DateTime)i.Item.DateReceived,
                           Inventories = i.Item.Inventories,
                           ItemLocations = i.Item.ItemLocations,
                           Notes = i.Item.Notes
                       };

            ViewDataReturnURL();

            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering

            //Populating the DropDownLists for the Search/Filtering criteria, which are the Location
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name");
            //ViewData["LocationID"] = new SelectList(_context
            //    .Locations
            //    .OrderBy(s => s.Name), "Id", "Name");

            //Populating the DropDownLists for the Search/Filtering criteria, which are the Supplier
            //ViewData["SupplierID"] = new SelectList(_context.Suppliers, "ID", "Name");
            ViewData["SupplierID"] = new SelectList(_context
                .Suppliers
                .OrderBy(s => s.Name), "ID", "Name");

            //Populating the DropDownLists for the Search/Filtering criteria, which are the Category
            //ViewData["CategoryID"] = new SelectList(_context.Categories, "Id", "Name");
            ViewData["CategoryID"] = new SelectList(_context
                .Categories
                .OrderBy(s => s.Name), "Id", "Name");

            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "Category", "UPC", "Items", "Cost", "Quantity", "Location", "Supplier", "DateReveived" };

            //Add as many filters as needed
            if (LocationID.Length > 0)
            {
                sumQ = sumQ.Where(p => LocationID.Contains(p.LocationID));
                ViewData["Filtering"] = "btn-danger";
            }
            if (SupplierID.HasValue)
            {
                sumQ = sumQ.Where(p => p.SupplierID == SupplierID);
                ViewData["Filtering"] = "btn-danger";
            }
            if (CategoryID.HasValue)
            {
                sumQ = sumQ.Where(p => p.CategoryID == CategoryID);
                ViewData["Filtering"] = "btn-danger";
            }
            if (!String.IsNullOrEmpty(SearchString1))
            {
                long searchUPC;
                bool isNumeric = long.TryParse(SearchString1, out searchUPC);
                sumQ = sumQ.Where(p => (isNumeric && p.UPC == searchUPC));
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.UPC = SearchString1;
            }
            if (!String.IsNullOrEmpty(SearchString2))
            {
                sumQ = sumQ.Where(p => p.ItemName.ToUpper().Contains(SearchString2.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.ItemName = SearchString2;
            }
            if (!String.IsNullOrEmpty(SearchString3))
            {
                sumQ = sumQ.Where(p => p.ItemName.ToUpper().Contains(SearchString3.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
            }
            if (FromDate != null || ToDate != null)
            {
                if (FromDate != null && ToDate != null)
                {
                    // Filter records where DateReceived is within the specified range
                    sumQ = sumQ.Where(x => x.DateReceived >= FromDate && x.DateReceived <= ToDate);
                    ViewData["Filtering"] = "btn-danger";
                }
                else
                {
                    // Filter records where DateReceived is greater than or equal to FromDate or less than or equal to ToDate
                    sumQ = sumQ.Where(x => x.DateReceived >= FromDate || x.DateReceived <= ToDate);
                    ViewData["Filtering"] = "btn-danger";
                }
                // Pass the values back to the view
                ViewBag.FromDate = FromDate;
                ViewBag.ToDate = ToDate;
            }

            // Filter by cost range
            if (MinCost != null && MaxCost != null)
            {
                sumQ = sumQ.Where(x => x.Cost >= MinCost && x.Cost <= MaxCost);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinCost = MinCost;
                ViewBag.MaxCost = MaxCost;
            }
            else if (MinCost != null)
            {
                sumQ = sumQ.Where(x => x.Cost >= MinCost);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinCost = MinCost;
            }
            else if (MaxCost != null)
            {
                sumQ = sumQ.Where(x => x.Cost <= MaxCost);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MaxCost = MaxCost;
            }

            // Filter by quantity range
            if (MinQuantity != null && MaxQuantity != null)
            {
                sumQ = sumQ.Where(x => x.Quantity >= MinQuantity && x.Quantity <= MaxQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinQuantity = MinQuantity;
                ViewBag.MaxQuantity = MaxQuantity;
            }
            else if (MinQuantity != null)
            {
                sumQ = sumQ.Where(x => x.Quantity >= MinQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinQuantity = MinQuantity;
            }
            else if (MaxQuantity != null)
            {
                sumQ = sumQ.Where(x => x.Quantity <= MaxQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MaxQuantity = MaxQuantity;
            }
            //filter locations based on stock levels if needed
            if (filterByBaseStockLevel || filterByLowStockLevel || filterByOutOfStockLevel)
            {
                sumQ = sumQ.Where(l => l.Inventories.Any(i =>
                    (filterByBaseStockLevel && i.Quantity < i.BaseStockLevel) ||
                    (filterByLowStockLevel && i.Quantity < i.LowInventoryThreshold) ||
                    (filterByOutOfStockLevel && i.Quantity == 0)
                ));

                ViewData["Filtering"] = "btn-danger";
                ViewBag.FilterByBaseStockLevel = new { IsChecked = filterByBaseStockLevel };
                ViewBag.FilterByLowStockLevel = new { IsChecked = filterByLowStockLevel };
                ViewBag.FilterByOutOfStockLevel = new { IsChecked = filterByOutOfStockLevel };
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
                    sumQ = sumQ
                        .OrderBy(i => i.Category);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(i => i.Category);
                }
            }
            else if (sortField == "UPC")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.UPC);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.UPC);
                }
            }
            else if (sortField == "Cost")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Cost.ToString());
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Cost.ToString());
                }
            }
            else if (sortField == "Quantity")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Quantity);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Quantity);
                }
            }
            else if (sortField == "Location")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Location);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Location);
                }
            }
            else if (sortField == "Supplier")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Supplier);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Supplier);
                }
            }
            else if (sortField == "DateReveived")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.DateReceived);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.DateReceived);
                }
            }
            else //Sorting by Item Name
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.ItemName);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.ItemName);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            //int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "InventoryReport");//Remember for this View
            //ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            //var pagedData = await PaginatedList<InventoryReportVM>.CreateAsync(sumQ.AsNoTracking(), page ?? 1, pageSize);

            //return View(pagedData);
            return View(sumQ);
        }

        //Method for Excel Full Inventory Report
        public IActionResult DownloadInventory(string SearchString1, string SearchString2, string SearchString3, int?[] LocationID, int? SupplierID, int? CategoryID, DateTime? FromDate, DateTime? ToDate,
             decimal? MinCost, decimal? MaxCost, int? MinQuantity, int? MaxQuantity, bool filterByBaseStockLevel, bool filterByLowStockLevel, bool filterByOutOfStockLevel,
                int? page, int? pageSizeID, string actionButton, string sortDirection = "asc", string sortField = "Items")
        {
            var sumQ = from i in _context.Inventories
                        .Include(i => i.Item.Supplier)
                        .Include(i => i.Item.Category)
                        .Include(i => i.Item.ItemLocations).ThenInclude(i => i.Location)
                       orderby i.Item.Name ascending
                       select new InventoryReportVM
                       {
                           ID = i.ItemID,
                           Category = i.Item.Category.Name,
                           CategoryID = i.Item.Category.Id,
                           UPC = i.Item.UPC,
                           ItemID = i.Item.ID,
                           ItemName = i.Item.Name,
                           Cost = i.Cost,
                           Quantity = i.Quantity,
                           Location = i.Location.Name,
                           LocationID = i.LocationID,
                           Supplier = i.Item.Supplier.Name,
                           SupplierID = i.Item.Supplier.ID,
                           DateReceived = (DateTime)i.Item.DateReceived,
                           Inventories = i.Item.Inventories,
                           ItemLocations = i.Item.ItemLocations,
                           Notes = i.Item.Notes
                       };

            ViewDataReturnURL();

            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering

            //Populating the DropDownLists for the Search/Filtering criteria, which are the Location
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name");

            //Populating the DropDownLists for the Search/Filtering criteria, which are the Supplier
            ViewData["SupplierID"] = new SelectList(_context
                .Suppliers
                .OrderBy(s => s.Name), "ID", "Name");

            //Populating the DropDownLists for the Search/Filtering criteria, which are the Category
            ViewData["CategoryID"] = new SelectList(_context
                .Categories
                .OrderBy(s => s.Name), "Id", "Name");

            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "Category", "UPC", "Items", "Cost", "Quantity", "Location", "Supplier", "Date Reveived" };

            //Add as many filters as needed
            if (LocationID.Length > 0)
            {
                sumQ = sumQ.Where(p => LocationID.Contains(p.LocationID));
                ViewData["Filtering"] = "btn-danger";
            }
            if (SupplierID.HasValue)
            {
                sumQ = sumQ.Where(p => p.SupplierID == SupplierID);
                ViewData["Filtering"] = "btn-danger";
            }
            if (CategoryID.HasValue)
            {
                sumQ = sumQ.Where(p => p.CategoryID == CategoryID);
                ViewData["Filtering"] = "btn-danger";
            }
            if (!String.IsNullOrEmpty(SearchString1))
            {
                long searchUPC;
                bool isNumeric = long.TryParse(SearchString1, out searchUPC);
                sumQ = sumQ.Where(p => (isNumeric && p.UPC == searchUPC));
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.UPC = SearchString1;
            }
            if (!String.IsNullOrEmpty(SearchString2))
            {
                sumQ = sumQ.Where(p => p.ItemName.ToUpper().Contains(SearchString2.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.ItemName = SearchString2;
            }
            if (!String.IsNullOrEmpty(SearchString3))
            {
                sumQ = sumQ.Where(p => p.ItemName.ToUpper().Contains(SearchString3.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
            }
            if (FromDate != null || ToDate != null)
            {
                if (FromDate != null && ToDate != null)
                {
                    // Filter records where DateReceived is within the specified range
                    sumQ = sumQ.Where(x => x.DateReceived >= FromDate && x.DateReceived <= ToDate);
                    ViewData["Filtering"] = "btn-danger";
                }
                else
                {
                    // Filter records where DateReceived is greater than or equal to FromDate or less than or equal to ToDate
                    sumQ = sumQ.Where(x => x.DateReceived >= FromDate || x.DateReceived <= ToDate);
                    ViewData["Filtering"] = "btn-danger";
                }
                // Pass the values back to the view
                ViewBag.FromDate = FromDate;
                ViewBag.ToDate = ToDate;
            }

            // Filter by cost range
            if (MinCost != null && MaxCost != null)
            {
                sumQ = sumQ.Where(x => x.Cost >= MinCost && x.Cost <= MaxCost);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinCost = MinCost;
                ViewBag.MaxCost = MaxCost;
            }
            else if (MinCost != null)
            {
                sumQ = sumQ.Where(x => x.Cost >= MinCost);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinCost = MinCost;
            }
            else if (MaxCost != null)
            {
                sumQ = sumQ.Where(x => x.Cost <= MaxCost);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MaxCost = MaxCost;
            }

            // Filter by quantity range
            if (MinQuantity != null && MaxQuantity != null)
            {
                sumQ = sumQ.Where(x => x.Quantity >= MinQuantity && x.Quantity <= MaxQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinQuantity = MinQuantity;
                ViewBag.MaxQuantity = MaxQuantity;
            }
            else if (MinQuantity != null)
            {
                sumQ = sumQ.Where(x => x.Quantity >= MinQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinQuantity = MinQuantity;
            }
            else if (MaxQuantity != null)
            {
                sumQ = sumQ.Where(x => x.Quantity <= MaxQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MaxQuantity = MaxQuantity;
            }
            //filter locations based on stock levels if needed
            if (filterByBaseStockLevel || filterByLowStockLevel || filterByOutOfStockLevel)
            {
                sumQ = sumQ.Where(l => l.Inventories.Any(i =>
                    (filterByBaseStockLevel && i.Quantity < i.BaseStockLevel) ||
                    (filterByLowStockLevel && i.Quantity < i.LowInventoryThreshold) ||
                    (filterByOutOfStockLevel && i.Quantity == 0)
                ));

                ViewData["Filtering"] = "btn-danger";
                ViewBag.FilterByBaseStockLevel = new { IsChecked = filterByBaseStockLevel };
                ViewBag.FilterByLowStockLevel = new { IsChecked = filterByLowStockLevel };
                ViewBag.FilterByOutOfStockLevel = new { IsChecked = filterByOutOfStockLevel };
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
                    sumQ = sumQ
                        .OrderBy(i => i.Category);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(i => i.Category);
                }
            }
            else if (sortField == "UPC")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.UPC);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.UPC);
                }
            }
            else if (sortField == "Cost")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Cost.ToString());
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Cost.ToString());
                }
            }
            else if (sortField == "Quantity")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Quantity);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Quantity);
                }
            }
            else if (sortField == "Location")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Location);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Location);
                }
            }
            else if (sortField == "Supplier")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Supplier);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Supplier);
                }
            }
            else if (sortField == "DateReveived")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.DateReceived);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.DateReceived);
                }
            }
            else //Sorting by Item Name
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.ItemName);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.ItemName);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            var items = from i in sumQ
                            //orderby i.Location, i.ItemName ascending
                        select new
                        {
                            Category = i.Category,
                            UPC = i.UPC,
                            ItemName = i.ItemName,
                            Cost = i.Cost,
                            Quantity = i.Quantity,
                            Location = i.Location,
                            Supplier = i.Supplier,
                            DateReceived = (DateTime)i.DateReceived,
                            Notes = i.Notes
                        };

            //How many rows?
            int numRows = items.Count();

            if (numRows > 0) //We have data
            {
                //Create a new spreadsheet from scratch.
                using (ExcelPackage excel = new ExcelPackage())
                {
                    var workSheet = excel.Workbook.Worksheets.Add("Inventory");

                    //Note: Cells[row, column]
                    workSheet.Cells[3, 1].LoadFromCollection(items, true);

                    //Style fee column for upc
                    workSheet.Column(2).Style.Numberformat.Format = "##############0";

                    //Style 8th column for dates (DateRecieved)
                    workSheet.Column(8).Style.Numberformat.Format = "yyyy-mm-dd";

                    //Style fee column for currency
                    workSheet.Column(4).Style.Numberformat.Format = "$###,##0.00";

                    //Note: You can define a BLOCK of cells: Cells[startRow, startColumn, endRow, endColumn]
                    //Make Item Category Bold
                    workSheet.Cells[4, 1, numRows + 3, 1].Style.Font.Bold = true;
                    //Make Item Names Bold
                    workSheet.Cells[4, 3, numRows + 3, 3].Style.Font.Bold = true;
                    //Make Item Locations Bold
                    workSheet.Cells[4, 6, numRows + 3, 6].Style.Font.Bold = true;

                    //Make Item Quantity Bold/Colour coded
                    workSheet.Cells[4, 5, numRows + 3, 5].Style.Font.Bold = true;

                    using (ExcelRange totalfees = workSheet.Cells[numRows + 4, 4])//
                    {
                        //Total Cost Text
                        workSheet.Cells[numRows + 4, 3].Value = "Total Cost:";
                        workSheet.Cells[numRows + 4, 3].Style.Font.Bold = true;
                        workSheet.Cells[numRows + 4, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        //Total Cost Sum - get cost * qty for each row
                        decimal totalCost = 0m;
                        for (int row = 4; row <= numRows + 3; row++)
                        {
                            decimal cost = (decimal)workSheet.Cells[row, 4].Value;
                            int qty = (int)workSheet.Cells[row, 5].Value;
                            totalCost += cost * qty;
                        }

                        totalfees.Value = totalCost;
                        totalfees.Style.Font.Bold = true;
                        totalfees.Style.Numberformat.Format = "$###,###,##0.00";
                        var range = workSheet.Cells[numRows + 4, 4, numRows + 4, 5];
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    //total Quantity
                    using (ExcelRange totalQty = workSheet.Cells[numRows + 5, 4])//
                    {
                        //Total Cost Text
                        workSheet.Cells[numRows + 5, 3].Value = "Total Quantity:";
                        workSheet.Cells[numRows + 5, 3].Style.Font.Bold = true;
                        workSheet.Cells[numRows + 5, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        //Total Cost Sum - get cost * qty for each row
                        totalQty.Formula = "Sum(" + (workSheet.Cells[4, 5].Address) + ":" + workSheet.Cells[numRows + 3, 5].Address + ")";
                        totalQty.Style.Font.Bold = true;
                        totalQty.Style.Numberformat.Format = "###,###,##0";
                        var range = workSheet.Cells[numRows + 5, 4, numRows + 5, 5];
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    //Set Style and backgound colour of headings
                    using (ExcelRange headings = workSheet.Cells[3, 1, 3, 9])
                    {
                        headings.Style.Font.Bold = true;
                        var fill = headings.Style.Fill;
                        fill.PatternType = ExcelFillStyle.Solid;
                        fill.BackgroundColor.SetColor(Color.LightBlue);
                    }

                    //Autofit columns
                    workSheet.Cells.AutoFitColumns();
                    //Note: You can manually set width of columns as well
                    //workSheet.Column(7).Width = 10;

                    //Add a title and timestamp at the top of the report
                    workSheet.Cells[1, 1].Value = "Inventory Report";
                    using (ExcelRange Rng = workSheet.Cells[1, 1, 1, 9])
                    {
                        Rng.Merge = true; //Merge columns start and end range
                        Rng.Style.Font.Bold = true; //Font should be bold
                        Rng.Style.Font.Size = 18;
                        Rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                    //Since the time zone where the server is running can be different, adjust to 
                    //Local for us.
                    DateTime utcDate = DateTime.UtcNow;
                    TimeZoneInfo esTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                    DateTime localDate = TimeZoneInfo.ConvertTimeFromUtc(utcDate, esTimeZone);
                    using (ExcelRange Rng = workSheet.Cells[2, 9])
                    {
                        Rng.Value = "Created: " + localDate.ToShortTimeString() + " on " +
                            localDate.ToShortDateString();
                        Rng.Style.Font.Bold = true; //Font should be bold
                        Rng.Style.Font.Size = 12;
                        Rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    }

                    //Ok, time to download the Excel

                    try
                    {
                        Byte[] theData = excel.GetAsByteArray();
                        string filename = "InventoryReport.xlsx";
                        string mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        return File(theData, mimeType, filename);
                    }
                    catch (Exception)
                    {
                        return BadRequest("Could not build and download the file.");
                    }
                }
            }
            return NotFound("No data.");
        }

        //Method for Viewing Inventory Levels Report
        public async Task<IActionResult> InventoryLevelsReport(string SearchString1, string SearchString2, string SearchString3, int?[] LocationID, int? MinQuantity, int? MaxQuantity,
             bool filterByBaseStockLevel, bool filterByLowStockLevel, bool filterByOutOfStockLevel, int? page, int? pageSizeID, string actionButton, string sortDirection = "asc", string sortField = "Item Name")
        {
            //For the Report View
            var sumQ = from i in _context.Inventories
                        .Include(i => i.Item.Supplier)
                        .Include(i => i.Item.Category)
                        .Include(i => i.Item.Employee)
                        .Include(i => i.Item.ItemLocations).ThenInclude(i => i.Location)
                       orderby i.Item.Name ascending
                       select new InventoryReportVM
                       {
                           ID = i.ItemID,
                           Category = i.Item.Category.Name,
                           CategoryID = i.Item.Category.Id,
                           UPC = i.Item.UPC,
                           ItemID = i.Item.ID,
                           ItemName = i.Item.Name,
                           Cost = i.Cost,
                           Quantity = i.Quantity,
                           Location = i.Location.Name,
                           LocationID = i.LocationID,
                           Supplier = i.Item.Supplier.Name,
                           SupplierID = i.Item.Supplier.ID,
                           DateReceived = (DateTime)i.Item.DateReceived,
                           Inventories = i.Item.Inventories,
                           ItemLocations = i.Item.ItemLocations,
                           Notes = i.Item.Notes
                       };

            ViewDataReturnURL();

            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering

            //Populating the DropDownLists for the Search/Filtering criteria, which are the Location
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name");

            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "UPC", "Item Name", "Quantity", "Location" };

            //Add as many filters as needed
            if (LocationID.Length > 0)
            {
                sumQ = sumQ.Where(p => LocationID.Contains(p.LocationID));
                ViewData["Filtering"] = "btn-danger";
            }

            if (!String.IsNullOrEmpty(SearchString1))
            {
                long searchUPC;
                bool isNumeric = long.TryParse(SearchString1, out searchUPC);
                sumQ = sumQ.Where(p => (isNumeric && p.UPC == searchUPC));
                ViewData["Filtering"] = "btn-danger";
            }
            if (!String.IsNullOrEmpty(SearchString2))
            {
                sumQ = sumQ.Where(p => p.ItemName.ToUpper().Contains(SearchString2.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
            }
            if (!String.IsNullOrEmpty(SearchString3))
            {
                sumQ = sumQ.Where(p => p.ItemName.ToUpper().Contains(SearchString3.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
            }

            // Filter by quantity range
            if (MinQuantity != null && MaxQuantity != null)
            {
                sumQ = sumQ.Where(x => x.Quantity >= MinQuantity && x.Quantity <= MaxQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinQuantity = MinQuantity;
                ViewBag.MaxQuantity = MaxQuantity;
            }
            else if (MinQuantity != null)
            {
                sumQ = sumQ.Where(x => x.Quantity >= MinQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinQuantity = MinQuantity;
            }
            else if (MaxQuantity != null)
            {
                sumQ = sumQ.Where(x => x.Quantity <= MaxQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MaxQuantity = MaxQuantity;
            }
            //filter locations based on stock levels if needed
            if (filterByBaseStockLevel || filterByLowStockLevel || filterByOutOfStockLevel)
            {
                sumQ = sumQ.Where(l => l.Inventories.Any(i =>
                    (filterByBaseStockLevel && i.Quantity < i.BaseStockLevel) ||
                    (filterByLowStockLevel && i.Quantity < i.LowInventoryThreshold) ||
                    (filterByOutOfStockLevel && i.Quantity == 0)
                ));

                ViewData["Filtering"] = "btn-danger";
                ViewBag.FilterByBaseStockLevel = new { IsChecked = filterByBaseStockLevel };
                ViewBag.FilterByLowStockLevel = new { IsChecked = filterByLowStockLevel };
                ViewBag.FilterByOutOfStockLevel = new { IsChecked = filterByOutOfStockLevel };
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
            if (sortField == "UPC")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.UPC);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.UPC);
                }
            }
            else if (sortField == "Quantity")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Quantity);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Quantity);
                }
            }
            else if (sortField == "Location")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Location);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Location);
                }
            }
            else //Sorting by Item Name
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.ItemName);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.ItemName);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            //int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "InventoryLevelsReport");//Remember for this View
            //ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            //var pagedData = await PaginatedList<InventoryReportVM>.CreateAsync(sumQ.AsNoTracking(), page ?? 1, pageSize);

            //return View(pagedData);
            return View(sumQ);
        }

        //Method for Excel Inventory Levels Report
        public ActionResult DownloadInventoryLevels(string SearchString1, string SearchString2, string SearchString3, int?[] LocationID, int? MinQuantity, int? MaxQuantity,
             bool filterByBaseStockLevel, bool filterByLowStockLevel, bool filterByOutOfStockLevel, int? page, int? pageSizeID, string actionButton, string sortDirection = "asc", string sortField = "Item Name")
        {
            //For Report
            var sumQ = from i in _context.Inventories
                        .Include(i => i.Item.Supplier)
                        .Include(i => i.Item.Category)
                        .Include(i => i.Item.Employee)
                        .Include(i => i.Item.ItemLocations).ThenInclude(i => i.Location)
                       orderby i.Location, i.Quantity ascending
                       select new InventoryReportVM
                       {
                           ID = i.ItemID,
                           Category = i.Item.Category.Name,
                           CategoryID = i.Item.Category.Id,
                           UPC = i.Item.UPC,
                           ItemID = i.Item.ID,
                           ItemName = i.Item.Name,
                           Cost = i.Cost,
                           Quantity = i.Quantity,
                           Location = i.Location.Name,
                           LocationID = i.LocationID,
                           Supplier = i.Item.Supplier.Name,
                           SupplierID = i.Item.Supplier.ID,
                           DateReceived = (DateTime)i.Item.DateReceived,
                           Inventories = i.Item.Inventories,
                           ItemLocations = i.Item.ItemLocations,
                           Notes = i.Item.Notes
                       };

            ViewDataReturnURL();

            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering

            //Populating the DropDownLists for the Search/Filtering criteria, which are the Location
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name");

            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "UPC", "Item Name", "Quantity", "Location" };

            //Add as many filters as needed
            if (LocationID.Length > 0)
            {
                sumQ = sumQ.Where(p => LocationID.Contains(p.LocationID));
                ViewData["Filtering"] = "btn-danger";
            }

            if (!String.IsNullOrEmpty(SearchString1))
            {
                long searchUPC;
                bool isNumeric = long.TryParse(SearchString1, out searchUPC);
                sumQ = sumQ.Where(p => (isNumeric && p.UPC == searchUPC));
                ViewData["Filtering"] = "btn-danger";
            }
            if (!String.IsNullOrEmpty(SearchString2))
            {
                sumQ = sumQ.Where(p => p.ItemName.ToUpper().Contains(SearchString2.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
            }
            if (!String.IsNullOrEmpty(SearchString3))
            {
                sumQ = sumQ.Where(p => p.ItemName.ToUpper().Contains(SearchString3.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
            }

            // Filter by quantity range
            if (MinQuantity != null && MaxQuantity != null)
            {
                sumQ = sumQ.Where(x => x.Quantity >= MinQuantity && x.Quantity <= MaxQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinQuantity = MinQuantity;
                ViewBag.MaxQuantity = MaxQuantity;
            }
            else if (MinQuantity != null)
            {
                sumQ = sumQ.Where(x => x.Quantity >= MinQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinQuantity = MinQuantity;
            }
            else if (MaxQuantity != null)
            {
                sumQ = sumQ.Where(x => x.Quantity <= MaxQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MaxQuantity = MaxQuantity;
            }
            //filter locations based on stock levels if needed
            if (filterByBaseStockLevel || filterByLowStockLevel || filterByOutOfStockLevel)
            {
                sumQ = sumQ.Where(l => l.Inventories.Any(i =>
                    (filterByBaseStockLevel && i.Quantity < i.BaseStockLevel) ||
                    (filterByLowStockLevel && i.Quantity < i.LowInventoryThreshold) ||
                    (filterByOutOfStockLevel && i.Quantity == 0)
                ));

                ViewData["Filtering"] = "btn-danger";
                ViewBag.FilterByBaseStockLevel = new { IsChecked = filterByBaseStockLevel };
                ViewBag.FilterByLowStockLevel = new { IsChecked = filterByLowStockLevel };
                ViewBag.FilterByOutOfStockLevel = new { IsChecked = filterByOutOfStockLevel };
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
            if (sortField == "UPC")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.UPC);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.UPC);
                }
            }
            else if (sortField == "Quantity")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Quantity);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Quantity);
                }
            }
            else if (sortField == "Location")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Location);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Location);
                }
            }
            else //Sorting by Item Name
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.ItemName);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.ItemName);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            //Get the inventory
            var items = from i in sumQ
                            //orderby i.Location, i.Quantity ascending
                        select new
                        {
                            UPC = i.UPC,
                            Item = i.ItemName,
                            Quantity = i.Quantity,
                            Location = i.Location
                        };
            //How many rows?
            int numRows = items.Count();

            if (numRows > 0) //We have data
            {
                //Create a new spreadsheet from scratch.
                using (ExcelPackage excel = new ExcelPackage())
                {
                    var workSheet = excel.Workbook.Worksheets.Add("Inventory Levels");

                    //Note: Cells[row, column]
                    workSheet.Cells[3, 1].LoadFromCollection(items, true);

                    //Style fee column for upc
                    workSheet.Column(1).Style.Numberformat.Format = "##############0";

                    //Style fee column for currency
                    workSheet.Column(3).Style.Numberformat.Format = "###,###,##0";

                    //Note: You can define a BLOCK of cells: Cells[startRow, startColumn, endRow, endColumn]
                    //Make Item Name Bold
                    workSheet.Cells[4, 2, numRows + 3, 2].Style.Font.Bold = true;

                    //Make Item Quantity Bold/Colour coded
                    workSheet.Cells[4, 3, numRows + 3, 3].Style.Font.Bold = true;

                    //Note: these are fine if you are only 'doing' one thing to the range of cells.
                    //Otherwise you should USE a range object for efficiency
                    //Total Cost for all Items in Inventory
                    //workSheet.Cells[4, 4, numRows + 3, 5].Calculate();
                    using (ExcelRange totalQty = workSheet.Cells[numRows + 4, 3])//
                    {
                        //Total Cost Text
                        workSheet.Cells[numRows + 4, 2].Value = "Total Quantity:";
                        workSheet.Cells[numRows + 4, 2].Style.Font.Bold = true;
                        workSheet.Cells[numRows + 4, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        //Total Cost Sum - get cost * qty for each row
                        totalQty.Formula = "Sum(" + (workSheet.Cells[4, 3].Address) + ":" + workSheet.Cells[numRows + 3, 3].Address + ")";
                        totalQty.Style.Font.Bold = true;
                        totalQty.Style.Numberformat.Format = "###,###,##0";
                        var range = workSheet.Cells[numRows + 4, 3, numRows + 4, 4];
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    //Set Style and backgound colour of headings
                    using (ExcelRange headings = workSheet.Cells[3, 1, 3, 4])
                    {
                        headings.Style.Font.Bold = true;
                        var fill = headings.Style.Fill;
                        fill.PatternType = ExcelFillStyle.Solid;
                        fill.BackgroundColor.SetColor(Color.LightBlue);
                    }

                    //Autofit columns
                    workSheet.Cells.AutoFitColumns();
                    //Note: You can manually set width of columns as well
                    //workSheet.Column(7).Width = 10;

                    //Add a title and timestamp at the top of the report
                    workSheet.Cells[1, 1].Value = "Inventory Levels Report";
                    using (ExcelRange Rng = workSheet.Cells[1, 1, 1, 4])
                    {
                        Rng.Merge = true; //Merge columns start and end range
                        Rng.Style.Font.Bold = true; //Font should be bold
                        Rng.Style.Font.Size = 18;
                        Rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                    //Since the time zone where the server is running can be different, adjust to 
                    //Local for us.
                    DateTime utcDate = DateTime.UtcNow;
                    TimeZoneInfo esTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                    DateTime localDate = TimeZoneInfo.ConvertTimeFromUtc(utcDate, esTimeZone);
                    using (ExcelRange Rng = workSheet.Cells[2, 4])
                    {
                        Rng.Value = "Created: " + localDate.ToShortTimeString() + " on " +
                            localDate.ToShortDateString();
                        Rng.Style.Font.Bold = true; //Font should be bold
                        Rng.Style.Font.Size = 12;
                        Rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    }

                    //Ok, time to download the Excel

                    try
                    {
                        Byte[] theData = excel.GetAsByteArray();
                        string filename = "InventoryLevelsReport.xlsx";
                        string mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        return File(theData, mimeType, filename);
                    }
                    catch (Exception)
                    {
                        return BadRequest("Could not build and download the file.");
                    }
                }
            }
            return NotFound("No data.");
        }

        //Method for Viewing Inventory Costs Report
        public async Task<IActionResult> InventoryCostsReport(string SearchString1, string SearchString2, string SearchString3, int?[] LocationID, decimal? MinCost,
            bool filterByBaseStockLevel, bool filterByLowStockLevel, bool filterByOutOfStockLevel, decimal? MaxCost, int? MinQuantity, int? MaxQuantity, int? page, int? pageSizeID, string actionButton, string sortDirection = "asc", string sortField = "Item Name")
        {
            //For the Report View
            var sumQ = from i in _context.Inventories
                        .Include(i => i.Item.Supplier)
                        .Include(i => i.Item.Category)
                        .Include(i => i.Item.ItemLocations).ThenInclude(i => i.Location)
                       orderby i.Item.Name ascending
                       select new InventoryReportVM
                       {
                           ID = i.ItemID,
                           Category = i.Item.Category.Name,
                           CategoryID = i.Item.Category.Id,
                           UPC = i.Item.UPC,
                           ItemID = i.Item.ID,
                           ItemName = i.Item.Name,
                           Cost = i.Cost,
                           Quantity = i.Quantity,
                           Location = i.Location.Name,
                           LocationID = i.LocationID,
                           Supplier = i.Item.Supplier.Name,
                           SupplierID = i.Item.Supplier.ID,
                           DateReceived = (DateTime)i.Item.DateReceived,
                           Inventories = i.Item.Inventories,
                           ItemLocations = i.Item.ItemLocations,
                           Notes = i.Item.Notes
                       };

            ViewDataReturnURL();

            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering

            //Populating the DropDownLists for the Search/Filtering criteria, which are the Location
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name");

            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "UPC", "Item Name", "Cost", "Quantity", "Location" };

            //Add as many filters as needed
            if (LocationID.Length > 0)
            {
                sumQ = sumQ.Where(p => LocationID.Contains(p.LocationID));
                ViewData["Filtering"] = "btn-danger";
            }

            if (!String.IsNullOrEmpty(SearchString1))
            {
                long searchUPC;
                bool isNumeric = long.TryParse(SearchString1, out searchUPC);
                sumQ = sumQ.Where(p => (isNumeric && p.UPC == searchUPC));
                ViewData["Filtering"] = "btn-danger";
            }
            if (!String.IsNullOrEmpty(SearchString2))
            {
                sumQ = sumQ.Where(p => p.ItemName.ToUpper().Contains(SearchString2.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
            }
            if (!String.IsNullOrEmpty(SearchString3))
            {
                sumQ = sumQ.Where(p => p.ItemName.ToUpper().Contains(SearchString3.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
            }
            // Filter by cost range
            if (MinCost != null && MaxCost != null)
            {
                sumQ = sumQ.Where(x => x.Cost >= MinCost && x.Cost <= MaxCost);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinCost = MinCost;
                ViewBag.MaxCost = MaxCost;
            }
            else if (MinCost != null)
            {
                sumQ = sumQ.Where(x => x.Cost >= MinCost);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinCost = MinCost;
            }
            else if (MaxCost != null)
            {
                sumQ = sumQ.Where(x => x.Cost <= MaxCost);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MaxCost = MaxCost;
            }

            // Filter by quantity range
            if (MinQuantity != null && MaxQuantity != null)
            {
                sumQ = sumQ.Where(x => x.Quantity >= MinQuantity && x.Quantity <= MaxQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinQuantity = MinQuantity;
                ViewBag.MaxQuantity = MaxQuantity;
            }
            else if (MinQuantity != null)
            {
                sumQ = sumQ.Where(x => x.Quantity >= MinQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinQuantity = MinQuantity;
            }
            else if (MaxQuantity != null)
            {
                sumQ = sumQ.Where(x => x.Quantity <= MaxQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MaxQuantity = MaxQuantity;
            }
            //filter locations based on stock levels if needed
            if (filterByBaseStockLevel || filterByLowStockLevel || filterByOutOfStockLevel)
            {
                sumQ = sumQ.Where(l => l.Inventories.Any(i =>
                    (filterByBaseStockLevel && i.Quantity < i.BaseStockLevel) ||
                    (filterByLowStockLevel && i.Quantity < i.LowInventoryThreshold) ||
                    (filterByOutOfStockLevel && i.Quantity == 0)
                ));

                ViewData["Filtering"] = "btn-danger";
                ViewBag.FilterByBaseStockLevel = new { IsChecked = filterByBaseStockLevel };
                ViewBag.FilterByLowStockLevel = new { IsChecked = filterByLowStockLevel };
                ViewBag.FilterByOutOfStockLevel = new { IsChecked = filterByOutOfStockLevel };
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
            if (sortField == "UPC")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.UPC);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.UPC);
                }
            }
            else if (sortField == "Cost")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Cost.ToString());
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Cost.ToString());
                }
            }
            else if (sortField == "Quantity")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Quantity);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Quantity);
                }
            }
            else if (sortField == "Location")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Location);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Location);
                }
            }
            else //Sorting by Item Name
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.ItemName);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.ItemName);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            //int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "InventoryCostsReport");//Remember for this View
            //ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            //var pagedData = await PaginatedList<InventoryReportVM>.CreateAsync(sumQ.AsNoTracking(), page ?? 1, pageSize);

            //return View(pagedData);
            return View(sumQ);
        }

        //Method for Excel Inventory Costs Report
        public ActionResult DownloadInventoryCosts(string SearchString1, string SearchString2, string SearchString3, int?[] LocationID, decimal? MinCost,
            bool filterByBaseStockLevel, bool filterByLowStockLevel, bool filterByOutOfStockLevel, decimal? MaxCost, int? MinQuantity, int? MaxQuantity, int? page, int? pageSizeID, string actionButton, string sortDirection = "asc", string sortField = "Item Name")
        {
            //For the Report View
            var sumQ = from i in _context.Inventories
                        .Include(i => i.Item.Supplier)
                        .Include(i => i.Item.Category)
                        .Include(i => i.Item.ItemLocations).ThenInclude(i => i.Location)
                       orderby i.Item.Name ascending
                       select new InventoryReportVM
                       {
                           ID = i.ItemID,
                           Category = i.Item.Category.Name,
                           CategoryID = i.Item.Category.Id,
                           UPC = i.Item.UPC,
                           ItemID = i.Item.ID,
                           ItemName = i.Item.Name,
                           Cost = i.Cost,
                           Quantity = i.Quantity,
                           Location = i.Location.Name,
                           LocationID = i.LocationID,
                           Supplier = i.Item.Supplier.Name,
                           SupplierID = i.Item.Supplier.ID,
                           DateReceived = (DateTime)i.Item.DateReceived,
                           Inventories = i.Item.Inventories,
                           ItemLocations = i.Item.ItemLocations,
                           Notes = i.Item.Notes
                       };

            ViewDataReturnURL();

            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering

            //Populating the DropDownLists for the Search/Filtering criteria, which are the Location
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name");

            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "UPC", "Item Name", "Cost", "Quantity", "Location" };

            //Add as many filters as needed
            if (LocationID.Length > 0)
            {
                sumQ = sumQ.Where(p => LocationID.Contains(p.LocationID));
                ViewData["Filtering"] = "btn-danger";
            }

            if (!String.IsNullOrEmpty(SearchString1))
            {
                long searchUPC;
                bool isNumeric = long.TryParse(SearchString1, out searchUPC);
                sumQ = sumQ.Where(p => (isNumeric && p.UPC == searchUPC));
                ViewData["Filtering"] = "btn-danger";
            }
            if (!String.IsNullOrEmpty(SearchString2))
            {
                sumQ = sumQ.Where(p => p.ItemName.ToUpper().Contains(SearchString2.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
            }
            if (!String.IsNullOrEmpty(SearchString3))
            {
                sumQ = sumQ.Where(p => p.ItemName.ToUpper().Contains(SearchString3.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
            }
            // Filter by cost range
            if (MinCost != null && MaxCost != null)
            {
                sumQ = sumQ.Where(x => x.Cost >= MinCost && x.Cost <= MaxCost);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinCost = MinCost;
                ViewBag.MaxCost = MaxCost;
            }
            else if (MinCost != null)
            {
                sumQ = sumQ.Where(x => x.Cost >= MinCost);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinCost = MinCost;
            }
            else if (MaxCost != null)
            {
                sumQ = sumQ.Where(x => x.Cost <= MaxCost);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MaxCost = MaxCost;
            }

            // Filter by quantity range
            if (MinQuantity != null && MaxQuantity != null)
            {
                sumQ = sumQ.Where(x => x.Quantity >= MinQuantity && x.Quantity <= MaxQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinQuantity = MinQuantity;
                ViewBag.MaxQuantity = MaxQuantity;
            }
            else if (MinQuantity != null)
            {
                sumQ = sumQ.Where(x => x.Quantity >= MinQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinQuantity = MinQuantity;
            }
            else if (MaxQuantity != null)
            {
                sumQ = sumQ.Where(x => x.Quantity <= MaxQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MaxQuantity = MaxQuantity;
            }
            //filter locations based on stock levels if needed
            if (filterByBaseStockLevel || filterByLowStockLevel || filterByOutOfStockLevel)
            {
                sumQ = sumQ.Where(l => l.Inventories.Any(i =>
                    (filterByBaseStockLevel && i.Quantity < i.BaseStockLevel) ||
                    (filterByLowStockLevel && i.Quantity < i.LowInventoryThreshold) ||
                    (filterByOutOfStockLevel && i.Quantity == 0)
                ));

                ViewData["Filtering"] = "btn-danger";
                ViewBag.FilterByBaseStockLevel = new { IsChecked = filterByBaseStockLevel };
                ViewBag.FilterByLowStockLevel = new { IsChecked = filterByLowStockLevel };
                ViewBag.FilterByOutOfStockLevel = new { IsChecked = filterByOutOfStockLevel };
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
            if (sortField == "UPC")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.UPC);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.UPC);
                }
            }
            else if (sortField == "Cost")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Cost.ToString());
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Cost.ToString());
                }
            }
            else if (sortField == "Quantity")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Quantity);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Quantity);
                }
            }
            else if (sortField == "Location")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Location);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Location);
                }
            }
            else //Sorting by Item Name
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.ItemName);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.ItemName);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            //Get the inventory
            var items = from i in sumQ
                            //orderby i.Location, i.ItemName, i.Quantity ascending
                        select new
                        {
                            UPC = i.UPC,
                            Item = i.ItemName,
                            Cost = i.Cost,
                            Quantity = i.Quantity,
                            Location = i.Location
                        };
            //How many rows?
            int numRows = items.Count();

            if (numRows > 0) //We have data
            {
                //Create a new spreadsheet from scratch.
                using (ExcelPackage excel = new ExcelPackage())
                {
                    var workSheet = excel.Workbook.Worksheets.Add("Inventory Costs");

                    //Note: Cells[row, column]
                    workSheet.Cells[3, 1].LoadFromCollection(items, true);

                    //Style fee column for upc
                    workSheet.Column(1).Style.Numberformat.Format = "##############0";

                    //Style fee column for quantity
                    workSheet.Column(4).Style.Numberformat.Format = "###,###,##0";

                    //Style fee column for currency
                    workSheet.Column(3).Style.Numberformat.Format = "$###,###,##0.00";

                    //Note: You can define a BLOCK of cells: Cells[startRow, startColumn, endRow, endColumn]
                    //Make Item Name Bold
                    workSheet.Cells[4, 2, numRows + 3, 2].Style.Font.Bold = true;

                    //Make Item Quantity Bold/Colour coded
                    workSheet.Cells[4, 3, numRows + 3, 4].Style.Font.Bold = true;
                    //var item = from i in sumQ
                    //           orderby i.Location, i.ItemName, i.Quantity ascending
                    //           select i.Quantity;
                    //int row = 4;
                    //foreach (var qty in item)
                    //{
                    //    if (row <= (numRows + 3))
                    //    {
                    //        if (qty == 0)
                    //        {
                    //            workSheet.Cells[row, 4].Style.Font.Color.SetColor(Color.Red);
                    //            row++;
                    //        }
                    //        else if ((qty <= 10) && (qty > 0))
                    //        {
                    //            workSheet.Cells[row, 4].Style.Font.Color.SetColor(Color.Orange);
                    //            row++;
                    //        }
                    //        else if (qty > 10)
                    //        {
                    //            workSheet.Cells[row, 4].Style.Font.Color.SetColor(Color.Green);
                    //            row++;
                    //        }
                    //    }
                    //    else
                    //    {
                    //        break;
                    //    }
                    //}

                    //Note: these are fine if you are only 'doing' one thing to the range of cells.
                    //Otherwise you should USE a range object for efficiency
                    //Total Cost for all Items in Inventory
                    //workSheet.Cells[4, 4, numRows + 3, 5].Calculate();
                    //Total Cost
                    using (ExcelRange totalfees = workSheet.Cells[numRows + 4, 4])//
                    {
                        //Total Cost Text
                        workSheet.Cells[numRows + 4, 3].Value = "Total Cost:";
                        workSheet.Cells[numRows + 4, 3].Style.Font.Bold = true;
                        workSheet.Cells[numRows + 4, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        //Total Cost Sum - get cost * qty for each row
                        decimal totalCost = 0m;
                        for (int row = 4; row <= numRows + 3; row++)
                        {
                            decimal cost = (decimal)workSheet.Cells[row, 3].Value;
                            int qty = (int)workSheet.Cells[row, 4].Value;
                            totalCost += cost * qty;
                        }

                        totalfees.Value = totalCost;
                        totalfees.Style.Font.Bold = true;
                        totalfees.Style.Numberformat.Format = "$###,###,##0.00";
                        var range = workSheet.Cells[numRows + 4, 4, numRows + 4, 5];
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    //Set Style and backgound colour of headings
                    using (ExcelRange headings = workSheet.Cells[3, 1, 3, 5])
                    {
                        headings.Style.Font.Bold = true;
                        var fill = headings.Style.Fill;
                        fill.PatternType = ExcelFillStyle.Solid;
                        fill.BackgroundColor.SetColor(Color.LightBlue);
                    }

                    //Autofit columns
                    workSheet.Cells.AutoFitColumns();
                    //Note: You can manually set width of columns as well
                    //workSheet.Column(7).Width = 10;

                    //Add a title and timestamp at the top of the report
                    workSheet.Cells[1, 1].Value = "Inventory Costs Report";
                    using (ExcelRange Rng = workSheet.Cells[1, 1, 1, 5])
                    {
                        Rng.Merge = true; //Merge columns start and end range
                        Rng.Style.Font.Bold = true; //Font should be bold
                        Rng.Style.Font.Size = 18;
                        Rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                    //Since the time zone where the server is running can be different, adjust to 
                    //Local for us.
                    DateTime utcDate = DateTime.UtcNow;
                    TimeZoneInfo esTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                    DateTime localDate = TimeZoneInfo.ConvertTimeFromUtc(utcDate, esTimeZone);
                    using (ExcelRange Rng = workSheet.Cells[2, 5])
                    {
                        Rng.Value = "Created: " + localDate.ToShortTimeString() + " on " +
                            localDate.ToShortDateString();
                        Rng.Style.Font.Bold = true; //Font should be bold
                        Rng.Style.Font.Size = 12;
                        Rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    }

                    //Ok, time to download the Excel

                    try
                    {
                        Byte[] theData = excel.GetAsByteArray();
                        string filename = "InventoryCostsReport.xlsx";
                        string mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        return File(theData, mimeType, filename);
                    }
                    catch (Exception)
                    {
                        return BadRequest("Could not build and download the file.");
                    }
                }
            }
            return NotFound("No data.");
        }

        //Method for Viewing Inventory Events Report
        public async Task<IActionResult> InventoryEventsReport(string SearchString1, string SearchString2, string SearchString3, int?[] LocationID, DateTime? returnToDate, DateTime? reservedToDate,
             DateTime? returnFromDate, DateTime? reservedFromDate, int? MinQuantity, int? MaxQuantity, bool filterByLowStockLevel, bool filterByOutOfStockLevel,
                int? page, int? pageSizeID, string actionButton, string sortDirection = "asc", string sortField = "Event Name")
        {
            //For the Report View
            //var sumQ = from i in _context.EventLogs
            //           .Include(i => i.ItemReservation)
            //           orderby i.EventName, i.ItemName ascending
            //           select new EventReportVM
            //           {
            //               Id = i.Id,
            //               EventName = i.EventName,
            //               ItemName = i.ItemName,
            //               Quantity = i.Quantity,
            //               LocationID = i.ItemReservation.LocationID,
            //               Location = i.ItemReservation.Location.Name,
            //               ReservedDate = (DateTime)i.ItemReservation.ReservedDate,
            //               ReturnDate = (DateTime)i.ItemReservation.ReturnDate,
            //               //LogDate = i.LogDate
            //           };

            var sumQ = from i in _context.ItemReservations
                .Include(ir => ir.Item)
                .Include(ir => ir.Location)
                           //.Where(ir => ir.EventId == id)
                       orderby i.Event.Name, i.Item.Name ascending
                       select new EventReportVM
                       {
                           Id = i.Id,
                           EventName = i.Event.Name,
                           ItemID = i.Item.ID,
                           ItemName = i.Item.Name,
                           Quantity = i.Quantity,
                           LocationID = i.LocationID,
                           Location = i.Location.Name,
                           ReservedDate = (DateTime)i.Event.ReservedEventDate,
                           ReturnDate = (DateTime)i.Event.ReturnEventDate,
                           //LogDate = i.LogDate
                       };

            ViewDataReturnURL();

            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering

            //Populating the DropDownLists for the Search/Filtering criteria, which are the Location
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name");

            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "Event Name", "Item Name", "Quantity", "LogDate", "Location", "Reserved Date", "Return Date" };

            //Add as many filters as needed
            if (LocationID.Length > 0)
            {
                sumQ = sumQ.Where(p => LocationID.Contains(p.LocationID));
                ViewData["Filtering"] = "btn-danger";
            }

            if (!String.IsNullOrEmpty(SearchString1))
            {
                sumQ = sumQ.Where(p => p.EventName.ToUpper().Contains(SearchString1.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
            }
            if (!String.IsNullOrEmpty(SearchString2))
            {
                sumQ = sumQ.Where(p => p.ItemName.ToUpper().Contains(SearchString2.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
            }
            if (!String.IsNullOrEmpty(SearchString3))
            {
                sumQ = sumQ.Where(p => p.EventName.ToUpper().Contains(SearchString3.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
            }

            if (reservedFromDate != null || reservedToDate != null)
            {
                if (reservedFromDate != null && reservedToDate != null)
                {
                    // Filter records where DateReceived is within the specified range
                    sumQ = sumQ.Where(x => x.ReservedDate >= reservedFromDate && x.ReservedDate <= reservedToDate);
                    ViewData["Filtering"] = "btn-danger";
                }
                else
                {
                    // Filter records where DateReceived is greater than or equal to FromDate or less than or equal to ToDate
                    sumQ = sumQ.Where(x => x.ReservedDate >= reservedFromDate || x.ReservedDate <= reservedToDate);
                    //sumQ = sumQ.Where(x => x.LogDate >= FromDate || x.LogDate <= ToDate);
                    ViewData["Filtering"] = "btn-danger";
                }
                // Pass the FromDate and ToDate values back to the view
                //ViewBag.FromDate = FromDate;
                //ViewBag.ToDate = ToDate;
                ViewBag.ReservedFromDate = reservedFromDate;
                ViewBag.ReservedToDate = reservedToDate;
            }
            if (returnFromDate != null || returnToDate != null)
            {
                if (returnFromDate != null && returnToDate != null)
                {
                    // Filter records where DateReceived is within the specified range
                    sumQ = sumQ.Where(x => x.ReturnDate >= returnFromDate && x.ReturnDate <= returnToDate);
                    //sumQ = sumQ.Where(x => x.LogDate >= FromDate && x.LogDate <= ToDate);
                    ViewData["Filtering"] = "btn-danger";
                }
                else
                {
                    // Filter records where DateReceived is greater than or equal to FromDate or less than or equal to ToDate
                    sumQ = sumQ.Where(x => x.ReturnDate >= returnFromDate || x.ReturnDate <= returnToDate);
                    //sumQ = sumQ.Where(x => x.LogDate >= FromDate || x.LogDate <= ToDate);
                    ViewData["Filtering"] = "btn-danger";
                }
                // Pass the FromDate and ToDate values back to the view
                //ViewBag.FromDate = FromDate;
                //ViewBag.ToDate = ToDate;
                ViewBag.ReturnFromDate = returnFromDate;
                ViewBag.ReturnToDate = returnToDate;
            }

            // Filter by quantity range
            if (MinQuantity != null && MaxQuantity != null)
            {
                sumQ = sumQ.Where(x => x.Quantity >= MinQuantity && x.Quantity <= MaxQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinQuantity = MinQuantity;
                ViewBag.MaxQuantity = MaxQuantity;
            }
            else if (MinQuantity != null)
            {
                sumQ = sumQ.Where(x => x.Quantity >= MinQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinQuantity = MinQuantity;
            }
            else if (MaxQuantity != null)
            {
                sumQ = sumQ.Where(x => x.Quantity <= MaxQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MaxQuantity = MaxQuantity;
            }
            //filter locations based on stock levels if needed
            if (filterByLowStockLevel || filterByOutOfStockLevel)
            {
                sumQ = sumQ.Where(i => (filterByLowStockLevel && i.Quantity < 10) ||
                    (filterByOutOfStockLevel && i.Quantity == 0));

                ViewData["Filtering"] = "btn-danger";
                ViewBag.FilterByLowStockLevel = new { IsChecked = filterByLowStockLevel };
                ViewBag.FilterByOutOfStockLevel = new { IsChecked = filterByOutOfStockLevel };
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
            if (sortField == "Item Name")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.ItemName);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.ItemName);
                }
            }
            else if (sortField == "Quantity")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Quantity);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Quantity);
                }
            }
            else if (sortField == "Location")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Location);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Location);
                }
            }
            else if (sortField == "Reserved Date")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.ReservedDate);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.ReservedDate);
                }
            }
            else if (sortField == "Return Date")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.ReturnDate);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.ReturnDate);
                }
            }
            //else if (sortField == "LogDate")
            //{
            //    if (sortDirection == "asc")
            //    {
            //        sumQ = sumQ
            //            .OrderBy(p => p.LogDate);
            //    }
            //    else
            //    {
            //        sumQ = sumQ
            //            .OrderByDescending(p => p.LogDate);
            //    }
            //}
            else //Sorting by Item Name
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.EventName);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.EventName);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            //int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "InventoryEventsReport");//Remember for this View
            //ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            //var pagedData = await PaginatedList<EventReportVM>.CreateAsync(sumQ.AsNoTracking(), page ?? 1, pageSize);

            //return View(pagedData);
            return View(sumQ);
        }


        //Method for Excel Inventory Events Report
        public ActionResult DownloadInventoryEvents(string SearchString1, string SearchString2, string SearchString3, int?[] LocationID, DateTime? returnToDate, DateTime? reservedToDate,
             DateTime? returnFromDate, DateTime? reservedFromDate, int? MinQuantity, int? MaxQuantity, bool filterByLowStockLevel, bool filterByOutOfStockLevel,
                int? page, int? pageSizeID, string actionButton, string sortDirection = "asc", string sortField = "Event Name")
        {
            //For the Report View
            //var sumQ = from i in _context.EventLogs
            //           .Include(i => i.ItemReservation)
            //           orderby i.EventName, i.ItemName ascending
            //           select new EventReportVM
            //           {
            //               Id = i.Id,
            //               EventName = i.EventName,
            //               ItemName = i.ItemName,
            //               Quantity = i.Quantity,
            //               LocationID = i.ItemReservation.LocationID,
            //               Location = i.ItemReservation.Location.Name,
            //               ReservedDate = (DateTime)i.ItemReservation.ReservedDate,
            //               ReturnDate = (DateTime)i.ItemReservation.ReturnDate,
            //               //LogDate = i.LogDate
            //           };

            var sumQ = from i in _context.ItemReservations
                .Include(ir => ir.Item)
                .Include(ir => ir.Location)
                           //.Where(ir => ir.EventId == id)
                       orderby i.Event.Name, i.Item.Name ascending
                       select new EventReportVM
                       {
                           Id = i.Id,
                           EventName = i.Event.Name,
                           ItemID = i.Item.ID,
                           ItemName = i.Item.Name,
                           Quantity = i.Quantity,
                           LocationID = i.LocationID,
                           Location = i.Location.Name,
                           ReservedDate = (DateTime)i.Event.ReservedEventDate,
                           ReturnDate = (DateTime)i.Event.ReturnEventDate,
                           //LogDate = i.LogDate
                       };

            ViewDataReturnURL();

            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering

            //Populating the DropDownLists for the Search/Filtering criteria, which are the Location
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name");

            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "Event Name", "Item Name", "Quantity", "LogDate", "Location", "Reserved Date", "Return Date" };

            //Add as many filters as needed
            if (LocationID.Length > 0)
            {
                sumQ = sumQ.Where(p => LocationID.Contains(p.LocationID));
                ViewData["Filtering"] = "btn-danger";
            }

            if (!String.IsNullOrEmpty(SearchString1))
            {
                sumQ = sumQ.Where(p => p.EventName.ToUpper().Contains(SearchString1.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
            }
            if (!String.IsNullOrEmpty(SearchString2))
            {
                sumQ = sumQ.Where(p => p.ItemName.ToUpper().Contains(SearchString2.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
            }
            if (!String.IsNullOrEmpty(SearchString3))
            {
                sumQ = sumQ.Where(p => p.EventName.ToUpper().Contains(SearchString3.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
            }

            if (reservedFromDate != null || reservedToDate != null)
            {
                if (reservedFromDate != null && reservedToDate != null)
                {
                    // Filter records where DateReceived is within the specified range
                    sumQ = sumQ.Where(x => x.ReservedDate >= reservedFromDate && x.ReservedDate <= reservedToDate);
                    ViewData["Filtering"] = "btn-danger";
                }
                else
                {
                    // Filter records where DateReceived is greater than or equal to FromDate or less than or equal to ToDate
                    sumQ = sumQ.Where(x => x.ReservedDate >= reservedFromDate || x.ReservedDate <= reservedToDate);
                    //sumQ = sumQ.Where(x => x.LogDate >= FromDate || x.LogDate <= ToDate);
                    ViewData["Filtering"] = "btn-danger";
                }
                // Pass the FromDate and ToDate values back to the view
                //ViewBag.FromDate = FromDate;
                //ViewBag.ToDate = ToDate;
                ViewBag.ReservedFromDate = reservedFromDate;
                ViewBag.ReservedToDate = reservedToDate;
            }
            if (returnFromDate != null || returnToDate != null)
            {
                if (returnFromDate != null && returnToDate != null)
                {
                    // Filter records where DateReceived is within the specified range
                    sumQ = sumQ.Where(x => x.ReturnDate >= returnFromDate && x.ReturnDate <= returnToDate);
                    //sumQ = sumQ.Where(x => x.LogDate >= FromDate && x.LogDate <= ToDate);
                    ViewData["Filtering"] = "btn-danger";
                }
                else
                {
                    // Filter records where DateReceived is greater than or equal to FromDate or less than or equal to ToDate
                    sumQ = sumQ.Where(x => x.ReturnDate >= returnFromDate || x.ReturnDate <= returnToDate);
                    //sumQ = sumQ.Where(x => x.LogDate >= FromDate || x.LogDate <= ToDate);
                    ViewData["Filtering"] = "btn-danger";
                }
                // Pass the FromDate and ToDate values back to the view
                //ViewBag.FromDate = FromDate;
                //ViewBag.ToDate = ToDate;
                ViewBag.ReturnFromDate = returnFromDate;
                ViewBag.ReturnToDate = returnToDate;
            }

            // Filter by quantity range
            if (MinQuantity != null && MaxQuantity != null)
            {
                sumQ = sumQ.Where(x => x.Quantity >= MinQuantity && x.Quantity <= MaxQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinQuantity = MinQuantity;
                ViewBag.MaxQuantity = MaxQuantity;
            }
            else if (MinQuantity != null)
            {
                sumQ = sumQ.Where(x => x.Quantity >= MinQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinQuantity = MinQuantity;
            }
            else if (MaxQuantity != null)
            {
                sumQ = sumQ.Where(x => x.Quantity <= MaxQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MaxQuantity = MaxQuantity;
            }
            //filter locations based on stock levels if needed
            if (filterByLowStockLevel || filterByOutOfStockLevel)
            {
                sumQ = sumQ.Where(i => (filterByLowStockLevel && i.Quantity < 10) ||
                    (filterByOutOfStockLevel && i.Quantity == 0));

                ViewData["Filtering"] = "btn-danger";
                ViewBag.FilterByLowStockLevel = new { IsChecked = filterByLowStockLevel };
                ViewBag.FilterByOutOfStockLevel = new { IsChecked = filterByOutOfStockLevel };
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
            if (sortField == "Item Name")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.ItemName);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.ItemName);
                }
            }
            else if (sortField == "Quantity")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Quantity);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Quantity);
                }
            }
            else if (sortField == "Location")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Location);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Location);
                }
            }
            else if (sortField == "Reserved Date")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.ReservedDate);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.ReservedDate);
                }
            }
            else if (sortField == "Return Date")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.ReturnDate);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.ReturnDate);
                }
            }
            //else if (sortField == "LogDate")
            //{
            //    if (sortDirection == "asc")
            //    {
            //        sumQ = sumQ
            //            .OrderBy(p => p.LogDate);
            //    }
            //    else
            //    {
            //        sumQ = sumQ
            //            .OrderByDescending(p => p.LogDate);
            //    }
            //}
            else //Sorting by Item Name
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.EventName);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.EventName);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            //Get the inventory
            var items = from i in sumQ
                            //orderby i.EventName, i.ItemName ascending
                        select new
                        {
                            EventName = i.EventName,
                            ReservedDate = (DateTime)i.ReservedDate,
                            ReturnDate = (DateTime)i.ReturnDate,
                            ItemName = i.ItemName,
                            Quantity = i.Quantity,
                            Location = i.Location
                        };

            //How many rows?
            int numRows = items.Count();

            if (numRows > 0) //We have data
            {
                //Create a new spreadsheet from scratch.
                using (ExcelPackage excel = new ExcelPackage())
                {
                    var workSheet = excel.Workbook.Worksheets.Add("Event Items");

                    //Note: Cells[row, column]
                    workSheet.Cells[3, 1].LoadFromCollection(items, true);

                    //Style fee column for quantity
                    workSheet.Column(6).Style.Numberformat.Format = "###,###,##0";

                    //Style 4th column for dates (ReservedDate)
                    workSheet.Column(2).Style.Numberformat.Format = "yyyy-mm-dd";
                    //Style 5th column for dates (ReturnDate)
                    workSheet.Column(3).Style.Numberformat.Format = "yyyy-mm-dd";

                    ////Style fee column for currency
                    //workSheet.Column(3).Style.Numberformat.Format = "$###,###,##0.00";

                    //Note: You can define a BLOCK of cells: Cells[startRow, startColumn, endRow, endColumn]
                    //Make Event Name Bold
                    workSheet.Cells[4, 1, numRows + 3, 1].Style.Font.Bold = true;

                    //Make Item Quantity Bold/Colour coded
                    workSheet.Cells[4, 5, numRows + 3, 5].Style.Font.Bold = true;
                    //var item = from i in sumQ
                    //           orderby i.EventName, i.ItemName ascending
                    //           select i.Quantity;
                    //int row = 4;
                    //foreach (var qty in item)
                    //{
                    //    if (row <= (numRows + 3))
                    //    {
                    //        if (qty == 0)
                    //        {
                    //            workSheet.Cells[row, 3].Style.Font.Color.SetColor(Color.Red);
                    //            row++;
                    //        }
                    //        else if ((qty <= 10) && (qty > 0))
                    //        {
                    //            workSheet.Cells[row, 3].Style.Font.Color.SetColor(Color.Orange);
                    //            row++;
                    //        }
                    //        else if (qty > 10)
                    //        {
                    //            workSheet.Cells[row, 3].Style.Font.Color.SetColor(Color.Green);
                    //            row++;
                    //        }
                    //    }
                    //    else
                    //    {
                    //        break;
                    //    }
                    //}

                    //Note: these are fine if you are only 'doing' one thing to the range of cells.
                    //Otherwise you should USE a range object for efficiency
                    //Total Cost for all Items in Inventory
                    //workSheet.Cells[4, 4, numRows + 3, 5].Calculate();
                    using (ExcelRange totalQty = workSheet.Cells[numRows + 4, 5])//
                    {
                        //Total Cost Text
                        var rangeName = workSheet.Cells[numRows + 4, 3, numRows + 4, 4];
                        rangeName.Value = "Total Quantity:";
                        rangeName.Style.Font.Bold = true;
                        rangeName.Merge = true;
                        rangeName.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        //Total Cost Sum - get cost * qty for each row
                        totalQty.Formula = "Sum(" + (workSheet.Cells[4, 5].Address) + ":" + workSheet.Cells[numRows + 3, 5].Address + ")";
                        totalQty.Style.Font.Bold = true;
                        totalQty.Style.Numberformat.Format = "###,###,##0";
                        var range = workSheet.Cells[numRows + 4, 5, numRows + 4, 6];
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    //Set Style and backgound colour of headings
                    using (ExcelRange headings = workSheet.Cells[3, 1, 3, 6])
                    {
                        headings.Style.Font.Bold = true;
                        var fill = headings.Style.Fill;
                        fill.PatternType = ExcelFillStyle.Solid;
                        fill.BackgroundColor.SetColor(Color.LightBlue);
                    }

                    //Autofit columns
                    workSheet.Cells.AutoFitColumns();
                    //Note: You can manually set width of columns as well
                    //workSheet.Column(7).Width = 10;

                    //Add a title and timestamp at the top of the report
                    workSheet.Cells[1, 1].Value = "Inventory Events Report";
                    using (ExcelRange Rng = workSheet.Cells[1, 1, 1, 6])
                    {
                        Rng.Merge = true; //Merge columns start and end range
                        Rng.Style.Font.Bold = true; //Font should be bold
                        Rng.Style.Font.Size = 18;
                        Rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                    //Since the time zone where the server is running can be different, adjust to 
                    //Local for us.
                    DateTime utcDate = DateTime.UtcNow;
                    TimeZoneInfo esTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                    DateTime localDate = TimeZoneInfo.ConvertTimeFromUtc(utcDate, esTimeZone);
                    using (ExcelRange Rng = workSheet.Cells[2, 6])
                    {
                        Rng.Value = "Created: " + localDate.ToShortTimeString() + " on " +
                            localDate.ToShortDateString();
                        Rng.Style.Font.Bold = true; //Font should be bold
                        Rng.Style.Font.Size = 12;
                        Rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    }

                    //Ok, time to download the Excel
                    try
                    {
                        Byte[] theData = excel.GetAsByteArray();
                        string filename = "InventoryEventsReport.xlsx";
                        string mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        return File(theData, mimeType, filename);
                    }
                    catch (Exception)
                    {
                        return BadRequest("Could not build and download the file.");
                    }
                }
            }
            return NotFound("No data.");
        }

        public async Task<IActionResult> SilencingToastrNottifPopup(int id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Inventories == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventories.FindAsync(id);
            if (inventory == null)
            {
                return NotFound();
            }

            ViewData["ItemID"] = new SelectList(_context.Items, "ID", "Name", inventory.ItemID);
            var options = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "1 day" },
                new SelectListItem { Value = "2", Text = "2 days" },
                new SelectListItem { Value = "3", Text = "3 days" },
                new SelectListItem { Value = "7", Text = "1 week" },
                new SelectListItem { Value = "0", Text = "Permanently" }
            };

            ViewData["SilentID"] = new SelectList(options, "Value", "Text");

            return View(inventory);
        }


        [HttpPost]
        public async Task<IActionResult> SilencingToastrNottifPopup(int id, Byte[] RowVersion, string SilentID)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            //Go get the Event to update
            var inventoryToUpdate = await _context.Inventories.FirstOrDefaultAsync(i => i.Id == id);

            if (inventoryToUpdate == null)
            {
                return NotFound();
            }

            //Put the original RowVersion value in the OriginalValues collection for the entity
            _context.Entry(inventoryToUpdate).Property("RowVersion").OriginalValue = RowVersion;

            try
            {
                int days = 0;
                // Use the selected value in your code
                if (!string.IsNullOrEmpty(SilentID))
                {
                    if (SilentID == "1") { days = 1; }
                    else if (SilentID == "2") { days = 2; }
                    else if (SilentID == "3") { days = 3; }
                    else if (SilentID == "7") { days = 7; }
                    else if (SilentID == "0")
                    {
                        days = 0;

                    }

                }
                if (days <= 0)
                { inventoryToUpdate.DismissNotification = null; }
                else
                {
                    inventoryToUpdate.DismissNotification = null;
                    inventoryToUpdate.DismissNotification = DateTime.Now.AddDays(days);
                }
                _context.Update(inventoryToUpdate);
                _context.SaveChanges();

                _context.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
            ViewData["ItemID"] = new SelectList(_context.Items, "ID", "Name", inventoryToUpdate.ItemID);
            var options = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "1 day" },
                new SelectListItem { Value = "2", Text = "2 days" },
                new SelectListItem { Value = "3", Text = "3 days" },
                new SelectListItem { Value = "7", Text = "1 week" },
                new SelectListItem { Value = "0", Text = "Permanently" }
            };

            ViewData["SilentID"] = new SelectList(options, "Value", "Text");

            TempData["SilenceNotifMessageBool"] = "true";
            return RedirectToAction("Index", "Inventories");
        }

        public async Task<IActionResult> RecoveringToastrNottifPopup(int id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Inventories == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventories.FindAsync(id);
            if (inventory == null)
            {
                return NotFound();
            }


            return View(inventory);
        }


        [HttpPost]
        public async Task<IActionResult> RecoveringToastrNottifPopup(int id, Byte[] RowVersion)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            //Go get the Event to update
            var inventoryToUpdate = await _context.Inventories.FirstOrDefaultAsync(i => i.Id == id);

            if (inventoryToUpdate == null)
            {
                return NotFound();
            }


            try
            {
                inventoryToUpdate.DismissNotification = DateTime.Now;

                _context.Update(inventoryToUpdate);
                _context.SaveChanges();
            }
            catch (Exception)
            {

                throw;
            }
            TempData["RecoverNotifMessageBool"] = "true";
            return RedirectToAction("Index", "Items");
        }

        ////for full audit
        //public async Task<PartialViewResult> ItemAuditHistory(int id)
        //{
        //    const string primaryEntity = "Inventory";
        //    //Get audit data 
        //    string pkFilter = "\"ID\":" + id.ToString();
        //    string fKFilter = "\"" + primaryEntity + "ID\":" + id.ToString();
        //    var audits = await _context.AuditLogs
        //        .Where(a => (a.EntityName == primaryEntity && a.PrimaryKey.Contains(pkFilter))
        //                || a.PrimaryKey.Contains(fKFilter)
        //                || a.ForeignKeys.Contains(fKFilter)
        //                || a.OldValues.Contains(fKFilter)
        //                || a.NewValues.Contains(fKFilter))
        //        .ToListAsync();

        //    List<AuditRecordVM> auditRecords = new List<AuditRecordVM>();
        //    if (audits.Count > 0)
        //    {
        //        foreach (var a in audits)
        //        {
        //            AuditRecordVM ar = a.ToAuditRecord();

        //            //Get the collection of keys
        //            Dictionary<string, string> primaryKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(a.PrimaryKey + String.Empty);
        //            //Get the collection of foreign keys
        //            Dictionary<string, string> foreignKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(a.ForeignKeys + String.Empty);

        //            if (ar.Type == "Updated")
        //            {
        //                //Here is where we will handle changes to any "loaded" entities related to
        //                //the primary entity that have properties to track.  You will need a "if" for each one.

        //                if (ar.Entity == primaryEntity)//Audit changes to the actual primary entity
        //                {
        //                    ar.Type += " " + primaryEntity;
        //                    //Update to the primary entity so lookup all the foreign keys 
        //                    //Go and get each "lookup" Value.  Only one in this case.
        //                    foreach (var value in ar.AuditValues)
        //                    {
        //                        if (value.PropertyName == "ItemID")
        //                        {
        //                            Item item = await _context.Items.FindAsync(int.Parse(value.OldValue));
        //                            value.OldValue = (item != null) ? item.Name : "Deleted Item";
        //                            item = await _context.Items.FindAsync(int.Parse(value.NewValue));
        //                            value.NewValue = (item != null) ? item.Name : "Deleted Item";
        //                            value.PropertyName = "Item";
        //                        }
        //                        else if (value.PropertyName == "LocationID")
        //                        {
        //                            Location locations = await _context.Locations.FindAsync(int.Parse(value.OldValue));
        //                            value.OldValue = (locations != null) ? locations.Name : "Deleted Location";
        //                            locations = await _context.Locations.FindAsync(int.Parse(value.NewValue));
        //                            value.NewValue = (locations != null) ? locations.Name : "Deleted Location";
        //                            value.PropertyName = "Location";
        //                        }
        //                    }
        //                }
        //            }
        //            else if (ar.Type == "Added" || ar.Type == "Removed")
        //            {
        //                //In this section we will handle when entities are added or 
        //                //removed in relation to the primary entity.

        //                //Get the values from either Old or New
        //                //Note: adding String.Empty prevents null
        //                string values = ar.Type == "Added" ? a.NewValues + String.Empty : a.OldValues + String.Empty;
        //                //Get the collection of values of the association entity
        //                Dictionary<string, string> allValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(values + String.Empty);

        //                //Modify the Type of audit and include some details about the related object
        //                string newComment = "";
        //                //Check to see if it is an uploaded document and show the name
        //                if (ar.Entity == "Inventory")
        //                {
        //                    if (int.TryParse(allValues["LocationID"]?.ToString(), out int locationID))
        //                    {
        //                        Location l = await _context.Locations.FindAsync(locationID);
        //                        newComment += (l != null) ? " Inventory (Location: " + l.Name : " Item (Deleted Location";
        //                    }
        //                    if (int.TryParse(allValues["ItemID"]?.ToString(), out int itemID))
        //                    {
        //                        Item i = await _context.Items.FindAsync(itemID);
        //                        string itemName = i.Name;
        //                        newComment += (i != null) ? ", Item: " + itemName + ")" : ", Deleted Item)";
        //                    }                       
        //                }
        //                ar.Type += " " + newComment;
        //            }
        //            auditRecords.Add(ar);
        //        }
        //    }
        //    return PartialView("_AuditHistory", auditRecords.OrderByDescending(a => a.DateTime));
        //}

        //Method for Viewing Inventory Report
        public async Task<IActionResult> RecoverAllSilencedNotif()
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();
            int records = 0;
            var inventories = _context.Inventories.Include(i => i.Location).Include(i => i.Item).ThenInclude(i => i.Category)
                .Include(i => i.Item.ItemLocations).ThenInclude(i => i.Location)
                .Where(t => t.Item.Archived == false).ToList();
            try
            {

                foreach (var inventory in inventories)
                {
                    if (inventory.Quantity <= inventory.Item.Category.LowCategoryThreshold)
                    {
                        if (inventory.DismissNotification >= DateTime.Now || inventory.DismissNotification == null)
                        {
                            records += 1;
                        }
                    }
                    inventory.DismissNotification = DateTime.Now;
                    _context.Update(inventory);
                    _context.SaveChanges();
                }
                if (records == 0)
                {
                    _toastNotification.AddSuccessToastMessage(
                                $@"No Messages To Recover");
                }
                else if (records > 0)
                {
                    _toastNotification.AddSuccessToastMessage(
                                $@"{records} Record(s) Recovered");

                }


            }
            catch (Exception)
            {

                throw;
            }
            return RedirectToAction("Index", "Items");
        }

        //Method for Viewing Silenced Messages
        public async Task<IActionResult> ViewSilencedNotif()
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();
            int records = 0;
            var inventories = _context.Inventories.Include(i => i.Location).Include(i => i.Item).ThenInclude(i => i.Category)
                .Include(i => i.Item.ItemLocations).ThenInclude(i => i.Location)
                .Where(t => t.Item.Archived == false).ToList();
            try
            {
                foreach (var inventory in inventories)
                {
                    if (inventory.Quantity <= inventory.Item.Category.LowCategoryThreshold)
                    {
                        if (inventory.DismissNotification >= DateTime.Now || inventory.DismissNotification == null)
                        {

                            DateTime inventoryDismissNotification = inventory.DismissNotification ?? DateTime.MinValue; // Use DateTime.MinValue as a default value if inventory.DismissNotification is null
                            TimeSpan timeDifference = inventoryDismissNotification - DateTime.Now;
                            int daysApart = timeDifference.Days;
                            if (timeDifference.Days == 0)
                            {
                                daysApart = 1;
                            }
                            else if (timeDifference.Days < 0)
                            {
                                daysApart = 0;
                            }


                            if (timeDifference.Days >= 0)
                            {
                                records++;
                                _toastNotification.AddWarningToastMessage(
                                $@"Inventory for {inventory.Item.Name} at location {inventory.Location.Name} is running low. Current quantity: {inventory.Quantity}
                                    <a href='#' onclick='redirectToEdit({inventory.Item.ID}); return false;'>Edit</a> <br>***Silenced {daysApart} day(s) left***
                                    
                                    <button class='btn btn-outline-secondary' id='nowEditActivateNotif' data-bs-toggle='modal' data-bs-target='#addNotifActivateModal' type='button'
                                    onclick='setItemIdForPartialNotifActivate({inventory.Item.ID})'>
                                        <strong>Recover Message</strong>
                                    </button> 

                                    ");
                            }
                            else
                            {
                                records++;
                                _toastNotification.AddWarningToastMessage(
                                $@"Inventory for {inventory.Item.Name} at location {inventory.Location.Name} is running low. Current quantity: {inventory.Quantity}
                                    <a href='#' onclick='redirectToEdit({inventory.Item.ID}); return false;'>Edit</a> <br>***Silenced Permanantly***
                                    
                               
                                    <button class='btn btn-outline-secondary' id='nowEditActivateNotifNull' data-bs-toggle='modal' data-bs-target='#addNotifActivateModal' type='button'
                                    onclick='setItemIdForPartialNotifActivate({inventory.Item.ID})'>
                                        <strong>Recover Message</strong>
                                    </button> 

                                    ");
                            }


                        }

                    }
                }

            }
            catch (Exception)
            {

                throw;
            }
            if (records == 0)
            {
                _toastNotification.AddSuccessToastMessage(
            $@"No Silent Messages At This Time");
            }
            return RedirectToAction("Index", "Items");
        }

        //Method for Viewing Silenced Messages
        public async Task<IActionResult> ViewActiveNotif()
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();
            int itemID = 0;
            int norecords = 0;
            var inventories = _context.Inventories.Include(i => i.Location).Include(i => i.Item).ThenInclude(i => i.Category)
                .Include(i => i.Item.ItemLocations).ThenInclude(i => i.Location)
                .Where(t => t.Item.Archived == false).ToList();
            foreach (var inventory in inventories)
            {

                if (inventory.Quantity <= inventory.Item.Category.LowCategoryThreshold)
                {
                    if (inventory.DismissNotification <= DateTime.Now && inventory.DismissNotification != null)
                    {
                        itemID = inventory.ItemID;
                        HttpContext.Session.SetString("ItemIdForPartialNotif" + itemID, inventory.ItemID.ToString());
                        norecords += 1;
                        inventory.IsLowInventory = true;
                        _toastNotification.AddWarningToastMessage(
                            $@"Inventory for {inventory.Item.Name} at location {inventory.Location.Name} is running low. Current quantity: {inventory.Quantity}
                                <a href='#' onclick='redirectToEdit({inventory.Item.ID}); return false;'>Edit</a>
                                <br><br>Qiuck Actions:
                                
                                <button class='btn btn-outline-secondary' id='nowEditNotifSilent' data-bs-toggle='modal' data-bs-target='#addNotifModal' type='button'
                                    onclick='setItemIdForPartialNotif({inventory.Item.ID})'>
                                    <strong>Silence Message</strong>
                                </button>");

                    }

                }
                else
                {

                }
            }

            if (norecords == 0)
            {
                _toastNotification.AddSuccessToastMessage(
                            $@"All Caught Up!");
            }
            return RedirectToAction("Index", "Items");
        }
        private string ControllerName()
        {
            return this.ControllerContext.RouteData.Values["controller"].ToString();
        }
        private void ViewDataReturnURL()
        {
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, ControllerName());
        }
        private bool InventoryExists(int id)
        {
            return _context.Inventories.Any(e => e.Id == id);
        }

    }
}
