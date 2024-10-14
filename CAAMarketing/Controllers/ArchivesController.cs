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
using Microsoft.AspNetCore.Authorization;

namespace CAAMarketing.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ArchivesController : Controller
    {
        private readonly CAAContext _context;

        private readonly IToastNotification _toastNotification;

        public ArchivesController(CAAContext context, IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;

        }

        // GET: Archives
        public async Task<IActionResult> Index(string SearchString, int? LocationID, bool? LowQty,
           int? page, int? pageSizeID, string actionButton, string sortDirection = "asc", string sortField = "Item")
        {


            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering
            //Then in each "test" for filtering, add ViewData["Filtering"] = " show" if true;
            var inventories = _context.Items
                .Include(i => i.Inventories).ThenInclude(i => i.Location)
                .Include(i => i.ItemThumbNail)
                .Include(i => i.Employee)
                
                .Include(i => i.ItemLocations).ThenInclude(i => i.Location)
            .AsNoTracking();


            inventories = inventories.Where(p => p.Archived == true);
            

            //Populating the DropDownLists for the Search/Filtering criteria, which are the Location
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name");


            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "Item", "UPC", "Location", "Employee", "Quantity", "Cost" };

            //Add as many filters as needed
            //if (LocationID.HasValue)
            //{
            //    inventories = inventories.Where(p => p.LocationID == LocationID);
            //    ViewData["Filtering"] = "btn-danger";
            //}
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

                inventories = inventories.Where(p => p.Name.ToUpper().Contains(SearchString.ToUpper())
                                       || (isNumeric && p.UPC == searchUPC));
                ViewData["Filtering"] = "btn-danger";
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
            if (sortField == "Cost")
            {
                if (sortDirection == "asc")
                {
                    inventories = inventories
                        .OrderBy(p => p.Cost.ToString());
                }
                else
                {
                    inventories = inventories
                        .OrderByDescending(p => p.Cost.ToString());
                }
            }
            else if (sortField == "Employee")
            {
                if (sortDirection == "asc")
                {
                    inventories = inventories
                        .OrderByDescending(p => p.Employee.LastName).ThenByDescending(p => p.Employee.FirstName);
                }
                else
                {
                    inventories = inventories
                        .OrderBy(p => p.Employee.LastName).ThenBy(p => p.Employee.FirstName);
                }
            }
            else if (sortField == "UPC")
            {
                if (sortDirection == "asc")
                {
                    inventories = inventories
                        .OrderBy(p => p.UPC);
                }
                else
                {
                    inventories = inventories
                        .OrderByDescending(p => p.UPC);
                }
            }
            else if (sortField == "Quantity")
            {
                if (sortDirection == "asc")
                {
                    inventories = inventories
                        .OrderByDescending(p => p.Quantity);
                }
                else
                {
                    inventories = inventories
                        .OrderBy(p => p.Quantity);
                }
            }
            //else if (sortField == "Location")
            //{
            //    if (sortDirection == "asc")
            //    {
            //        inventories = inventories
            //            .OrderBy(p => p.Loca.Name);
            //    }
            //    else
            //    {
            //        inventories = inventories
            //            .OrderByDescending(p => p.Location.Name);
            //    }
            //}
            else //Sorting by Patient Name
            {
                if (sortDirection == "asc")
                {
                    inventories = inventories
                        .OrderBy(p => p.Name);
                }
                else
                {
                    inventories = inventories
                        .OrderByDescending(p => p.Name);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;


            //Handle Paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "Inventories");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);

            var pagedData = await PaginatedList<Item>.CreateAsync(inventories.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
        }


        // GET: InventoryTransfers
        public async Task<IActionResult> TransferArchivesIndex(string SearchString, int? FromLocationID, int? ToLocationId, int? ItemID,
           int? page, int? pageSizeID, string actionButton, string sortDirection = "asc", string sortField = "ItemTransfered")
        {
            ViewDataReturnURL();



            if (HttpContext.Session.GetString("TransferRecordRecovered") == "True")
            {
                _toastNotification.AddSuccessToastMessage("Transfer Record Recovered <br/> <a href='/InventoryTransfers/'>View Active Transfers.</a>");
            }
            HttpContext.Session.SetString("TransferRecordRecovered", "False");



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

            
            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);




            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering
            //Then in each "test" for filtering, add ViewData["Filtering"] = " show" if true;


            //Populating the DropDownLists for the Search/Filtering criteria, which are the Category and Supplier DDL
            ViewData["FromLocationId"] = new SelectList(_context.Locations, "Id", "Name");
            ViewData["ToLocationId"] = new SelectList(_context.Locations, "Id", "Name");

            var transfers = _context.InventoryTransfers
                .Include(i => i.FromLocation)
                .Include(i => i.Item)
                .Include(i => i.ToLocation)
                .AsNoTracking();

            var transfersICollection = _context.Transfers
                .Include(e => e.InventoryTransfers).ThenInclude(i => i.FromLocation)
                .Include(e => e.InventoryTransfers).ThenInclude(i => i.ToLocation)
                .Include(e => e.InventoryTransfers).ThenInclude(i => i.Item)
                .Where(i => i.Archived == true)
                .AsNoTracking();

            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "ItemTransfered", "FromLocation", "ToLocation", "Quantity", "TransferDate" };


            //Add as many filters as needed
            if (FromLocationID.HasValue)
            {
                transfers = transfers.Where(p => p.FromLocationId == FromLocationID);
                ViewData["Filtering"] = "btn-danger";
            }
            if (ToLocationId.HasValue)
            {
                transfers = transfers.Where(p => p.ToLocationId == ToLocationId);
                ViewData["Filtering"] = "btn-danger";
            }
            if (!String.IsNullOrEmpty(SearchString))
            {
                long searchUPC;
                bool isNumeric = long.TryParse(SearchString, out searchUPC);

                transfers = transfers.Where(p => p.Item.Name.ToUpper().Contains(SearchString.ToUpper())
                                                 || (isNumeric && p.Item.UPC == searchUPC));
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
            if (sortField == "TransferDate")
            {
                if (sortDirection == "asc")
                {
                    transfers = transfers
                        .OrderBy(p => p.TransferDate);
                }
                else
                {
                    transfers = transfers
                        .OrderByDescending(p => p.TransferDate);
                }
            }
            else if (sortField == "Quantity")
            {
                if (sortDirection == "asc")
                {
                    transfers = transfers
                        .OrderByDescending(p => p.Quantity);
                }
                else
                {
                    transfers = transfers
                        .OrderBy(p => p.Quantity);
                }
            }
            else if (sortField == "ToLocation")
            {
                if (sortDirection == "asc")
                {
                    transfers = transfers
                        .OrderBy(p => p.ToLocation.Name);
                }
                else
                {
                    transfers = transfers
                        .OrderByDescending(p => p.ToLocation.Name);
                }
            }
            else if (sortField == "FromLocation")
            {
                if (sortDirection == "asc")
                {
                    transfers = transfers
                        .OrderBy(p => p.FromLocation.Name);
                }
                else
                {
                    transfers = transfers
                        .OrderByDescending(p => p.FromLocation.Name);
                }
            }
            else //Sorting by Patient Name
            {
                if (sortDirection == "asc")
                {
                    transfers = transfers
                        .OrderBy(p => p.Item.Name);
                }
                else
                {
                    transfers = transfers
                        .OrderByDescending(p => p.Item.Name);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;


            //List<Transfer> Transfers = _context.Transfers
            //   .Include(e => e.InventoryTransfers).ThenInclude(i => i.FromLocation)
            //   .Include(e => e.InventoryTransfers).ThenInclude(i => i.ToLocation)
            //   .Include(e => e.InventoryTransfers).ThenInclude(e => e.Item)
            //   .Include(e => e.InventoryTransfers).ThenInclude(e => e.Item).ThenInclude(e => e.Inventories)
            //   .AsNoTracking()
            //   .ToList();

            List<InventoryTransfer> InvTransfers = _context.InventoryTransfers
               .Include(e => e.Transfer)
               .Include(e => e.Item)
               .Include(e => e.ToLocation)
               .Include(e => e.FromLocation)
               .Include(i => i.Item).ThenInclude(i => i.ItemImages)
               .Include(i => i.Item).ThenInclude(i => i.ItemThumbNail)
               .AsNoTracking()
               .ToList();

            Inventory inventory = _context.Inventories
                 .Where(p => p.ItemID == ItemID.GetValueOrDefault())
                 .FirstOrDefault();



            ViewBag.InvTransfers = InvTransfers;
            ViewBag.Inventory = inventory;



            //Handle Paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "InventoryTransfers");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);

            var pagedData = await PaginatedList<Transfer>.CreateAsync(transfersICollection.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
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
        public async Task<IActionResult> Create([Bind("Id,Cost,Quantity,ItemID,LocationID,IsLowInventory,LowInventoryThreshold")] Inventory inventory)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (ModelState.IsValid)
            {
                var existingInventory = _context.Inventories.FirstOrDefault(i => i.ItemID == inventory.ItemID);
                if (existingInventory != null)
                {
                    existingInventory.Quantity += inventory.Quantity;
                    _context.Update(existingInventory);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    _context.Add(inventory);
                    await _context.SaveChangesAsync();
                }
               
                //return RedirectToAction(nameof(Index));
                return RedirectToAction("Details", new { inventory.ItemID });

            }
            ViewData["ItemID"] = new SelectList(_context.Items, "ID", "Name", inventory.ItemID);
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name", inventory.LocationID);
            return View(inventory);
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
                i => i.Cost, i => i.Quantity, i => i.ItemID, i => i.LocationID, i => i.IsLowInventory, i => i.LowInventoryThreshold))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    //return RedirectToAction(nameof(Index));
                    return RedirectToAction("Details", new { inventoryToUpdate.ItemID });

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

            var inventory = await _context.Items
                .Include(i => i.Inventories).ThenInclude(i => i.Location)
                .Include(i => i.ItemThumbNail)
                .Include(i => i.ItemImages)
                .Include(i => i.Employee)
                .Include(i=>i.Supplier)
                .Include(i=>i.Category)
                .Include(i => i.ItemLocations).ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(m => m.ID == id);

            //var inventory = await _context.Inventories
            //    .Include(i => i.Item)
            //    .Include(i => i.Location)
            //    .Include(i => i.Item.Employee)
            //    .FirstOrDefaultAsync(m => m.Id == id);


            if (inventory == null)
            {
                return NotFound();
            }

            return View(inventory);
        }

        // POST: Inventories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (_context.Items == null)
            {
                return Problem("Entity set 'CAAContext.Inventories'  is null.");
            }
            var item = await _context.Items.FindAsync(id);
            if (item != null)
            {
                //_context.Inventories.Remove(inventory);
                item.Archived = false;
            }

            await _context.SaveChangesAsync();
            // return RedirectToAction(nameof(Index));
            _toastNotification.AddSuccessToastMessage("Item record recovered. <br/> <a href='/Items/'>View Active Items.</a>");

            return Redirect(ViewData["returnURL"].ToString());

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
