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
    public class ItemReservationsController : Controller
    {
        private readonly CAAContext _context;
        private readonly IToastNotification _toastNotification;

        public ItemReservationsController(CAAContext context, IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
        }

        public async Task<IActionResult> Index(int?[] EventID, string SearchString, int? SupplierID, int? page, int? pageSizeID
            , string actionButton, string sortDirection = "asc", string sortField = "Event")
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

            //Populating the DropDownLists for the Search/Filtering criteria, which are the Category and Supplier DDL
            ViewData["EventID"] = new SelectList(_context.Events, "ID", "Name");

            // List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "Event", "Item", "Quantity" };

            var itemReservations = _context.ItemReservations
                .Include(i => i.Event)
                .Include(i => i.Item)
                .AsNoTracking();

            //Add as many filters as needed
            if (EventID.Length > 0)
            {
                itemReservations = itemReservations.Where(p => EventID.Contains(p.EventId));
                ViewData["Filtering"] = "btn-danger";
            }

            if (!String.IsNullOrEmpty(SearchString))
            {
                itemReservations = itemReservations.Where(p => p.Item.Name.ToUpper().Contains(SearchString.ToUpper()));
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
            if (sortField == "Quantity")
            {
                if (sortDirection == "asc")
                {
                    itemReservations = itemReservations
                        .OrderBy(i => i.Quantity);
                }
                else
                {
                    itemReservations = itemReservations
                        .OrderByDescending(i => i.Quantity);
                }
            }
            else if (sortField == "Item")
            {
                if (sortDirection == "asc")
                {
                    itemReservations = itemReservations
                        .OrderBy(i => i.Item.Name);
                }
                else
                {
                    itemReservations = itemReservations
                        .OrderByDescending(i => i.Item.Name);
                }
            }
            else //Sorting by Event Name
            {
                if (sortDirection == "asc")
                {
                    itemReservations = itemReservations
                        .OrderBy(i => i.Event.Name);
                }
                else
                {
                    itemReservations = itemReservations
                        .OrderByDescending(i => i.Event.Name);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            //Handle Paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "ItemReservations");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<ItemReservation>.CreateAsync(itemReservations.AsNoTracking(), page ?? 1, pageSize);
            return View(pagedData);
        }

        // GET: ItemReservations/Details/5
        public async Task<IActionResult> Details(int? itemId)
        {
            if (itemId == null)
            {
                return NotFound();
            }

            var itemReservation = await _context.ItemReservations
                .Include(i => i.Event)
                .Include(i => i.Item)
                .FirstOrDefaultAsync(m => m.ItemId == itemId);

            if (itemReservation == null)
            {
                return NotFound();
            }

            ViewBag.LogBackInDate = itemReservation.ReturnDate.HasValue ? itemReservation.ReturnDate.Value.ToString("MM/dd/yyyy hh:mm tt") : "";

            return View(itemReservation);
        }

        // GET: ItemReservations/Create
        public IActionResult Create(string returnUrl, string eventSearchString, string itemSearchString)
        {

            _toastNotification.AddAlertToastMessage($"Please Start By Entering Information Of The Reservation, You Can Cancel By Clicking The Exit Button.");

            IQueryable<Event> events = _context.Events;
            IQueryable<Item> items = _context.Items;

            if (!string.IsNullOrEmpty(eventSearchString))
            {
                events = events.Where(s => s.Name.Contains(eventSearchString));
            }

            if (!string.IsNullOrEmpty(itemSearchString))
            {
                items = items.Where(s => s.Name.Contains(itemSearchString));
            }

            ViewData["EventId"] = new SelectList(events.OrderBy(s => s.Name), "ID", "Name");
            ViewData["ItemId"] = new SelectList(items.OrderBy(s => s.Name), "ID", "Name");

            if (!string.IsNullOrEmpty(Request.Query["itemId"]))
            {
                int itemId;
                if (int.TryParse(Request.Query["itemId"], out itemId) && _context.Items.Any(i => i.ID == itemId))
                {
                    ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name", itemId);
                    ViewBag.ReturnUrl = returnUrl;
                    return View(new ItemReservation { ItemId = itemId });
                }
                else
                {
                    return NotFound();
                }
            }
            else
            {
                //ViewBag.ReturnUrl = returnUrl;
                return View(new ItemReservation());
            }
        }

        // POST: ItemReservations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int itemId, [Bind("Id,EventId,ItemId,Quantity,ReservedDate,ReturnDate")] ItemReservation itemReservation, bool isLogBack, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                // Get the associated Event and Item objects
                itemReservation.Event = await _context.Events.FindAsync(itemReservation.EventId);
                itemReservation.Item = await _context.Items.FindAsync(itemReservation.ItemId);

                // Check if the selected item is available for the event
                bool isItemAvailable = _context.ItemReservations
                    .Where(ir => ir.ItemId == itemReservation.ItemId)
                    .All(ir => ir.EventId != itemReservation.EventId ||
                               itemReservation.ReservedDate >= ir.ReturnDate ||
                               itemReservation.ReturnDate <= ir.ReservedDate);

                // Check if there is enough inventory available for the item
                bool isInventoryAvailable = await _context.Inventories
                    .AnyAsync(inv => inv.ItemID == itemReservation.ItemId &&
                                      inv.Quantity >= itemReservation.Quantity);

                if (isItemAvailable && isInventoryAvailable)
                {
                    _context.Add(itemReservation);
                    await _context.SaveChangesAsync();

                    // Create a new event log entry
                    var eventLog = new EventLog
                    {
                        EventName = itemReservation.Event.Name,
                        ItemName = itemReservation.Item.Name,
                        Quantity = itemReservation.Quantity,
                        LogDate = DateTime.Now,
                        ItemReservation = itemReservation
                    };
                    _context.Add(eventLog);
                    await _context.SaveChangesAsync();

                    // Update the inventory quantity
                    var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ItemID == itemReservation.ItemId);
                    if (inventory != null)
                    {
                        if (isLogBack)
                        {
                            inventory.Quantity += itemReservation.Quantity;
                            itemReservation.LogBackInDate = DateTime.Now;
                        }
                        else
                        {
                            inventory.Quantity -= itemReservation.Quantity;
                        }
                    }

                    await _context.SaveChangesAsync();
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction(nameof(Index), new { ItemID = itemId });
                }
                else if (!isItemAvailable)
                {
                    ModelState.AddModelError("", "The selected item is already reserved for another event during the same time period.");
                }
                else
                {
                    ModelState.AddModelError("", "There is not enough inventory available for the selected item.");
                }
            }

            ViewData["EventId"] = new SelectList(_context.Events, "ID", "Name", itemReservation.EventId);
            ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name", itemReservation.ItemId);
            return View(itemReservation);
        }



        // GET: ItemReservations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var itemReservation = await _context.ItemReservations.FindAsync(id);
            if (itemReservation == null)
            {
                return NotFound();
            }

            ViewData["EventId"] = new SelectList(_context.Events, "ID", "Name", itemReservation.EventId);
            ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name", itemReservation.ItemId);

            // Retrieve the original event log record
            var eventLog = await _context.EventLogs
                .FirstOrDefaultAsync(el => el.ItemReservation.Id == itemReservation.Id);

            // Store the original quantity and dates
            var originalQuantity = eventLog.Quantity;
            var originalReservedDate = itemReservation.ReservedDate;
            var originalReturnDate = itemReservation.ReturnDate;
            var originalLoggedOutDate = itemReservation.LoggedOutDate;

            return View(itemReservation);
        }

        // POST: ItemReservations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Byte[] RowVersion)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            //Go get the ItemReservation to update
            var itemReservationToUpdate = await _context.ItemReservations.Include(ir => ir.Event).Include(ir => ir.Item).FirstOrDefaultAsync(ir => ir.Id == id);

            if (itemReservationToUpdate == null)
            {
                return NotFound();
            }

            //Put the original RowVersion value in the OriginalValues collection for the entity
            _context.Entry(itemReservationToUpdate).Property("RowVersion").OriginalValue = RowVersion;

            //Try updating it with the values posted
            if (await TryUpdateModelAsync<ItemReservation>(itemReservationToUpdate, "",
                ir => ir.EventId, ir => ir.ItemId, ir => ir.Quantity, ir => ir.ReservedDate, ir => ir.ReturnDate, ir => ir.LoggedOutDate))
            {
                try
                {
                    // Check if the selected item is already reserved for another event during the same time period
                    bool isItemAvailable = _context.ItemReservations
                        .Where(ir => ir.ItemId == itemReservationToUpdate.ItemId && ir.Id != itemReservationToUpdate.Id)
                        .All(ir => itemReservationToUpdate.ReservedDate > ir.ReturnDate || itemReservationToUpdate.ReturnDate < ir.ReservedDate);

                    if (isItemAvailable)
                    {
                        // Get the original item reservation
                        var originalItemReservation = await _context.ItemReservations.AsNoTracking().FirstOrDefaultAsync(ir => ir.Id == itemReservationToUpdate.Id);

                        // Get the original event log
                        var originalEventLog = await _context.EventLogs.FirstOrDefaultAsync(el => el.ItemReservation.Id == originalItemReservation.Id);

                        // Update the inventory quantity
                        var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ItemID == itemReservationToUpdate.ItemId);
                        if (inventory != null)
                        {
                            // Check if quantity is being added or subtracted
                            if (itemReservationToUpdate.Quantity > originalItemReservation.Quantity)
                            {
                                inventory.Quantity -= (itemReservationToUpdate.Quantity - originalItemReservation.Quantity);
                            }
                            else if (itemReservationToUpdate.Quantity < originalItemReservation.Quantity)
                            {
                                inventory.Quantity += (originalItemReservation.Quantity - itemReservationToUpdate.Quantity);
                            }

                            // Check if log out date is being updated
                            if (itemReservationToUpdate.LoggedOutDate != originalItemReservation.LoggedOutDate)
                            {
                                inventory.Quantity += originalItemReservation.Quantity;
                            }
                        }

                        await _context.SaveChangesAudit();

                        // Update the event log
                        if (originalEventLog != null)
                        {
                            originalEventLog.EventName = itemReservationToUpdate.Event.Name;
                            originalEventLog.ItemName = itemReservationToUpdate.Item.Name;
                            originalEventLog.Quantity = itemReservationToUpdate.Quantity;
                            originalEventLog.LogDate = DateTime.Now;

                            //_context.Update(originalEventLog);
                            await _context.SaveChangesAudit();
                        }

                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError("", "The selected item is already reserved for another event during the same time period.");
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ItemReservationExists(itemReservationToUpdate.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (DbUpdateException dex)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
            }

            ViewData["EventId"] = new SelectList(_context.Events, "ID", "Name", itemReservationToUpdate.EventId);
            ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name", itemReservationToUpdate.ItemId);
            return View(itemReservationToUpdate);
        }


        // GET: ItemReservations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var itemReservation = await _context.ItemReservations
                .Include(ir => ir.Event)
                .Include(ir => ir.Item)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (itemReservation == null)
            {
                return NotFound();
            }

            return View(itemReservation);
        }

        // POST: ItemReservations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var itemReservation = await _context.ItemReservations.FindAsync(id);
            if (itemReservation == null)
            {
                return NotFound();
            }

            // Mark the ItemReservation as deleted
            itemReservation.IsDeleted = true;

            // Update the inventory quantity
            var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ItemID == itemReservation.ItemId);
            if (inventory != null)
            {
                inventory.Quantity += itemReservation.Quantity;
            }

            await _context.SaveChangesAudit();

            // Check if the total quantity of the item in all reservations
            // is greater than the available quantity in the inventory
            var totalReservedQuantity = await _context.ItemReservations
                .Where(ir => ir.ItemId == itemReservation.ItemId && !ir.IsDeleted)
                .SumAsync(ir => ir.Quantity);

            if (totalReservedQuantity > inventory.Quantity)
            {
                return BadRequest("The total reserved quantity of the item is greater than the available quantity in the inventory.");
            }

            return RedirectToAction("Index", "OrderItems", new { itemId = itemReservation.ItemId });

        }

        //For Adding Event
        [HttpGet]
        public JsonResult GetEvents(int? id)
        {
            return Json(EventSelectList(id));
        }
        //For Adding Event
        private SelectList EventSelectList(int? selectedId)
        {
            return new SelectList(_context
                .Events
                .OrderBy(c => c.Name), "ID", "Name", selectedId);
        }

        // GET: ItemReservations/LogBackIn/5
        public async Task<IActionResult> LogBackIn(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Get all reservations for the item associated with the specified ID
            var itemReservations = await _context.ItemReservations
                .Include(ir => ir.Event)
                .Include(ir => ir.Item)
                .Where(ir => ir.ItemId == id && !ir.IsDeleted)
                .ToListAsync();

            if (itemReservations == null || itemReservations.Count == 0)
            {
                return NotFound();
            }

            return View(itemReservations);
        }

        private string ControllerName()
        {
            return this.ControllerContext.RouteData.Values["controller"].ToString();
        }
        private void ViewDataReturnURL()
        {
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, ControllerName());
        }

        private bool ItemReservationExists(int id)
        {
            return _context.ItemReservations.Any(e => e.Id == id);
        }
    }
}
