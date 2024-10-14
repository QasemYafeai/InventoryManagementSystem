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
using Microsoft.AspNetCore.Authorization;
using NToastNotify;
using Microsoft.CodeAnalysis;
using System.Security.Cryptography.Xml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using static iTextSharp.text.pdf.AcroFields;
using Microsoft.AspNetCore.Http;

namespace CAAMarketing.Controllers
{
    [Authorize]
    public class InventoryTransfersController : Controller
    {
        private readonly CAAContext _context;
        private readonly IToastNotification _toastNotification;

        public InventoryTransfersController(CAAContext context, IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
        }

        // GET: InventoryTransfers
        public async Task<IActionResult> Index(string SearchString, int? FromLocationID, int? ToLocationId, int? ItemID,
           int? page, int? pageSizeID, string actionButton, string sortDirection = "asc", string sortField = "Title")
        {

            ViewDataReturnURL();

            if (HttpContext.Session.GetString("TransferRecordArchived") == "True")
            {
                _toastNotification.AddSuccessToastMessage("Transfer Record Archived <br/> <a href='/Archives/TransferArchivesIndex'>View Transfer Archives.</a>");
            }
            HttpContext.Session.SetString("TransferRecordArchived", "False");

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
                .Where(i => i.Archived == false)
                .AsNoTracking();

            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "ItemTransfered", "From Location", "To Location", "Quantity", "TransferDate", "Items", "Title" };


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
            if (sortField == "Transfer Date")
            {
                if (sortDirection == "asc")
                {
                    transfersICollection = transfersICollection
                        .OrderBy(p => p.TransferDate);
                }
                else
                {
                    transfersICollection = transfersICollection
                        .OrderByDescending(p => p.TransferDate);
                }
            }
            else if (sortField == "Items")
            {
                if (sortDirection == "asc")
                {
                    transfersICollection = transfersICollection
                        .OrderBy(p => p.InventoryTransfers.FirstOrDefault().Item.Name);
                }
                else
                {
                    transfersICollection = transfersICollection
                        .OrderByDescending(p => p.InventoryTransfers.FirstOrDefault().Item.Name);
                }
            }
            else if (sortField == "Quantity")
            {
                if (sortDirection == "asc")
                {
                    transfersICollection = transfersICollection
                        .OrderByDescending(p => p.InventoryTransfers.FirstOrDefault().Quantity);
                }
                else
                {
                    transfersICollection = transfersICollection
                        .OrderBy(p => p.InventoryTransfers.FirstOrDefault().Quantity);
                }
            }
            else if (sortField == "To Location")
            {
                if (sortDirection == "asc")
                {
                    transfersICollection = transfersICollection
                        .OrderBy(p => p.InventoryTransfers.FirstOrDefault().ToLocation.Name);
                }
                else
                {
                    transfersICollection = transfersICollection
                        .OrderByDescending(p => p.InventoryTransfers.FirstOrDefault().ToLocation.Name);
                }
            }
            else if (sortField == "From Location")
            {
                if (sortDirection == "asc")
                {
                    transfersICollection = transfersICollection
                        .OrderBy(p => p.InventoryTransfers.FirstOrDefault().FromLocation.Name);
                }
                else
                {
                    transfersICollection = transfersICollection
                        .OrderByDescending(p => p.InventoryTransfers.FirstOrDefault().FromLocation.Name);
                }
            }
            else //Sorting by Title
            {
                if (sortDirection == "asc")
                {
                    transfersICollection = transfersICollection
                        .OrderBy(p => p.Title);
                }
                else
                {
                    transfersICollection = transfersICollection
                        .OrderByDescending(p => p.Title);
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

        // GET: InventoryTransfers/Create
        public IActionResult Create(int? ItemId, int FromLocationId, int ToLocationId, DateTime TransferDate)
        {

            _toastNotification.AddAlertToastMessage($"Please Start By Entering Information Of The Transfer, You Can Cancel By Clicking The Exit Button.");

            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (!ItemId.HasValue)
            {
                return Redirect(ViewData["returnURL"].ToString());
            }
            ViewData["ItemName"] = ItemId;
            InventoryTransfer a = new InventoryTransfer()
            {
                ItemId = ItemId.GetValueOrDefault()
            };


            var quantity = 0;
            // Get the quantity value from TempData



            var existingLocationIds = _context.Inventories.Where(i => i.ItemID == ItemId).Select(i => i.LocationID).Distinct().ToList();
            var locations = _context.Locations.Where(l => existingLocationIds.Contains(l.Id)).ToList();
            var GetTotalStock = _context.Items
            .Where(i => i.ID == ItemId)
            .SelectMany(i => i.Inventories)
            .ToList();
            TempData["numOfLocations"] = locations.Count();
            foreach (var loc in locations)
            {
                var tempname = _context.Locations.Where(i => i.Id == loc.Id).Select(i => i.Name).First();
                quantity = 0;
                quantity += GetTotalStock
                    .Where(i => i.LocationID == loc.Id)
                    .Sum(i => i.Quantity);
                TempData[loc.Id.ToString()] = quantity;
            }

            ViewData["FromLocationId"] = new SelectList(locations, "Id", "Name", FromLocationId);

            ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name", ItemId);
            ViewData["ToLocationId"] = new SelectList(_context.Locations, "Id", "Name", ToLocationId);


            return View(a);
        }

        // POST: InventoryTransfers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ItemId,FromLocationId,ToLocationId,Quantity,TransferDate")] InventoryTransfer inventoryTransfer)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();
            int ItemId = inventoryTransfer.ItemId;
            int ToLocationId = inventoryTransfer.ToLocationId;
            int FromLocationId = inventoryTransfer.FromLocationId;

            if (inventoryTransfer.Quantity < 0)
            {
                _toastNotification.AddErrorToastMessage("Oops, you can put negative values. Review your values and try again.");

                var quantity = 0;
                // Get the quantity value from TempData



                var existingLocationIds = _context.Inventories.Where(i => i.ItemID == ItemId).Select(i => i.LocationID).Distinct().ToList();
                var locations = _context.Locations.Where(l => existingLocationIds.Contains(l.Id)).ToList();
                var GetTotalStock = _context.Items
                .Where(i => i.ID == ItemId)
                .SelectMany(i => i.Inventories)
                .ToList();
                TempData["numOfLocations"] = locations.Count();
                foreach (var loc in locations)
                {
                    var tempname = _context.Locations.Where(i => i.Id == loc.Id).Select(i => i.Name).First();
                    quantity = 0;
                    quantity += GetTotalStock
                        .Where(i => i.LocationID == loc.Id)
                        .Sum(i => i.Quantity);
                    TempData[loc.Id.ToString()] = quantity;
                }

                ViewData["FromLocationId"] = new SelectList(locations, "Id", "Name", FromLocationId);

                ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name", ItemId);
                ViewData["ToLocationId"] = new SelectList(_context.Locations, "Id", "Name", ToLocationId);

                return RedirectToAction("Create", "InventoryTransfers", new { inventoryTransfer.ItemId });
            }
            else
            {
                if (ModelState.IsValid)
                {
                    // Find the inventory record for the item being transferred from the specified location
                    var fromInventory = await _context.Inventories
                        .Include(i => i.Location)
                        .FirstOrDefaultAsync(i => i.ItemID == inventoryTransfer.ItemId && i.LocationID == inventoryTransfer.FromLocationId);

                    if (fromInventory.Quantity < inventoryTransfer.Quantity)
                    {
                        ModelState.AddModelError("Quantity", "Not enough inventory to transfer.");
                        return View(inventoryTransfer);
                    }

                    // Update the from location to the current location of the inventory item
                    inventoryTransfer.FromLocationId = fromInventory.LocationID;

                    // Update the inventory quantity at the from location
                    fromInventory.Quantity -= inventoryTransfer.Quantity;
                    //_context.Update(fromInventory);

                    // Find the inventory record for the item being transferred to the specified location
                    var toInventory = await _context.Inventories
                        .Include(i => i.Location)
                        .FirstOrDefaultAsync(i => i.ItemID == inventoryTransfer.ItemId && i.LocationID == inventoryTransfer.ToLocationId);

                    //if (toInventory == null)
                    //{
                    //    // Create a new inventory record if one doesn't exist at the to location
                    //    toInventory = new Inventory
                    //    {
                    //        ItemID = inventoryTransfer.ItemId,
                    //        LocationID = inventoryTransfer.ToLocationId,
                    //        Quantity = inventoryTransfer.Quantity,
                    //        Cost = fromInventory.Cost
                    //    };
                    //    _context.Add(toInventory);
                    //}
                    //else
                    //{
                    //    // Update the inventory quantity at the to location
                    //    toInventory.Quantity += inventoryTransfer.Quantity;
                    //    _context.Update(toInventory);
                    //}

                    // Update the to location
                    //inventoryTransfer.ToLocationId = toInventory.LocationID;


                    Transfer Trans = new Transfer();
                    Trans.Title = "Single Transfer";
                    Trans.TransferDate = DateTime.Now;
                    Trans.ToLocationID = inventoryTransfer.ToLocationId;
                    _context.Add(Trans);
                    await _context.SaveChangesAudit();

                    InventoryTransfer InvTrans = new InventoryTransfer();

                    InvTrans.ItemId = inventoryTransfer.ItemId;
                    InvTrans.FromLocationId = inventoryTransfer.FromLocationId;
                    InvTrans.ToLocationId = inventoryTransfer.ToLocationId;
                    InvTrans.Quantity = inventoryTransfer.Quantity;
                    InvTrans.TransferDate = inventoryTransfer.TransferDate;
                    InvTrans.TransferId = Trans.Id;

                    _toastNotification.AddSuccessToastMessage("Single Transfer Created!, View In Master Details Page");
                    // Save changes to the database
                    await _context.AddAsync(InvTrans);
                    await _context.SaveChangesAudit();

                    return RedirectToAction("Index", "OrderItems", new { inventoryTransfer.ItemId });
                }
                else
                {

                    var quantity = 0;
                    // Get the quantity value from TempData



                    var existingLocationIds = _context.Inventories.Where(i => i.ItemID == ItemId).Select(i => i.LocationID).Distinct().ToList();
                    var locations = _context.Locations.Where(l => existingLocationIds.Contains(l.Id)).ToList();
                    var GetTotalStock = _context.Items
                    .Where(i => i.ID == ItemId)
                    .SelectMany(i => i.Inventories)
                    .ToList();
                    TempData["numOfLocations"] = locations.Count();
                    foreach (var loc in locations)
                    {
                        var tempname = _context.Locations.Where(i => i.Id == loc.Id).Select(i => i.Name).First();
                        quantity = 0;
                        quantity += GetTotalStock
                            .Where(i => i.LocationID == loc.Id)
                            .Sum(i => i.Quantity);
                        TempData[loc.Id.ToString()] = quantity;
                    }

                    ViewData["FromLocationId"] = new SelectList(locations, "Id", "Name", FromLocationId);

                    ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name", ItemId);
                    ViewData["ToLocationId"] = new SelectList(_context.Locations, "Id", "Name", ToLocationId);




                }
            }



            




            // If ModelState is invalid, return the view with the input inventoryTransfer
            return View(inventoryTransfer);
        }


        // GET: InventoryTransfers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.InventoryTransfers == null)
            {
                return NotFound();
            }

            var inventoryTransfer = await _context.InventoryTransfers.FindAsync(id);
            if (inventoryTransfer == null)
            {
                return NotFound();
            }
            ViewData["FromLocationId"] = new SelectList(_context.Locations, "Id", "Name", inventoryTransfer.FromLocationId);
            ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name", inventoryTransfer.ItemId);
            ViewData["ToLocationId"] = new SelectList(_context.Locations, "Id", "Name", inventoryTransfer.ToLocationId);
            return View(inventoryTransfer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ItemId,FromLocationId,ToLocationId,Quantity,TransferDate")] InventoryTransfer inventoryTransfer, Byte[] RowVersion)
        {
            // Get the URL with the last filter, sort, and page parameters for this controller
            ViewDataReturnURL();

            // Get the InventoryTransfer to update
            var transferToUpdate = await _context.InventoryTransfers.FirstOrDefaultAsync(t => t.Id == id);

            if (transferToUpdate == null)
            {
                return NotFound();
            }

            // Set the original RowVersion value for the entity
            _context.Entry(transferToUpdate).Property("RowVersion").OriginalValue = RowVersion;

            // Find the inventory record for the item being transferred from the specified location
            var fromInventory = await _context.Inventories
                .Include(i => i.Location)
                .FirstOrDefaultAsync(i => i.ItemID == inventoryTransfer.ItemId && i.LocationID == inventoryTransfer.FromLocationId);

            if (fromInventory.Quantity < inventoryTransfer.Quantity)
            {
                ModelState.AddModelError("Quantity", "Not enough inventory to transfer.");

            }

            // Find the inventory record for the item being transferred to the specified location
            var toInventory = await _context.Inventories
                .Include(i => i.Location)
                .FirstOrDefaultAsync(i => i.ItemID == inventoryTransfer.ItemId && i.LocationID == inventoryTransfer.ToLocationId);

            // Calculate the difference in inventory quantities
            int quantityDifference = inventoryTransfer.Quantity - transferToUpdate.Quantity;

            // Update the from location inventory
            fromInventory.Quantity -= quantityDifference;
            _context.Update(fromInventory);

            if (toInventory == null)
            {
                // Create a new inventory record if one doesn't exist at the to location
                toInventory = new Inventory
                {
                    ItemID = inventoryTransfer.ItemId,
                    LocationID = inventoryTransfer.ToLocationId,
                    Quantity = inventoryTransfer.Quantity,
                    Cost = fromInventory.Cost
                };
                _context.Add(toInventory);
            }
            else
            {
                // Update the inventory quantity at the to location
                toInventory.Quantity += quantityDifference;
                _context.Update(toInventory);
            }

            // Update the InventoryTransfer with the values posted
            if (await TryUpdateModelAsync<InventoryTransfer>(transferToUpdate, "",
                t => t.ItemId, t => t.FromLocationId, t => t.ToLocationId, t => t.Quantity, t => t.TransferDate))
            {
                try
                {
                    // Save changes to the database
                    await _context.SaveChangesAudit();

                    // Redirect to the updated InventoryTransfer's details page
                    return RedirectToAction("Index", "OrderItems", new { transferToUpdate.ItemId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InventoryTransferExists(transferToUpdate.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "The record you attempted to edit "
                            + "was modified by another user. Please go back and refresh.");
                    }
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Unable to save changes. Please contact your system administrator.");
                }
            }

            // If the update was unsuccessful, populate ViewData with the original values and return to the Edit view
            ViewData["FromLocationId"] = new SelectList(_context.Locations, "Id", "Name", transferToUpdate.FromLocationId);
            ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name", transferToUpdate.ItemId);
            ViewData["ToLocationId"] = new SelectList(_context.Locations, "Id", "Name", transferToUpdate.ToLocationId);
            return View(transferToUpdate);
        }

        // GET: InventoryTransfers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.InventoryTransfers == null)
            {
                return NotFound();
            }

            var inventoryTransfer = await _context.InventoryTransfers
                .Include(i => i.FromLocation)
                .Include(i => i.Item)
                .Include(i => i.ToLocation)
                .FirstOrDefaultAsync(m => m.Id == 1);
            if (inventoryTransfer == null)
            {
                return NotFound();
            }

            return View(inventoryTransfer);
        }

        // POST: InventoryTransfers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (_context.InventoryTransfers == null)
            {
                return Problem("Entity set 'CAAContext.InventoryTransfers'  is null.");
            }
            var inventoryTransfer = await _context.InventoryTransfers.FindAsync(id);
            if (inventoryTransfer != null)
            {
                _context.InventoryTransfers.Remove(inventoryTransfer);
            }

            await _context.SaveChangesAudit();
            //return RedirectToAction(nameof(Index));
            return Redirect(ViewData["returnURL"].ToString());

        }



        // GET: InventoryTransfers/Delete/5
        public async Task<IActionResult> Archive(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Transfers == null)
            {
                return NotFound();
            }

            var Transfer = await _context.Transfers
                .Include(i => i.InventoryTransfers).ThenInclude(i => i.Item).ThenInclude(i => i.Inventories)
                .Include(i => i.InventoryTransfers).ThenInclude(i => i.FromLocation)
                .Include(i => i.InventoryTransfers).ThenInclude(i => i.ToLocation)
                .FirstOrDefaultAsync(m => m.Id == id);



            if (Transfer == null)
            {
                return NotFound();
            }

            return View(Transfer);
        }

        // POST: InventoryTransfers/Archive
        [HttpPost]
        public IActionResult Archive(int id)
        {
            try
            {
                var transfer = _context.Transfers.Find(id);
                if (transfer == null)
                {
                    return Json(new { success = false, message = "Transfer not found" });
                }

                transfer.Archived = true;
                _context.SaveChanges();


                var invTransfers = _context.InventoryTransfers
                    .Where(i => i.TransferId == id && i.IsComfirmed == false);

                foreach (var invtrans in invTransfers)
                {
                    var inventory = _context.Inventories
                        .Where(i => i.ItemID == invtrans.ItemId && i.LocationID == invtrans.FromLocationId)
                        .FirstOrDefault();

                    inventory.Quantity += invtrans.Quantity;
                    _context.Update(inventory);
                    _context.SaveChanges();
                }

                // Get the value of MySessionVariable from the session state
                HttpContext.Session.SetString("TransferRecordArchived", "True");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: InventoryTransfers/Delete/5
        public async Task<IActionResult> Recover(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Transfers == null)
            {
                return NotFound();
            }

            var Transfer = await _context.Transfers
                .Include(i => i.InventoryTransfers).ThenInclude(i => i.Item).ThenInclude(i => i.Inventories)
                .Include(i => i.InventoryTransfers).ThenInclude(i => i.FromLocation)
                .Include(i => i.InventoryTransfers).ThenInclude(i => i.ToLocation)
                .FirstOrDefaultAsync(m => m.Id == id);



            if (Transfer == null)
            {
                return NotFound();
            }

            return View(Transfer);
        }

        // POST: InventoryTransfers/Archive
        [HttpPost]
        public IActionResult Recover(int id)
        {
            try
            {
                var transfer = _context.Transfers.Find(id);
                if (transfer == null)
                {
                    return Json(new { success = false, message = "Transfer not found" });
                }

                transfer.Archived = false;
                _context.SaveChanges();

                var invTransfers = _context.InventoryTransfers
                    .Where(i => i.TransferId == id && i.IsComfirmed == false);

                foreach (var invtrans in invTransfers)
                {
                    var inventory = _context.Inventories
                        .Where(i => i.ItemID == invtrans.ItemId && i.LocationID == invtrans.FromLocationId)
                        .FirstOrDefault();

                    inventory.Quantity -= invtrans.Quantity;
                    _context.Update(inventory);
                    _context.SaveChanges();
                }
                // Get the value of MySessionVariable from the session state
                HttpContext.Session.SetString("TransferRecordRecovered", "True");

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        // GET: SelectItems
        public async Task<IActionResult> SelectItems(string SearchString, int? page, int? pageSizeID
            , string actionButton, string sortDirection = "asc", string sortField = "Item")
        {

            // Perform any necessary logic here
            if (HttpContext.Session.GetString("ClickedAddMultipleTransferInOrderItems") == "True")
            {
                var singleitem = await _context.Items
                    .Where(i => i.ID == Convert.ToInt32(HttpContext.Session.GetString("ItemIDFromSingleTransferBooking")))
                    .FirstOrDefaultAsync();

                singleitem.isSlectedForEvent = true;
                _context.Update(singleitem);
                _context.SaveChanges();
                HttpContext.Session.SetString("ClickedAddMultipleTransferInOrderItems", "False");
            }


            ViewDataReturnURL();

            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);


            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering
                                        //Then in each "test" for filtering, add ViewData["Filtering"] = " show" if true;




            // List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "Event", "Date", "Location" };


            int toLocationId = Convert.ToInt32(HttpContext.Session.GetString("ToLocationForTransfer"));


            var items = _context.Items
                .Include(i => i.Supplier)
                .Include(i => i.Category)
                .Include(i => i.Inventories)
                .Include(i => i.ItemImages).Include(i => i.ItemThumbNail)
                .Include(i => i.Inventories).ThenInclude(i => i.Location)
                .Where(i => i.Inventories.Count(inv => inv.LocationID == toLocationId) < i.Inventories.Count)
                .AsNoTracking();




            if (!String.IsNullOrEmpty(SearchString))
            {
                items = items.Where(p => p.Name.ToUpper().Contains(SearchString.ToUpper()));
                ViewData["Filtering"] = " show";
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

            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;


            var SelectedItems = await _context.Items.Include(i => i.Supplier).Include(i => i.Category).Include(i => i.ItemImages).Include(i => i.ItemThumbNail)
            .ToListAsync();

            ViewBag.SelectedItems = SelectedItems;
            _toastNotification.AddInfoToastMessage("Please Select The Items That Are Needed For This Booking");
            //Handle Paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "Items");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<Models.Item>.CreateAsync(items.AsNoTracking(), page ?? 1, pageSize);
            return View(pagedData);
        }




        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> SelectItems(int ItemID)
        {
            if (ModelState.IsValid)
            {
                var itemsupdate = _context.Items.Include(i => i.ItemImages).Include(i => i.ItemThumbNail).Where(i => i.ID == ItemID).FirstOrDefault();

                itemsupdate.isSlectedForEvent = true;
                _context.Update(itemsupdate);
                _context.SaveChanges();

                //_context.SaveChanges();

                return RedirectToAction("SelectItems", "InventoryTransfers");
            }
            else
            {
                // Return a validation error if the model is invalid
                _toastNotification.AddErrorToastMessage($"Oops! There was an issue saving the record, please check your input and try again, if the problem continues, try again later.");
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage);
                return View();
            }
        }


        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> RemoveSelectedItems(int ItemID)
        {
            if (ModelState.IsValid)
            {

                var itemsupdate = _context.Items.Include(i => i.ItemImages).Include(i => i.ItemThumbNail).Where(i => i.ID == ItemID).FirstOrDefault();

                itemsupdate.isSlectedForEvent = false;
                _context.Update(itemsupdate);
                _context.SaveChanges();

                //_context.SaveChanges();

                return RedirectToAction("SelectItems", "InventoryTransfers");
            }
            else
            {
                // Return a validation error if the model is invalid
                _toastNotification.AddErrorToastMessage($"Oops! There was an issue saving the record, please check your input and try again, if the problem continues, try again later.");
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage);
                return View();
            }
        }



        // GET: SelectItems
        public async Task<IActionResult> ChooseItemQuantities()
        {

            ViewDataReturnURL();

            var events = _context.Items.Include(i => i.Supplier)
                .AsNoTracking();

            var SelectedItems = await _context.Items
                .Include(i => i.Supplier)
                .Include(i => i.Category)
                .Include(i => i.Inventories)
                .Include(i => i.ItemImages).Include(i => i.ItemThumbNail)
                .Include(i => i.Inventories).ThenInclude(i => i.Location)
                .Where(i => i.isSlectedForEvent == true)
            .ToListAsync();
            _toastNotification.AddInfoToastMessage("Please Choose The Locations And Quantites For Each Item");

            return View(SelectedItems);


        }


        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> ChooseItemQuantities(int id)
        {
            string output = "";
            bool flag = false;
            string transferDateString = HttpContext.Session.GetString("TransferDateForTransfer");
            int transferIdInt = Convert.ToInt32(HttpContext.Session.GetString("TransferIdForTransfer"));



            bool OverQuantityFlag = false;

            foreach (var item in _context.Items)
            {
                if (Request.Form.ContainsKey("itemId" + item.ID.ToString()))
                {
                    //Getting the quantity of the item and location selected
                    int Quantity = int.Parse(Request.Form["itemId" + item.ID.ToString()]);

                    var itemName = _context.Items.FirstOrDefault(i => i.ID == item.ID);



                    //Getting id of location so I can display the name (I dont think I need to siplay name but for testing purposes)
                    int locationID = 0;

                    if (Request.Form.ContainsKey("locationId" + item.ID.ToString()))
                    {
                        if (int.TryParse(Request.Form["locationId" + item.ID.ToString()], out int parsedLocationID))
                        {
                            locationID = parsedLocationID;
                        }
                        else
                        {

                        }
                    }
                    // Update the inventory quantity
                    var inventory = await _context.Inventories.Include(i => i.Location).Include(i => i.Item)
                        .Where(i => i.ItemID == item.ID && i.LocationID == locationID)
                        .FirstOrDefaultAsync();
                    string OverQtyOutput = "";
                    if (inventory.Quantity < Quantity)
                    {
                        OverQuantityFlag = true;
                        _toastNotification.AddErrorToastMessage($"Oops, You entered <u>invalid quantity</u> {Quantity}. This exceeds the {inventory.Item.Name} stock, of {inventory.Quantity}. Please Try Again..." +
                            $"");
                    }
                    if (Quantity < 0)
                    {
                        OverQuantityFlag = true;
                        _toastNotification.AddErrorToastMessage($"Oops, You cant enter negative numbers for item: {inventory.Item.Name}... Please Try Again" +
                            $"");
                    }

                }
            }


            if (OverQuantityFlag == false)
            {
                foreach (var item in _context.Items)
                {
                    if (Request.Form.ContainsKey("itemId" + item.ID.ToString()))
                    {
                        //Getting the quantity of the item and location selected
                        int Quantity = int.Parse(Request.Form["itemId" + item.ID.ToString()]);

                        //Getting id of location so I can display the name (I dont think I need to siplay name but for testing purposes)
                        int fromlocationID = int.Parse(Request.Form["locationId" + item.ID.ToString()]);
                        //Getting the Name of the location they selected by id
                        var location = _context.Locations
                            .Where(i => i.Id == fromlocationID)
                            .FirstOrDefault();

                        //Outputted a message to see if my logic worked, and It Did!
                        output += "Name: " + item.Name.ToString() + ", Location: " + location.Name + ", Qty: " + Quantity + "\n";


                        //// Update the inventory quantity
                        var inventory = await _context.Inventories.Include(i => i.Location).Include(i => i.Item)
                            .Where(i => i.ItemID == item.ID && i.LocationID == fromlocationID)
                            .FirstOrDefaultAsync();


                        //// Find the inventory record for the item being transferred from the specified location
                        var fromInventory = await _context.Inventories
                            .Include(i => i.Location)
                           .FirstOrDefaultAsync(i => i.ItemID == item.ID && i.LocationID == fromlocationID);

                        if (fromInventory.Quantity < Quantity)
                        {
                            ModelState.AddModelError("Quantity", "Not enough inventory to transfer.");
                            flag = true;
                            _toastNotification.AddErrorToastMessage($"Oops, You entered a quantity that exceeds the stock of {fromInventory.Item.Name} At {fromInventory.Location.Name}, Please enter a valid Quantity that is under {fromInventory.Quantity}");

                        }
                        else
                        {
                            // Update the from location to the current location of the inventory item
                            //inventoryTransfer.FromLocationId = fromInventory.LocationID;

                            // Update the inventory quantity at the from location
                            fromInventory.Quantity -= Quantity;
                            _context.Update(fromInventory);

                            //// Find the inventory record for the item being transferred to the specified location
                            var toInventory = await _context.Inventories
                                .Include(i => i.Location)
                                .FirstOrDefaultAsync(i => i.ItemID == item.ID && i.LocationID == Convert.ToInt32(HttpContext.Session.GetString("ToLocationForTransfer")));

                            //if (toInventory == null)
                            //{
                            //    // Create a new inventory record if one doesn't exist at the to location
                            //    toInventory = new Inventory
                            //    {
                            //        ItemID = item.ID,
                            //        LocationID = Convert.ToInt32(HttpContext.Session.GetString("ToLocationForTransfer")),
                            //        Quantity = Quantity,
                            //        Cost = fromInventory.Cost
                            //    };
                            //    _context.Add(toInventory);
                            //   }
                            //else
                            //{
                            //    // Update the inventory quantity at the to location
                            //    toInventory.Quantity += Quantity;
                            //    _context.Update(toInventory);
                            //}


                            if (flag == true) { }
                            var inventoryTransfer = new InventoryTransfer();
                            inventoryTransfer.ItemId = item.ID;
                            inventoryTransfer.Quantity = Quantity;
                            inventoryTransfer.ToLocationId = Convert.ToInt32(HttpContext.Session.GetString("ToLocationForTransfer"));
                            inventoryTransfer.FromLocationId = fromlocationID;
                            inventoryTransfer.IsComfirmed = false;
                            inventoryTransfer.TransferId = transferIdInt;
                            //inventoryTransfer.TransferDate = DateTime.Today;
                            if (DateTime.TryParse(transferDateString, out DateTime transferDate))
                            {
                                inventoryTransfer.TransferDate = transferDate;
                            }
                            else
                            {
                                // handle the case where the string is not a valid date
                                _toastNotification.AddErrorToastMessage($"The Transfer Date Is Invalid...");
                            }

                            // Save changes to the database
                            await _context.AddAsync(inventoryTransfer);

                        }


                    }
                }

                //Means there wasnt any errors and all the create statements were added
                if (flag == false)
                {
                    foreach (var item in _context.Items)
                    {
                        item.isSlectedForEvent = false;
                        _context.Update(item);

                    }
                    _context.SaveChanges();
                    await _context.SaveChangesAudit();
                    //_toastNotification.AddErrorToastMessage($"{output} EventID: {EventID.ToString()}");
                    _toastNotification.AddSuccessToastMessage("Item Transfer Created! You can view them all in this index.");
                    //_toastNotification.AddSuccessToastMessage($"{output}");
                    return RedirectToAction("Index", "InventoryTransfers");
                }
            }


            return RedirectToAction("ChooseItemQuantities", "InventoryTransfers");
            //return RedirectToAction("ChooseItemQuantities", "InventoryTransfers");
        }


        // GET: Events/Create
        public IActionResult CreateMultipleTransfers()
        {
            _toastNotification.AddAlertToastMessage($"Please Start By Entering Information Of The Transfer, You Can Cancel By Clicking The Exit Button.");

            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            ViewData["ToLocationId"] = new SelectList(_context.Locations, "Id", "Name");


            return View();
        }

        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> CreateMultipleTransfers(InventoryTransfer model)
        {
            ViewData["ToLocationId"] = new SelectList(_context.Locations, "Id", "Name");
            // _toastNotification.AddErrorToastMessage($"ToLocation: {model.ToLocationId}");
            //  _toastNotification.AddErrorToastMessage($"Transfer Date: {model.TransferDate}");

            // Create a new inventory record if one doesn't exist at the to location
            var newTransfer = new Transfer();
            newTransfer.Title = "Bulk Transfer";
            newTransfer.TransferDate = model.TransferDate;
            newTransfer.ToLocationID = model.ToLocationId;
            _context.Add(newTransfer);
            await _context.SaveChangesAudit();


            var location = _context.Locations
                .Where(i => i.Id == model.ToLocationId)
                .FirstOrDefault();


            // Get the value of MySessionVariable from the session state
            HttpContext.Session.SetString("ToLocationNameForTransfer", location.Name);

            // Get the value of MySessionVariable from the session state
            HttpContext.Session.SetString("ToLocationForTransfer", model.ToLocationId.ToString());

            // Get the value of MySessionVariable from the session state
            HttpContext.Session.SetString("TransferIdForTransfer", newTransfer.Id.ToString());

            // Get the value of MySessionVariable from the session state
            HttpContext.Session.SetString("TransferDateForTransfer", model.TransferDate.ToString());

            foreach (var item in _context.Items)
            {
                item.isSlectedForEvent = false;
                _context.Update(item);

            }
            await _context.SaveChangesAudit();
            return RedirectToAction("SelectItems", "InventoryTransfers");

        }


        public async Task<IActionResult> ComfirmMultiple(int? id)
        {

            ViewDataReturnURL();


            var itemDetails = await _context.Transfers
                .Include(e => e.InventoryTransfers).ThenInclude(i => i.FromLocation)
                .Include(e => e.InventoryTransfers).ThenInclude(i => i.ToLocation)
                .Include(e => e.InventoryTransfers).ThenInclude(e => e.Item)
                .Include(e => e.InventoryTransfers).ThenInclude(e => e.Item).ThenInclude(e => e.Inventories)
                .FirstOrDefaultAsync(m => m.Id == id);

            _toastNotification.AddInfoToastMessage("Please Comfirm The Items That Are Recieved Back From This Transfer.");
            return View(itemDetails.InventoryTransfers);
        }


        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> ComfirmMultiple(int id, IFormCollection form, string missingReportBool, string Notes, string Reason)
        {
            string itemIDCheck = form["formId"]; // Get the form ID
            string QuantityCheck = form[$"itemId{itemIDCheck}"].ToString(); // Get the item ID
            int EventIDCheck = id;
            if (missingReportBool == "True")
            {


                // Get the value of MySessionVariable from the session state
                int InvTransferID = Convert.ToInt32(HttpContext.Session.GetString("InvTransferForLogMethod"));

                // Get the value of MySessionVariable from the session state
                //int ToinventoryID = Convert.ToInt32(HttpContext.Session.GetString("ToInventoryForLogMethod"));

                // Get the value of MySessionVariable from the session state
                int userQuantity = Convert.ToInt32(HttpContext.Session.GetString("UserQuantityForLogMethod"));

                // Get the value of MySessionVariable from the session state
                int itemQtyVariance = Convert.ToInt32(HttpContext.Session.GetString("ItemQtyVarianceForLogMethod"));

                //Go get the ItemReservation to update
                var InvTransferToComfirm = await _context.InventoryTransfers.Include(ir => ir.Transfer).Include(ir => ir.Item).Include(i => i.ToLocation).Include(i => i.FromLocation)
                    .Where(i => i.Id == InvTransferID)
                    .FirstOrDefaultAsync();
                // Update the inventory quantity
                var toInventory = await _context.Inventories.Include(i => i.Location).Include(i => i.Item)
                    .Where(i => i.LocationID == InvTransferToComfirm.ToLocationId && i.ItemID == InvTransferToComfirm.ItemId)
                    .FirstOrDefaultAsync();

                var item = _context.Items
               .Where(i => i.ID == InvTransferToComfirm.ItemId)
               .FirstOrDefault();


                item.Archived = false;
                _context.Update(item);


                if (toInventory == null)
                {
                    // Create a new inventory record if one doesn't exist at the to location
                    toInventory = new Inventory
                    {
                        ItemID = InvTransferToComfirm.ItemId,
                        LocationID = InvTransferToComfirm.ToLocationId,
                        Quantity = userQuantity,
                        Cost = 9.99M
                    };
                    _context.Add(toInventory);
                }
                else
                {
                    toInventory.Quantity += Convert.ToInt32(userQuantity); //ADDING THE INVENTORY QUANTITY BACK FOR ITS ASSIGNED LOCATION
                    _context.Update(toInventory);
                }


                //_toastNotification.AddSuccessToastMessage($"Notes: {Notes}, Reason: {Reason}, Item: {itemReservationToRemove.Item.Name}, Qty: {itemReservationToRemove.Quantity}, UserQty: {userQuantity}, Inv Record: {inventory.Item.Name} - {inventory.Location.Name}");

                //FOR GETTING THE EMPLOYEE THAT LOGGED THIS AND CAUSED A QUANTITY VARIANCE
                var email = User.Identity.Name;
                var employee = _context.Employees.FirstOrDefault(e => e.Email == email);

                //MAKING A MISSINGITEMLOG RECORD FOR THE VARIANCE OF THIS ITEM
                var addMissingItemLog = new MissingTransitItem();
                addMissingItemLog.Notes = Notes;
                addMissingItemLog.Reason = Reason;
                addMissingItemLog.Quantity = itemQtyVariance;
                addMissingItemLog.ItemId = InvTransferToComfirm.ItemId;
                addMissingItemLog.FromLocationID = InvTransferToComfirm.FromLocationId;
                addMissingItemLog.ToLocationID = InvTransferToComfirm.ToLocationId;
                addMissingItemLog.EmployeeID = employee.ID;
                addMissingItemLog.Date = DateTime.Now;

                //SINCE WE ARE GOING TO LOGGED THIS ITEM BACK IN AND ADD THE QUANTITY, I WILL ASSIGN THE BOOL TO TRUE AS IF TECHNICALLY DELETED AND THE USER WILL SEE ITS LOGGED
                InvTransferToComfirm.IsComfirmed = true;
                InvTransferToComfirm.ComfirmedQuantity = userQuantity;


                //ADDING EVERYTHING IN THE DATABASE.
                _context.Add(addMissingItemLog);
                _context.Update(InvTransferToComfirm);
                await _context.SaveChangesAudit();


            }
            else if (Convert.ToInt32(QuantityCheck) < 0)
            {
                _toastNotification.AddErrorToastMessage("Oops, You cant input negative values, review your selections and try again");
                return RedirectToAction("ComfirmMultiple", "InventoryTransfers", id);
            }
            else
            {
                int EventID = id;


                string itemID = form["formId"]; // Get the form ID
                string locationId = form[$"locations-{itemID}"].ToString(); // Get the location ID
                string Quantity = form[$"itemId{itemID}"].ToString(); // Get the item ID



                var location = _context.Locations
                   .Where(i => i.Id == Convert.ToInt32(locationId))
                   .FirstOrDefault();
                var item = _context.Items
                   .Where(i => i.ID == Convert.ToInt32(itemID))
                   .FirstOrDefault();


                item.Archived = false;
                _context.Update(item);



                //Go get the ItemReservation to update
                var InvTransferToRemove = await _context.InventoryTransfers.Include(ir => ir.Transfer).Include(ir => ir.Item).Include(i => i.FromLocation).Include(i => i.ToLocation)
                    .Where(i => i.ItemId == Convert.ToInt32(itemID) && i.Transfer.Id == id && i.IsComfirmed == false)
                    .FirstOrDefaultAsync();

                //// Find the inventory record for the item being transferred to the specified location
                var toInventory = await _context.Inventories
                    .Include(i => i.Location)
                    .Where(i => i.ItemID == item.ID && i.LocationID == InvTransferToRemove.ToLocationId)
                    .FirstOrDefaultAsync();

                if (toInventory == null)
                {
                    // Create a new inventory record if one doesn't exist at the to location
                    toInventory = new Inventory
                    {
                        ItemID = item.ID,
                        LocationID = InvTransferToRemove.ToLocationId,
                        Quantity = Convert.ToInt32(Quantity),
                        Cost = 9.99M
                    };
                    _context.Add(toInventory);
                }
                else
                {
                    // Update the inventory quantity at the to location
                    toInventory.Quantity += Convert.ToInt32(Quantity);
                    _context.Update(toInventory);
                }
                // Update the inventory quantity
                var inventory = await _context.Inventories.Include(i => i.Location).Include(i => i.Item)
                    .Where(i => i.ItemID == item.ID && i.LocationID == location.Id)
                    .FirstOrDefaultAsync();


                if (InvTransferToRemove == null)
                {
                    _toastNotification.AddErrorToastMessage($"Inventory Transfer Record Not Found");
                }
                else if (inventory == null)
                {
                    _toastNotification.AddErrorToastMessage($"Inventory Record Not Found");
                }
                else
                {
                    if (InvTransferToRemove.Quantity < Convert.ToInt32(Quantity))
                    {
                        _toastNotification.AddErrorToastMessage($"Oops, you entered a quantity that is more than what you logged in. Please enter the correct amount.");
                    }
                    //THIS METHOD IS TO POP UP THE MODEL FOR A DISCREPANCY LOG, TOOK A WHILE BUT MADE IT WORK.
                    else if (InvTransferToRemove.Quantity != Convert.ToInt32(Quantity))
                    {
                        TempData["InitateMissingItemLog"] = "Data received";

                        var missingamount = InvTransferToRemove.Quantity - Convert.ToInt32(Quantity);
                        _toastNotification.AddErrorToastMessage($"{InvTransferToRemove.Quantity} {InvTransferToRemove.Item.Name}'s were logged out, you only logged in {Quantity}. Please specify the reason why <u>{missingamount}</u> of {InvTransferToRemove.Item.Name}'s aren't logged.");


                        // Get the value of MySessionVariable from the session state
                        HttpContext.Session.SetString("InvTransferForLogMethod", InvTransferToRemove.Id.ToString());

                        // Get the value of MySessionVariable from the session state
                        HttpContext.Session.SetString("ToInventoryForLogMethod", inventory.Id.ToString());

                        // Get the value of MySessionVariable from the session state
                        HttpContext.Session.SetString("UserQuantityForLogMethod", Quantity.ToString());

                        // Get the value of MySessionVariable from the session state
                        HttpContext.Session.SetString("ItemQtyVarianceForLogMethod", missingamount.ToString());

                    }

                    else
                    {
                        InvTransferToRemove.IsComfirmed = true;
                        InvTransferToRemove.ComfirmedQuantity = Convert.ToInt32(Quantity);
                        _context.Update(InvTransferToRemove);
                        await _context.SaveChangesAudit();
                    }

                }

            }

            return RedirectToAction("ComfirmMultiple", "InventoryTransfers", id);
        }


        public async Task<IActionResult> ComfirmTransitModal(int? id)
        {

            ViewDataReturnURL();


            var itemDetails = await _context.Transfers
                .Include(e => e.InventoryTransfers).ThenInclude(i => i.FromLocation)
                .Include(e => e.InventoryTransfers).ThenInclude(i => i.ToLocation)
                .Include(e => e.InventoryTransfers).ThenInclude(e => e.Item)
                .Include(e => e.InventoryTransfers).ThenInclude(e => e.Item).ThenInclude(e => e.Inventories)
                .FirstOrDefaultAsync(m => m.Id == id);


            return View(itemDetails.InventoryTransfers);
        }


        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> ComfirmTransitModal(int id)
        {
            int TransferID = Convert.ToInt32(HttpContext.Session.GetString("SelectedTransfer"));
            //Go get the InvTransfer to update
            var InvTransferToRemove = await _context.InventoryTransfers.Include(ir => ir.Transfer).Include(ir => ir.Item).Include(i => i.FromLocation).Include(i => i.ToLocation)
                .Where(i => i.Transfer.Id == TransferID && i.IsComfirmed == false)
                .ToListAsync();
            foreach (var invTrans in InvTransferToRemove)
            {
                // Find the inventory record for the item being transferred to the specified location
                var toInventory = await _context.Inventories
                    .Include(i => i.Location)
                    .FirstOrDefaultAsync(i => i.ItemID == invTrans.ItemId && i.LocationID == invTrans.ToLocationId);

                var item = _context.Items
                    .Where(i => i.ID == invTrans.Id)
                    .FirstOrDefault();

                //item.Archived = false;
                //_context.Update(item);

                if (toInventory == null)
                {
                    // Create a new inventory record if one doesn't exist at the to location
                    toInventory = new Inventory
                    {
                        ItemID = invTrans.ItemId,
                        LocationID = invTrans.ToLocationId,
                        Quantity = invTrans.Quantity,
                        Cost = 9.99M
                    };
                    _context.Add(toInventory);
                }
                else
                {
                    // Update the inventory quantity at the to location
                    toInventory.Quantity += invTrans.Quantity;
                    //_context.Update(toInventory);
                }

                if (invTrans == null)
                {
                    _toastNotification.AddErrorToastMessage($"Inventory Transfer Record Not Found");
                }
                else
                {
                    invTrans.IsComfirmed = true;
                    invTrans.ComfirmedQuantity = invTrans.Quantity;
                    //_context.Update(invTrans);
                    await _context.SaveChangesAudit();
                    

                }
                
            }

            _toastNotification.AddSuccessToastMessage("All Transfers Were Succesfully Added To The Inventories");



            return RedirectToAction("Index", "InventoryTransfers");
        }



        public IActionResult SetSelectedTransfer(string selectedId)
        {
            HttpContext.Session.SetString("SelectedTransfer", selectedId);

            //var count = _context.InventoryTransfers.Include(i => i.Item).Include(i => i.ToLocation).Where(i => i.TransferId == Convert.ToInt32(selectedId)).Count();



            // Get the value of Item For the Assigned Transfer
            HttpContext.Session.SetString("ReloadForModal", "true");

            _toastNotification.AddSuccessToastMessage($"Selected Transfer: {selectedId}");
            return Json(new { success = true });
        }

        // GET: SelectItems
        public async Task<IActionResult> EditOverview(int? Id)
        {
            HttpContext.Session.SetString("TransferIDForEditOverView", Id.ToString());
            

            //REMOVED THE SELECTEDEVENT BOOL BEFORE DOING ANY LOGIC
            foreach (var item in _context.Items)
            {
                item.isSlectedForEvent = false;
                _context.Update(item);
                _context.SaveChanges();

            }


            if (Id == null)
            {
                return BadRequest();
            }

            var events = await _context.Transfers
                .FindAsync(Id);

            if (events == null)
            {
                return NotFound();
            }

            var invTransfers = await _context.Transfers
               .Include(e => e.InventoryTransfers).ThenInclude(i => i.Item)
               .Include(e => e.InventoryTransfers).ThenInclude(i => i.FromLocation)
               .Include(i => i.InventoryTransfers).ThenInclude(i => i.Item).ThenInclude(i => i.Inventories)
               .Include(i => i.InventoryTransfers).ThenInclude(i => i.ToLocation)
               .FirstOrDefaultAsync(m => m.Id == Id);

            var invtransferForSession = _context.InventoryTransfers
                .Where(i => i.TransferId == Id)
                .FirstOrDefault();

            if(invTransfers != null)
            {
                HttpContext.Session.SetString("ToLocationIDForEditOverView", invTransfers.ToLocationID.ToString());
            }


            //Since the user clicked on the items and modified it, it will take affect.
            if (HttpContext.Session.GetString("TypeOfOperationReservations") == "EditedItems")
            {
                //GETTING THE ITEMS THAT ARE IN THE RESERVATIONS AND CHANGING THE BOOL TO TRUE FOR OUTPUTTING THE SELECTEDITEMS TO THE PAGE
                var InventoryTransfers = _context.InventoryTransfers.Include(i => i.Item).Include(i => i.Transfer)
                    .Where(i => i.TransferId == Convert.ToInt32(HttpContext.Session.GetString("TransferIDForEditOverView")))
                    .ToList();

                //_toastNotification.AddInfoToastMessage($"{HttpContext.Session.GetString("TransferIDForEditOverView")}");
                foreach (var invTrans in InventoryTransfers)
                {
                    foreach (var item in _context.Items)
                    {
                        if (invTrans.ItemId == item.ID)
                        {
                            item.isSlectedForEvent = true;
                            _context.Update(item);
                            _context.SaveChanges();
                        }
                    }
                }

                var SelectedItems = await _context.Items.Include(i => i.Supplier).Include(i => i.Category).Include(i => i.ItemImages).Include(i => i.ItemThumbNail).Where(i => i.isSlectedForEvent == true)
                .ToListAsync();

                var inv = _context.Inventories
                    .Include(i => i.Item)
                    .Include(i => i.Location)
                    .AsNoTracking();




                ViewBag.invTransfers = invTransfers.InventoryTransfers;
                ViewBag.SelectedItems = SelectedItems;
            }
            else
            {
                //GETTING THE ITEMS THAT ARE IN THE RESERVATIONS AND CHANGING THE BOOL TO TRUE FOR OUTPUTTING THE SELECTEDITEMS TO THE PAGE
                var InventoryTransfers = _context.InventoryTransfers.Include(i => i.Item).Include(i => i.Transfer)
                    .Where(i => i.TransferId == Convert.ToInt32(HttpContext.Session.GetString("TransferIDForEditOverView")))
                    .ToList();
                
                //_toastNotification.AddInfoToastMessage($"{HttpContext.Session.GetString("TransferIDForEditOverView")}");
                foreach (var invTrans in InventoryTransfers)
                {
                    foreach (var item in _context.Items)
                    {
                        if (invTrans.ItemId == item.ID)
                        {
                            item.isSlectedForEvent = true;
                            _context.Update(item);
                            _context.SaveChanges();
                        }
                    }
                }

                var SelectedItems = await _context.Items.Include(i => i.Supplier).Include(i => i.Category).Include(i => i.ItemImages).Include(i => i.ItemThumbNail).Where(i => i.isSlectedForEvent == true)
                .ToListAsync();

                var inv = _context.Inventories
                    .Include(i => i.Item)
                    .Include(i => i.Location)
                    .AsNoTracking();




                ViewBag.invTransfers = invTransfers.InventoryTransfers;
                ViewBag.SelectedItems = SelectedItems;
            }


            //_toastNotification.AddErrorToastMessage($"To Location Name: {HttpContext.Session.GetString("ToLocationNameForEdit")}");

            return View(events);
        }




        // POST: Events/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditOverView(int id, Byte[] RowVersion)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            //Go get the Event to update
            var eventToUpdate = await _context.Events.FirstOrDefaultAsync(e => e.ID == id);

            if (eventToUpdate == null)
            {
                return NotFound();
            }

            //Put the original RowVersion value in the OriginalValues collection for the entity
            _context.Entry(eventToUpdate).Property("RowVersion").OriginalValue = RowVersion;

            //Try updating it with the values posted
            if (await TryUpdateModelAsync<Event>(eventToUpdate, "",
                e => e.Name, e => e.Description, e => e.Date, e => e.location, e => e.ReservedEventDate, e => e.ReturnEventDate))
            {
                try
                {
                    await _context.SaveChangesAudit();
                    //return RedirectToAction(nameof(Index));
                    return RedirectToAction("Details", new { eventToUpdate.ID });

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InventoryTransferExists(eventToUpdate.ID))
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
            return View(eventToUpdate);
        }

        // GET: InventoryTransfers/Edit/5
        public async Task<IActionResult> EditTransferOverview(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Transfers == null)
            {
                return NotFound();
            }

            var inventoryTransfer = await _context.Transfers.FindAsync(id);
            if (inventoryTransfer == null)
            {
                return NotFound();
            }
            return View(inventoryTransfer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTransferOverview(int id, [Bind("Id,Title,TransferDate")] Transfer inventoryTransfer)
        {
            // Get the URL with the last filter, sort, and page parameters for this controller
            ViewDataReturnURL();

            // Get the InventoryTransfer to update
            var transferToUpdate = await _context.Transfers.FirstOrDefaultAsync(t => t.Id == id);

            if (transferToUpdate == null)
            {
                return NotFound();
            }


            // Update the InventoryTransfer with the values posted
            if (await TryUpdateModelAsync<Transfer>(transferToUpdate, "",
                t => t.Title, t => t.TransferDate))
            {
                try
                {
                    // Save changes to the database
                    await _context.SaveChangesAudit();

                    // Redirect to the updated InventoryTransfer's details page
                    return RedirectToAction("EditOverview", "InventoryTransfers", new { transferToUpdate.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InventoryTransferExists(transferToUpdate.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "The record you attempted to edit "
                            + "was modified by another user. Please go back and refresh.");
                    }
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Unable to save changes. Please contact your system administrator.");
                }
            }
            return View(transferToUpdate);
        }

        // GET: SelectItems
        public async Task<IActionResult> EditSelectItems(int? EventID, int? CategoryID, string SearchString, int? page, int? pageSizeID
            , string actionButton, string sortDirection = "asc", string sortField = "Item")
        {
            //_toastNotification.AddInfoToastMessage("Add or Remove The Items That Are In This Transfer");
            _toastNotification.AddInfoToastMessage(HttpContext.Session.GetString("ItemIDForEditOverView"));

            foreach (var item in _context.Items)
            {
                item.isSlectedForEvent = false;
                _context.Update(item);
                _context.SaveChanges();
            }

            ViewDataReturnURL();

            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);


            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering
                                        //Then in each "test" for filtering, add ViewData["Filtering"] = " show" if true;


            ViewData["CategoryID"] = new SelectList(_context.Categories, "Id", "Name");

            // List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "Item", "Category", "UPC" };


            var items = _context.Items
                .Include(i => i.Supplier)
                .Include(i => i.Category)
                .Include(i => i.Inventories)
                .Include(i => i.ItemImages)
                .Include(i => i.ItemThumbNail)
                .Include(i => i.Inventories).ThenInclude(i => i.Location)
                .Where(i => i.Inventories.Count(inv => inv.LocationID == Convert.ToInt32(HttpContext.Session.GetString("ToLocationIDForEditOverView"))) < i.Inventories.Count)
                .AsNoTracking();


            var inv = _context.Inventories
                .Include(i => i.Item)
                .Include(i => i.Location)
                .AsNoTracking();


            if (CategoryID.HasValue)
            {
                items = items.Where(p => p.CategoryID == CategoryID);
                ViewData["Filtering"] = "btn-danger";
            }

            if (!String.IsNullOrEmpty(SearchString))
            {
                items = items.Where(p => p.Name.ToUpper().Contains(SearchString.ToUpper()));
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

            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            int toLocationID = 0;

            var transfers = _context.InventoryTransfers
                .Where(i => i.TransferId == Convert.ToInt32(HttpContext.Session.GetString("TransferIDForEditOverView")));
            foreach (var trans in transfers)
            {
                toLocationID = trans.ToLocationId;
                foreach (var item in _context.Items)
                {
                    if (trans.ItemId == item.ID)
                    {
                        item.isSlectedForEvent = true;
                        _context.Update(item);
                    }
                    _context.SaveChanges();
                }
            }

            var selectedItems = _context.Items
                //.Where(item => item.Inventories.Count(i => i.LocationID == toLocationID) > 1)
                .Include(i => i.Supplier)
                .Include(i => i.Category)
                .Include(i => i.ItemImages)
                .Include(i => i.ItemThumbNail)
                .ToList();
            var LoggedInItems = _context.InventoryTransfers
            //.Where(item => item.Inventories.Count(i => i.LocationID == toLocationID) > 1)
            .Include(i => i.Item)
            .Include(i => i.Transfer)
            .Where(i => i.TransferId == Convert.ToInt32(HttpContext.Session.GetString("TransferIDForEditOverView")) && i.IsComfirmed == true)
            .ToList();


            ViewBag.LoggedInItems = LoggedInItems;

            ViewBag.SelectedItems = selectedItems;
            //Handle Paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "Items");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<CAAMarketing.Models.Item>.CreateAsync(items.AsNoTracking(), page ?? 1, pageSize);
            return View(pagedData);
        }




        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> EditSelectItems(int ItemID)
        {
            //HttpContext.Session.SetString("ItemIDForEditOverView", ItemID.ToString());
            if (ModelState.IsValid)
            {
                bool flag = false;
                var itemsupdate = _context.Items.Include(i => i.ItemImages).Include(i => i.ItemThumbNail).Where(i => i.ID == ItemID).FirstOrDefault();
                var itemTransToMake = _context.InventoryTransfers.Include(i => i.Item).Include(i => i.Transfer)
                    .Where(i => i.TransferId == Convert.ToInt32(HttpContext.Session.GetString("TransferIDForEditOverView")))
                    .ToList();


                var inventory = _context.Inventories
                    .Where(i => i.ItemID == ItemID)
                    .FirstOrDefault();

                foreach (var res in itemTransToMake)
                {
                    if (res.ItemId == ItemID)
                    {
                        flag = true;
                        break;
                    }
                }
                if (flag == false)
                {
                    //CREATING THE RECORDS NOW:
                    InventoryTransfer createItemTransfer = new InventoryTransfer();
                    createItemTransfer.ItemId = ItemID;
                    createItemTransfer.TransferId = Convert.ToInt32(HttpContext.Session.GetString("TransferIDForEditOverView"));
                    //createItemTransfer.TransferId = 1;
                    createItemTransfer.FromLocationId = inventory.LocationID;
                    createItemTransfer.ToLocationId = Convert.ToInt32(HttpContext.Session.GetString("ToLocationIDForEditOverView"));
                    //createItemTransfer.ToLocationId = 1;
                    createItemTransfer.Quantity = 0;
                    createItemTransfer.TransferDate = DateTime.Now;
                    _context.Add(createItemTransfer);

                    itemsupdate.isSlectedForEvent = true;
                    _context.Update(itemsupdate);
                    _context.SaveChanges();
                }


                HttpContext.Session.SetString("TypeOfOperationReservations", "EditedItems");

                //_context.SaveChanges();

                return RedirectToAction("SelectItems", "InventoryTransfers");
            }
            else
            {
                // Return a validation error if the model is invalid
                _toastNotification.AddErrorToastMessage($"Oops! There was an issue saving the record, please check your input and try again, if the problem continues, try again later.");
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage);
                return View();
            }

        }


        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> EditRemoveSelectedItems(int ItemID)
        {
            HttpContext.Session.SetString("ItemIDForOper", ItemID.ToString());
            if (ModelState.IsValid)
            {


                var itemsupdate = _context.Items.Include(i => i.ItemImages).Include(i => i.ItemThumbNail).Where(i => i.ID == ItemID).FirstOrDefault();
                var itemTransToRemove = _context.InventoryTransfers.Include(i => i.Item).Include(i => i.Transfer)
                    .Where(i => i.TransferId == Convert.ToInt32(HttpContext.Session.GetString("TransferIDForEditOverView")))
                    .ToList();

                foreach (var trans in itemTransToRemove)
                {
                    if (trans.ItemId == ItemID)
                    {
                        var inventoryToUpdate = await _context.Inventories.Include(i => i.Location).Include(i => i.Item)
                            .Where(i => i.ItemID == trans.ItemId && i.LocationID == trans.FromLocationId)
                            .FirstOrDefaultAsync();

                        if (inventoryToUpdate != null)
                        {
                            //Updating the Inventory before deleting the reservation record
                            inventoryToUpdate.Quantity += trans.Quantity;
                            _context.Update(inventoryToUpdate);
                        }




                        _context.Remove(trans);
                        _context.SaveChanges();

                    }
                }



                itemsupdate.isSlectedForEvent = false;
                _context.Update(itemsupdate);
                _context.SaveChanges();


                return RedirectToAction("SelectItemsEdit", "InventoryTransfers");
            }
            else
            {
                // Return a validation error if the model is invalid
                _toastNotification.AddErrorToastMessage($"Oops! There was an issue saving the record, please check your input and try again, if the problem continues, try again later.");
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage);
                return View();
            }
        }



        // GET: SelectItems
        public async Task<IActionResult> ChooseItemQuantitiesEdit()
        {

            ViewDataReturnURL();
            _toastNotification.AddInfoToastMessage("Modify The Locations And/Or Quantities For The Transfer");
            //692 - NF
            //592 = Thorold

            var InventoryTransfers = await _context.InventoryTransfers
                .Include(i => i.FromLocation)
                .Include(i => i.ToLocation)
                .Include(i => i.Item).ThenInclude(i => i.Supplier)
                .Include(i => i.Item).ThenInclude(i => i.ItemImages)
                .Include(i => i.Item).ThenInclude(i => i.ItemThumbNail)
                .Include(i => i.Item).ThenInclude(i => i.Category)
                .Include(i => i.Item).ThenInclude(i => i.Inventories).ThenInclude(i => i.Location)
                .Where(i => i.TransferId == Convert.ToInt32(HttpContext.Session.GetString("TransferIDForEditOverView")))
            .ToListAsync();


            return View(InventoryTransfers);




        }


        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> ChooseItemQuantitiesEdit(int id)
        {
            string output = "";
            bool flag = false;
            string transferDateString = HttpContext.Session.GetString("TransferDateForTransfer");
            int transferIdInt = Convert.ToInt32(HttpContext.Session.GetString("TransferIdForTransfer"));

            //CHECKING FOR THE QUANTITY IF USER PUT OVER THE DESIRED STOCK
            bool OverQuantityFlag = false;

            foreach (var item in _context.Items)
            {
                if (Request.Form.ContainsKey("itemId" + item.ID.ToString()))
                {
                    //Getting the quantity of the item and location selected
                    int Quantity = int.Parse(Request.Form["itemId" + item.ID.ToString()]);

                    var itemName = _context.Items.FirstOrDefault(i => i.ID == item.ID);

                    //This code is for when the user doesnt select anything, the outfilled location does not retrive the value here, so this is a shortcut to get its value.
                    var itemTrans = _context.InventoryTransfers
                        .Include(i=>i.FromLocation)
                        .Where(i => i.ItemId == itemName.ID && i.TransferId == Convert.ToInt32(HttpContext.Session.GetString("TransferIDForEditOverView")))
                        .FirstOrDefault();

                    //Getting id of location so I can display the name (I dont think I need to siplay name but for testing purposes)
                    int FromlocationID = 0;

                    if (Request.Form.ContainsKey("locationId" + item.ID.ToString()))
                    {
                        if (int.TryParse(Request.Form["locationId" + item.ID.ToString()], out int parsedLocationID))
                        {
                            FromlocationID = parsedLocationID;
                        }
                        else
                        {

                            FromlocationID = itemTrans.FromLocationId;
                        }
                    }
                    // Update the inventory quantity
                    var inventory = await _context.Inventories.Include(i => i.Location).Include(i => i.Item)
                        .Where(i => i.ItemID == item.ID && i.LocationID == FromlocationID)
                        .FirstOrDefaultAsync();
                    //_toastNotification.AddInfoToastMessage((inventory.Quantity + itemTrans.Quantity).ToString());
                    if(inventory.Location.Name != itemTrans.FromLocation.Name)
                    {
                        if ((inventory.Quantity) < Quantity)
                        {
                            OverQuantityFlag = true;
                            _toastNotification.AddErrorToastMessage($"Oops, You entered <u>invalid quantity</u> {Quantity}. This exceeds the {inventory.Item.Name} stock, of {inventory.Quantity}.  <br/> please review your numbers and try again." +
                                $"");
                        }
                    }
                    else if ((inventory.Quantity + itemTrans.Quantity) < Quantity)
                    {
                        OverQuantityFlag = true;
                        _toastNotification.AddErrorToastMessage($"Oops, You entered <u>invalid quantity</u> {Quantity}. This exceeds the {inventory.Item.Name} stock, of {inventory.Quantity}.  <br/> please review your numbers and try again." +
                            $"");
                        //_toastNotification.AddErrorToastMessage($"Oops, You entered a quantity that exceeds the stock of {frominventory.Item.Name} At {frominventory.Location.Name}, Please enter a valid Quantity that is under {frominventory.Quantity}");
                    }
                    if (Quantity < 0)
                    {
                        OverQuantityFlag = true;
                        _toastNotification.AddErrorToastMessage($"Oops, You cant enter negative numbers... Please Try Again" +
                            $"");
                    }

                }
            }

            if (OverQuantityFlag == false)
            {
                foreach (var item in _context.Items)
                {
                    if (Request.Form.ContainsKey("itemId" + item.ID.ToString()))
                    {
                        //Getting the quantity of the item and location selected
                        int Quantity = int.Parse(Request.Form["itemId" + item.ID.ToString()]);

                        var itemName = _context.Items.FirstOrDefault(i => i.ID == item.ID);
                        //Getting id of location so I can display the name (I dont think I need to siplay name but for testing purposes)
                        int FromlocationID = 0;

                        if (Request.Form.ContainsKey("locationId" + item.ID.ToString()))
                        {
                            if (int.TryParse(Request.Form["locationId" + item.ID.ToString()], out int parsedLocationID))
                            {
                                FromlocationID = parsedLocationID;
                            }
                            else
                            {
                                //This code is for when the user doesnt select anything, the outfilled location does not retrive the value here, so this is a shortcut to get its value.
                                var itemTrans = _context.InventoryTransfers
                                .Where(i => i.ItemId == itemName.ID && i.TransferId == Convert.ToInt32(HttpContext.Session.GetString("TransferIDForEditOverView")))
                                .FirstOrDefault();
                                FromlocationID = itemTrans.FromLocationId;
                            }
                        }

                        //UPDATING THE INVENTORY BEFORE UPDATING THE RESERVATION
                        var OldItemTransfer = _context.InventoryTransfers
                        .Where(i => i.ItemId == itemName.ID && i.TransferId == Convert.ToInt32(HttpContext.Session.GetString("TransferIDForEditOverView")))
                        .FirstOrDefault();
                        if (OldItemTransfer.IsComfirmed == true)
                        {
                            //DO NOTHING IF THE ITEM IS LOGGED
                        }
                        else
                        {
                            //_toastNotification.AddInfoToastMessage($"Old Qty: {oldQty}");

                            // Update the inventory quantity
                            var Oldfrominventory = await _context.Inventories.Include(i => i.Location).Include(i => i.Item)
                                .Where(i => i.ItemID == OldItemTransfer.Item.ID && i.LocationID == OldItemTransfer.FromLocationId)
                                .FirstOrDefaultAsync();


                            var newItemTransfer = await _context.InventoryTransfers.Include(i => i.FromLocation).Include(i => i.Item)
                            .Where(i => i.ItemId == item.ID && i.FromLocationId == FromlocationID && i.TransferId == Convert.ToInt32(HttpContext.Session.GetString("TransferIDForEditOverView")))
                            .FirstOrDefaultAsync();
                            //THIS LOGIC IS FINDING IF THE USER SELECTED A DIFFERENT LOCATION, SO WE WILL ADD THE RESEREVED QTY BACK TO ITS INVENTORY
                            if (newItemTransfer == null)
                            {
                                if (Oldfrominventory != null)
                                {
                                    Oldfrominventory.Quantity += OldItemTransfer.Quantity;
                                    //Oldfrominventory.Quantity -= Convert.ToInt32(Quantity);
                                    //_toastNotification.AddSuccessToastMessage(Oldfrominventory.Quantity.ToString());

                                }

                            }


                            // Update the inventory quantity
                            var frominventory = await _context.Inventories.Include(i => i.Location).Include(i => i.Item)
                                .Where(i => i.ItemID == item.ID && i.LocationID == FromlocationID)
                                .FirstOrDefaultAsync();


                            if (frominventory != null)
                            {
                                if (newItemTransfer != null)
                                {
                                    frominventory.Quantity += OldItemTransfer.Quantity;
                                }
                                
                                frominventory.Quantity -= Quantity;
                                //_toastNotification.AddSuccessToastMessage($"Inventory For {inventory.Item.Name}: {inventory.Quantity}");
                                _context.Update(frominventory);
                                _context.SaveChanges();

                            }
                            ////CREATING THE RECORDS NOW:
                            ///
                            var UpdateItemTrans = _context.InventoryTransfers
                                    .Where(i => i.ItemId == itemName.ID && i.TransferId == Convert.ToInt32(HttpContext.Session.GetString("TransferIDForEditOverView")))
                                    .FirstOrDefault();
                            if (UpdateItemTrans != null)
                            {
                                UpdateItemTrans.Quantity = Quantity;
                                UpdateItemTrans.FromLocationId = FromlocationID;
                                UpdateItemTrans.TransferDate = DateTime.Now;

                                _context.Update(UpdateItemTrans);

                            }
                            else
                            {
                                InventoryTransfer createInvTransfer = new InventoryTransfer();
                                createInvTransfer.TransferId = Convert.ToInt32(HttpContext.Session.GetString("TransferIDForEditOverView"));
                                createInvTransfer.ItemId = item.ID;
                                createInvTransfer.FromLocationId = FromlocationID;
                                createInvTransfer.FromLocationId = FromlocationID;
                                createInvTransfer.Quantity = Quantity;
                                createInvTransfer.TransferDate = DateTime.Now;
                                _context.Add(createInvTransfer);
                            }


                            _context.SaveChanges();
                        }


                    }
                }

                //Means there wasnt any errors and all the create statements were added
                if (flag == false)
                {
                    foreach (var item in _context.Items)
                    {
                        item.isSlectedForEvent = false;
                        _context.Update(item);

                    }
                    _context.SaveChanges();
                    //await _context.SaveChangesAudit();
                    //_toastNotification.AddErrorToastMessage($"{output} EventID: {EventID.ToString()}");
                    //_toastNotification.AddSuccessToastMessage("Item Transfer Created! You can view them all in this index.");
                    //_toastNotification.AddSuccessToastMessage($"{output}");
                    _toastNotification.AddSuccessToastMessage("Transfer Record Updated Successfully");

                    return RedirectToAction("EditOverview", "InventoryTransfers", new { Id = Convert.ToInt32(HttpContext.Session.GetString("TransferIDForEditOverView")) });
                    //return RedirectToAction("ChooseItemQuantitiesEdit", "InventoryTransfers");
                }

            }






            return RedirectToAction("ChooseItemQuantitiesEdit", "InventoryTransfers");
            //return RedirectToAction("ChooseItemQuantities", "InventoryTransfers");
        }

        public ActionResult AddMoreItemsTransfers()
        {
            // Perform any necessary logic here
            if (HttpContext.Session.GetString("SingleTransferBooking") == "True")
            {
                HttpContext.Session.SetString("ClickedAddMultipleTransferInOrderItems", "True");
                HttpContext.Session.SetString("SingleTransferBooking", "False");
            }


            // Perform any necessary logic here
            HttpContext.Session.SetString("ClickedAddMultipleTransferInOrderItems", "True");
            return RedirectToAction("CreateMultipleTransfers", "InventoryTransfers");
        }
        private string ControllerName()
        {
            return this.ControllerContext.RouteData.Values["controller"].ToString();
        }
        private void ViewDataReturnURL()
        {
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, ControllerName());
        }
        private bool InventoryTransferExists(int id)
        {
            return _context.InventoryTransfers.Any(e => e.Id == id);
        }
    }
}
