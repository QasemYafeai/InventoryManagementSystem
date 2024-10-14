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
using NToastNotify;
using CAAMarketing.ViewModels;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using static iTextSharp.text.pdf.AcroFields;

namespace CAAMarketing.Controllers
{
    public class EventsController : Controller
    {
        private readonly CAAContext _context;
        private readonly IToastNotification _toastNotification;

        public EventsController(CAAContext context, IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
        }

        // GET: Events
        public async Task<IActionResult> Index(string SearchString, int? LocationID, int? page, int? pageSizeID
            , string actionButton, string sortDirection = "asc", string sortField = "Event")
        {

            //int[] getItemResFromMethod = HasReservationStarted();
            //string message = "";
            //if (getItemResFromMethod != null && getItemResFromMethod.Length > 0)
            //{
            //    // Do something with the IDs in the array
            //    foreach (int itemresID in getItemResFromMethod)
            //    {
            //        // Show message for each item reservation that had its inventory deducted
            //        message += $"Inventory has been deducted for Item Reservation ID {itemresID}. ";
            //    }
            //}
            //else
            //{
            //    // Show message if no item reservations had their inventory deducted
            //    message = "No item reservations have had their inventory deducted.";
            //}

            // Show toast notification with message
            //_toastNotification.AddSuccessToastMessage(message);


            ViewDataReturnURL();

            if (HttpContext.Session.GetString("TypeOfOperationReservations") == "EditedItems")
            {
                HttpContext.Session.SetString("TypeOfOperationReservations", "");
            }


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




            // List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "Event", "Date", "Location", "Reserve Date", "Return Date", "# Booked", "Logged", "Items" };


            var events = _context.Events
                .Include(i => i.ItemReservations).ThenInclude(i => i.Item)
                .Include(i => i.ItemReservations).ThenInclude(i => i.Location)
                .AsNoTracking();

            if (!String.IsNullOrEmpty(SearchString))
            {
                events = events.Where(p => p.Name.ToUpper().Contains(SearchString.ToUpper()));
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

            //Now we know which field and direction to sort by
            if (sortField == "Location")
            {
                if (sortDirection == "asc")
                {
                    events = events
                        .OrderBy(p => p.location);
                }
                else
                {
                    events = events
                        .OrderByDescending(p => p.location);
                }
            }
            else if (sortField == "Items")
            {
                if (sortDirection == "asc")
                {
                    events = events
                        .OrderBy(p => p.ItemReservations.FirstOrDefault().Item.Name);
                }
                else
                {
                    events = events
                        .OrderByDescending(p => p.ItemReservations.FirstOrDefault().Item.Name);
                }
            }
            else if (sortField == "Logged")
            {
                if (sortDirection == "asc")
                {
                    events = events
                        .OrderBy(p => p.ItemReservations.FirstOrDefault().IsLoggedIn);
                }
                else
                {
                    events = events
                        .OrderByDescending(p => p.ItemReservations.FirstOrDefault().IsLoggedIn);
                }
            }
            else if (sortField == "# Booked")
            {
                if (sortDirection == "asc")
                {
                    events = events
                        .OrderBy(p => p.ItemReservations.FirstOrDefault().Quantity);
                }
                else
                {
                    events = events
                        .OrderByDescending(p => p.ItemReservations.FirstOrDefault().Quantity);
                }
            }
            else if (sortField == "Return Date")
            {
                if (sortDirection == "asc")
                {
                    events = events
                        .OrderBy(p => p.ReturnEventDate);
                }
                else
                {
                    events = events
                        .OrderByDescending(p => p.ReturnEventDate);
                }
            }
            else if (sortField == "Reserve Date")
            {
                if (sortDirection == "asc")
                {
                    events = events
                        .OrderBy(p => p.ReservedEventDate);
                }
                else
                {
                    events = events
                        .OrderByDescending(p => p.ReservedEventDate);
                }
            }
            else //Sorting by Patient Name
            {
                if (sortDirection == "asc")
                {
                    events = events
                        .OrderBy(p => p.Name);
                }
                else
                {
                    events = events
                        .OrderByDescending(p => p.Name);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            //Handle Paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "Events");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<Event>.CreateAsync(events.AsNoTracking(), page ?? 1, pageSize);
            return View(pagedData);
        }

        // GET: Events/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Events == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
                .FirstOrDefaultAsync(m => m.ID == id);
            if (@event == null)
            {
                return NotFound();
            }

            // Get the list of items for the event
            var items = await _context.ItemReservations
                .Include(ir => ir.Item)
                .Include(ir => ir.Location)
                .Where(ir => ir.EventId == id)
                .ToListAsync();

            ViewData["Items"] = items;

            return View(@event);
        }

        // GET: Events/Create
        public IActionResult Create()
        {
            _toastNotification.AddAlertToastMessage($"Please Start By Entering Information Of The Event, You Can Cancel By Clicking The Exit Button.");

            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            return View();
        }

        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Create(Event model)
        {
            if (ModelState.IsValid)
            {
                if (model.ReservedEventDate > model.ReturnEventDate)
                {
                    _toastNotification.AddErrorToastMessage("Sorry, the return date cannot be before the reserved date, review your changes and try again...");
                    return View();
                }
                else
                {
                    // Add the new event to the database
                    _context.Events.Add(model);
                }
                await _context.SaveChangesAudit();



                HttpContext.Session.SetInt32("EventID", model.ID);

                foreach (var item in _context.Items)
                {
                    item.isSlectedForEvent = false;
                    _context.Update(item);
                    _context.SaveChanges();
                }



                return RedirectToAction("SelectItems", "Events", new { id = model.ID });
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

        // GET: Events/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Events == null)
            {
                return NotFound();
            }

            var @event = await _context.Events.FindAsync(id);
            if (@event == null)
            {
                return NotFound();
            }
            return View(@event);
        }

        // POST: Events/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Byte[] RowVersion)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            //Go get the Event to update
            var eventToUpdate = await _context.Events.FirstOrDefaultAsync(e => e.ID == id);

            var beforeEventDate = eventToUpdate.ReservedEventDate;

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
                var checkNoRes = _context.ItemReservations.Where(i => i.EventId == eventToUpdate.ID).Count();
                bool overallflag = false;
                try
                {
                    if (eventToUpdate.ReservedEventDate > eventToUpdate.ReturnEventDate)
                    {
                        _toastNotification.AddErrorToastMessage("Sorry, the return date cannot be before the reserved date, review your changes and try again...");
                        overallflag = true;
                    }
                    else if (checkNoRes <= 0)
                    {

                    }
                    else if (overallflag != true)
                    {
                        if (beforeEventDate == eventToUpdate.ReservedEventDate) {  }
                        else if (beforeEventDate > DateTime.Today && eventToUpdate.ReservedEventDate <= DateTime.Today)
                        {
                            bool flag = false;
                            string flagmessage = "Oops, OverBooking Error: Some items cant be deducted from inventory as other events reserved them: <br/>";
                            var itemReservs = _context.ItemReservations
                            .Where(i => i.EventId == eventToUpdate.ID)
                            .ToList();

                            //CHECKING ALL THE RESERVATIONS IF THEY SUCCEED BEFORE UPDATING THE INVENTORY
                            foreach (var itemRes in itemReservs)
                            {
                                var inv = _context.Inventories
                                    .Where(i => i.ItemID == itemRes.ItemId && i.LocationID == itemRes.LocationID)
                                    .FirstOrDefault();
                                if (inv != null && (inv.Quantity - itemRes.Quantity) < 0)
                                {
                                    flag = true;
                                    overallflag = true;
                                    flagmessage += $"Event: {itemRes.Event}, <u>{itemRes.Item}</u> at {itemRes.Location}, Qty: {itemRes.Quantity}.";
                                    //_toastNotification.AddSuccessToastMessage("All item quantities have been deducted from inventory. Have fun at the event!");
                                }
                            }
                            if (flag == true)
                            {
                            }
                            if (flag == false)
                            {
                                foreach (var itemRes in itemReservs)
                                {
                                    var inv = _context.Inventories
                                        .Where(i => i.ItemID == itemRes.ItemId && i.LocationID == itemRes.LocationID)
                                        .FirstOrDefault();
                                    if (inv != null)
                                    {
                                        inv.Quantity -= itemRes.Quantity;
                                        itemRes.IsInventoryDeducted = true;
                                        //_toastNotification.AddSuccessToastMessage("All item quantities have been deducted from inventory. Have fun at the event!");
                                    }
                                }
                            }


                        }
                        else if (beforeEventDate <= DateTime.Today && eventToUpdate.ReservedEventDate > DateTime.Today)
                        {
                            bool flag = false;
                            bool OverBookingFlag = false;
                            var itemReservs = _context.ItemReservations
                            .Where(i => i.EventId == eventToUpdate.ID)
                            .ToList();


                            //GETTING THE ITEMS THAT ARE IN THE RESERVATIONS AND CHANGING THE BOOL TO TRUE FOR OUTPUTTING THE SELECTEDITEMS TO THE PAGE
                            var itemResOverBookingCaution = _context.ItemReservations
                                .Include(i => i.Item)
                                .Include(i => i.Event)
                                .Where(i =>
                                    (i.Event.ReservedEventDate >= eventToUpdate.ReservedEventDate && i.Event.ReservedEventDate < eventToUpdate.ReturnEventDate) ||
                                    (i.Event.ReservedEventDate > eventToUpdate.ReservedEventDate && i.Event.ReturnEventDate <= eventToUpdate.ReturnEventDate) ||
                                    (i.Event.ReservedEventDate == eventToUpdate.ReservedEventDate && i.Event.ReturnEventDate == eventToUpdate.ReturnEventDate) ||
                                    (i.Event.ReservedEventDate < eventToUpdate.ReservedEventDate && i.Event.ReturnEventDate > eventToUpdate.ReturnEventDate))
                                .ToList();

                            List<ItemReservation> itemresCheck = new List<ItemReservation>();
                            int potentialItemQuantity = 0;
                           
                            foreach (var res in itemResOverBookingCaution)
                            {
                                var inventory = _context.Inventories
                                    .Where(i => i.ItemID == res.ItemId && i.LocationID == res.LocationID).Include(i => i.Location).Include(i => i.Item)
                                    .FirstOrDefault();
                                if (res.ItemId == inventory.ItemID && res.LocationID == inventory.LocationID && res.EventId != eventToUpdate.ID)
                                {
                                    potentialItemQuantity += res.Quantity;
                                }
                            }
                            //_toastNotification.AddErrorToastMessage($"Potential Item Qty: {potentialItemQuantity.ToString()}");
                            foreach (var resOverBook in itemResOverBookingCaution)
                            {
                                var inventory = _context.Inventories
                                    .Where(i => i.ItemID == resOverBook.ItemId && i.LocationID == resOverBook.LocationID).Include(i => i.Location).Include(i => i.Item)
                                    .FirstOrDefault();
                                if (resOverBook.ItemId == inventory.ItemID && resOverBook.LocationID == inventory.LocationID && resOverBook.EventId != eventToUpdate.ID)
                                {
                                    var itemres = _context.ItemReservations
                                    .Include(ir => ir.Event)
                                    .Where(ir => ir.ItemId == inventory.ItemID && ir.IsLoggedIn == false && ir.Event.ReservedEventDate > DateTime.Today)
                                    .Select(ir => ir.Quantity)
                                    .Sum();

                                    _toastNotification.AddInfoToastMessage($"Item sum: {inventory.Item.Name}, {itemres}");
                                    if ((itemres) > (inventory.Quantity + itemres))
                                    {
                                        flag = true;
                                        itemresCheck.Add(resOverBook);
                                    }
                                }
                            }
                            if (flag == true)
                            {
                                OverBookingFlag = true;
                                overallflag = true;
                                string output2 = "OverBooking Error: Other events reserved: <br/> ";
                                foreach (var outputOverBook in itemresCheck)
                                {
                                    var itemres = _context.ItemReservations
                                    .Include(ir => ir.Event)
                                    .Where(ir => ir.ItemId == outputOverBook.ItemId && ir.IsLoggedIn == false && ir.Event.ReservedEventDate > DateTime.Today)
                                    .Select(ir => ir.Quantity)
                                    .Sum();
                                    var inv = _context.Inventories.Where(i => i.ItemID == outputOverBook.ItemId && i.LocationID == outputOverBook.LocationID).FirstOrDefault();
                                    int qty = (inv.Quantity + itemres) - itemres;
                                    output2 += $"<u>{outputOverBook.Event.Name} - {outputOverBook.Item.Name}</u> ({qty} available at {outputOverBook.Location.Name}) <br/>";
                                }
                                _toastNotification.AddErrorToastMessage(output2);
                            }

                            string outputsuccess = "The following items have been added back to inventory:<br/>";
                            if (OverBookingFlag == false)
                            {
                                foreach (var itemRes in itemReservs)
                                {
                                    var inv = _context.Inventories
                                        .Where(i => i.ItemID == itemRes.ItemId && i.LocationID == itemRes.LocationID).Include(i => i.Location).Include(i => i.Item)
                                        .FirstOrDefault();
                                    if (inv != null)
                                    {
                                        inv.Quantity += itemRes.Quantity;
                                        itemRes.IsInventoryDeducted = false;

                                        outputsuccess += $"<u>{inv.Item.Name}</u> in {inv.Location.Name} - Qty {itemRes.Quantity}<br/>";
                                        //_toastNotification.AddSuccessToastMessage("All item quantities have been added back from inventory.");
                                    }
                                }
                                _toastNotification.AddSuccessToastMessage(outputsuccess);
                            }
                        }
                    }
                    if (overallflag != true)
                    {
                        _toastNotification.AddSuccessToastMessage("Event Record Editted Succesfully");
                        await _context.SaveChangesAudit();
                    }
                    //return RedirectToAction(nameof(Index));

                    return RedirectToAction("EditOverview", new { eventToUpdate.ID });

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(eventToUpdate.ID))
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

        // GET: Events/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Events == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
                .FirstOrDefaultAsync(m => m.ID == id);
            if (@event == null)
            {
                return NotFound();
            }

            // Get the list of items for the event
            var items = await _context.ItemReservations
                .Include(ir => ir.Item)
                .Include(ir => ir.Location)
                .Where(ir => ir.EventId == id)
                .ToListAsync();

            ViewData["Items"] = items;

            //Return event item quantity back to inventory quantity
            //var inventory = await _context.Inventories.Include(i => i.Location).Include(i => i.Item)
            //    .Where(i => i.Id == items.FirstOrDefault().ItemId)
            //    .FirstOrDefaultAsync();
            //inventory.Quantity += Convert.ToInt32(items.FirstOrDefault().Event.ItemReservations.FirstOrDefault().Quantity);
            //_context.SaveChangesAudit();

            return View(@event);
        }

        // POST: Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            string messageOutput = "";
            bool showmessage = false;
            if (_context.Events == null)
            {
                return Problem("Entity set 'CAAContext.Events'  is null.");
            }
            var @event = await _context.Events.FindAsync(id);
            if (@event != null)
            {
                // Get the list of item reservations for the event
                var itemReservations = await _context.ItemReservations
                    .Include(ir => ir.Item)
                    .Include(ir => ir.Location)
                    .Where(ir => ir.EventId == id)
                    .ToListAsync();
                var trans = await _context.Events
                    .Where(i => i.ID == id)
                    .FirstOrDefaultAsync();
                
                // For each item reservation, update the inventory and delete the item reservation
                foreach (var itemReservation in itemReservations)
                {
                    if (trans.ReservedEventDate <= DateTime.Today)
                    {
                        // Update the inventory quantity
                        var inventory = await _context.Inventories
                            .Where(i => i.ItemID == itemReservation.ItemId && i.LocationID == itemReservation.LocationID)
                            .FirstOrDefaultAsync();

                        if (inventory != null)
                        {
                            inventory.Quantity += itemReservation.Quantity;
                            //_context.Update(inventory);
                        }

                        showmessage = true;
                    }
                    var eventlogs = _context.EventLogs
                        .Where(i => i.ItemReservationId == itemReservation.Id)
                        .ToList();

                    foreach (var eventlog in eventlogs)
                    {

                        _context.EventLogs.Remove(eventlog);
                        await _context.SaveChangesAudit();
                    }
                    // Remove the item reservation from the database
                    _context.ItemReservations.Remove(itemReservation);


                }

                if(showmessage == true)
                {
                    messageOutput += "All Reserved Quantities Are Added Back To Inventory";
                }

                // Save changes to the inventory and item reservations
                await _context.SaveChangesAudit();


                // Remove the event from the database
                _context.Events.Remove(@event);
                await _context.SaveChangesAudit();

                _toastNotification.AddSuccessToastMessage($"Event Deleted. {messageOutput}");
            }

            return RedirectToAction(nameof(Index));
        }




        // GET: SelectItems
        public async Task<IActionResult> SelectItems(int? EventID, int? CategoryID, string SearchString, int? page, int? pageSizeID
            , string actionButton, string sortDirection = "asc", string sortField = "Item")
        {
            int? EventID1 = HttpContext.Session.GetInt32("EventID") ?? 1; //default;
            // Perform any necessary logic here
            if (HttpContext.Session.GetString("ClickedAddMultipleInOrderItems") == "True")
            {
                var singleitem = await _context.Items
                    .Where(i => i.ID == Convert.ToInt32(HttpContext.Session.GetString("ItemIDFromSingleEventBooking")))
                    .FirstOrDefaultAsync();

                singleitem.isSlectedForEvent = true;
                _context.Update(singleitem);
                _context.SaveChanges();
                HttpContext.Session.SetString("ClickedAddMultipleInOrderItems", "False");
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

            var Event = _context.Events
                .Where(i => i.ID == EventID1)
                .FirstOrDefault();

            var ReservedDate = Event.ReservedEventDate;
            var ReturnDate = Event.ReturnEventDate;


            var items = _context.Items
                .Include(i => i.Supplier)
                .Include(i => i.Category)
                .Include(i => i.Inventories)
                .Include(i => i.ItemImages)
                .Include(i => i.ItemThumbNail)
                .Include(i => i.Inventories).ThenInclude(i => i.Location)
                .Where(i => i.Archived == false)
                .AsNoTracking();


            //GETTING THE ITEMS THAT ARE IN THE RESERVATIONS AND CHANGING THE BOOL TO TRUE FOR OUTPUTTING THE SELECTEDITEMS TO THE PAGE
            var itemResOverBookingCaution = _context.ItemReservations.Include(i => i.Item).Include(i => i.Event)
                .Where(i => i.Event.ReservedEventDate >= ReservedDate && i.Event.ReturnEventDate <= ReturnDate)
                .ToList();


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

            _toastNotification.AddInfoToastMessage("Please Select The Items That Are Needed For This Booking");
            var SelectedItems = await _context.Items.Include(i => i.Supplier).Include(i => i.Category).Include(i => i.ItemImages).Include(i => i.ItemThumbNail)
            .ToListAsync();

            ViewBag.SelectedItems = SelectedItems;
            ViewBag.itemResOverBookingCaution = itemResOverBookingCaution;
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

                return RedirectToAction("SelectItems", "Events");
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
                //ItemReservation createItemReserv = new ItemReservation();
                //createItemReserv.ItemId = ItemID;
                //createItemReserv.EventId = 1;
                //createItemReserv.LocationID = 1;
                //createItemReserv.Quantity = 0;
                //createItemReserv.LoggedOutDate = DateTime.Now;
                //createItemReserv.ReturnDate = DateTime.Now;
                //createItemReserv.ReservedDate = DateTime.Now;
                //_context.Add(createItemReserv);



                var itemsupdate = _context.Items.Include(i => i.ItemImages).Include(i => i.ItemThumbNail).Where(i => i.ID == ItemID).FirstOrDefault();

                itemsupdate.isSlectedForEvent = false;
                _context.Update(itemsupdate);
                _context.SaveChanges();

                //_context.SaveChanges();

                return RedirectToAction("SelectItems", "Events");
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
                .Include(i => i.ItemImages)
                .Include(i => i.ItemThumbNail)
                .Include(i => i.Inventories).ThenInclude(i => i.Location)
                .Where(i => i.isSlectedForEvent == true)
            .ToListAsync();
            int? EventID = HttpContext.Session.GetInt32("EventID") ?? default;


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
            int EventID = HttpContext.Session.GetInt32("EventID") ?? default;
            var EventDate = _context.Events
                .Where(i => i.ID == EventID)
                .FirstOrDefault();

            var ReserveEventDate = EventDate.ReservedEventDate;


            bool OverQuantityFlag = false;
            bool OverBookingFlag = false;




            //FOR THE OVERQUANTITYFLAG
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
            if (ReserveEventDate > DateTime.Today)
            {
                //FOR THE OVERBOOKINGFLAG
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

                        //MAKING SURE THE QUANTITES OF THE INVENTORIES FOR THE EVENT DONT OVERLAP THE QUANITIES OF OTHER EVENTS AND PREVENT OVERBOOKING

                        var Event = _context.Events
                            .Where(i => i.ID == EventID)
                            .FirstOrDefault();

                        var ReservedDate = Event.ReservedEventDate;
                        var ReturnDate = Event.ReturnEventDate;

                        //GETTING THE ITEMS THAT ARE IN THE RESERVATIONS AND CHANGING THE BOOL TO TRUE FOR OUTPUTTING THE SELECTEDITEMS TO THE PAGE
                        var itemResOverBookingCaution = _context.ItemReservations.Include(i => i.Item).Include(i => i.Event)
                            .Where(i => i.Event.ReservedEventDate >= ReservedDate && i.Event.ReturnEventDate <= ReturnDate)
                            .Where(i => i.Event.ReservedEventDate <= ReturnDate && i.Event.ReturnEventDate >= ReservedDate)
                            .ToList();
                        bool flag = false;
                        List<ItemReservation> itemres = new List<ItemReservation>();
                        int potentialItemQuantity = 0;
                        foreach (var res in itemResOverBookingCaution)
                        {
                            if (res.ItemId == inventory.ItemID && res.LocationID == inventory.LocationID)
                            {
                                potentialItemQuantity += res.Quantity;
                            }
                        }
                        var res1 = _context.ItemReservations
                            .Include(ir => ir.Event)
                            .Where(ir => ir.ItemId == inventory.ItemID && ir.IsLoggedIn == false && ir.Event.ReservedEventDate > DateTime.Today)
                            .Select(ir => ir.Quantity)
                            .Sum();
                        foreach (var resOverBook in itemResOverBookingCaution)
                        {
                            if (resOverBook.ItemId == inventory.ItemID && resOverBook.LocationID == inventory.LocationID)
                            {

                                //_toastNotification.AddInfoToastMessage($"Res: {res1} Your Qty: {Quantity}");
                                if ((res1 + Quantity) > inventory.Quantity)
                                {
                                    flag = true;
                                    itemres.Add(resOverBook);
                                }
                            }
                        }
                        if (flag == true)
                        {
                            OverBookingFlag = true;
                            string output2 = "OverBooking Error: Other events reserved: <br/> ";
                            foreach (var outputOverBook in itemres)
                            {
                                var inv = _context.Inventories.Where(i => i.ItemID == outputOverBook.ItemId && i.LocationID == outputOverBook.LocationID).FirstOrDefault();
                                int qty = inv.Quantity - potentialItemQuantity;
                                if (qty == 0)
                                {
                                    output2 += $"<u>{outputOverBook.Event.Name} - {outputOverBook.Item.Name}</u> ({qty} available at {outputOverBook.Location.Name}). Please Go Back And Select A Different Item <br/>";
                                }
                                else
                                {
                                    output2 += $"<u>{outputOverBook.Event.Name} - {outputOverBook.Item.Name}</u> ({qty} available at {outputOverBook.Location.Name}) <br/>";
                                }

                            }
                            _toastNotification.AddErrorToastMessage(output2);
                        }
                    }
                }
            }


            if (OverQuantityFlag == false && OverBookingFlag == false)
            {
                await _context.SaveChangesAudit();


                foreach (var item in _context.Items)
                {
                    if (Request.Form.ContainsKey("itemId" + item.ID.ToString()))
                    {



                        //Getting the quantity of the item and location selected
                        int Quantity = int.Parse(Request.Form["itemId" + item.ID.ToString()]);

                        //Getting id of location so I can display the name (I dont think I need to siplay name but for testing purposes)
                        int locationID = int.Parse(Request.Form["locationId" + item.ID.ToString()]);
                        //Getting the Name of the location they selected by id
                        var location = _context.Locations
                            .Where(i => i.Id == locationID)
                            .FirstOrDefault();

                        //Outputted a message to see if my logic worked, and It Did!
                        output += "Name: " + item.Name.ToString() + ", Location: " + location.Name + ", Qty: " + Quantity + "\n";

                        var events = _context.Events
                            .Where(i => i.ID == EventID)
                            .FirstOrDefault();

                        // Update the inventory quantity
                        var inventory = await _context.Inventories.Include(i => i.Location).Include(i => i.Item)
                            .Where(i => i.ItemID == item.ID && i.LocationID == locationID)
                            .FirstOrDefaultAsync();


                        var eventResDate = _context.Events
                            .Where(i => i.ID == EventID)
                            .FirstOrDefault();





                        //CREATING THE RECORDS NOW:
                        ItemReservation createItemReserv = new ItemReservation();
                        createItemReserv.ItemId = item.ID;
                        createItemReserv.EventId = EventID;
                        createItemReserv.LocationID = locationID;
                        createItemReserv.Quantity = Quantity;
                        createItemReserv.LoggedOutDate = DateTime.Now;



                        if (inventory != null && eventResDate.ReservedEventDate <= DateTime.Now)
                        {
                            inventory.Quantity -= Quantity;
                            createItemReserv.IsInventoryDeducted = true;
                            _context.Update(inventory);
                        }
                        _context.Add(createItemReserv);
                        _context.SaveChanges();


                        if (createItemReserv != null)
                        {
                            // Create a new event log entry
                            var eventLog = new EventLog
                            {
                                EventName = createItemReserv.Event.Name,
                                ItemName = createItemReserv.Item.Name,
                                Quantity = createItemReserv.Quantity,
                                LogDate = DateTime.Now,
                                ItemReservation = createItemReserv
                            };
                            _context.Add(eventLog);
                        }
                        _context.SaveChanges();
                    }
                }
                foreach (var item in _context.Items)
                {
                    item.isSlectedForEvent = false;
                    _context.Update(item);

                }
                _context.SaveChanges();

                //_toastNotification.AddErrorToastMessage($"{output} EventID: {EventID.ToString()}");
                _toastNotification.AddSuccessToastMessage("Item Bookings Created! You can view them all in this index.");
                return RedirectToAction("Index", "Events");
            }

            return RedirectToAction("ChooseItemQuantities", "Events");
        }


        public async Task<IActionResult> LogBackInMultiple(int? id)
        {

            ViewDataReturnURL();


            var eventDetails = await _context.Events
                .Include(e => e.ItemReservations).ThenInclude(i => i.Item)
                .Include(e => e.ItemReservations).ThenInclude(i => i.Location)
                .Include(i => i.ItemReservations).ThenInclude(i => i.Item).ThenInclude(i => i.Inventories)
                .Include(i => i.ItemReservations).ThenInclude(i => i.Item).ThenInclude(i => i.Inventories).ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(m => m.ID == id);


            return View(eventDetails.ItemReservations);
        }


        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> LogBackInMultiple(int id, IFormCollection form, string missingReportBool, string Notes, string Reason)
        {
            string itemIDCheck = form["formId"]; // Get the form ID
            string QuantityCheck = form[$"itemId{itemIDCheck}"].ToString(); // Get the item ID
            int EventIDCheck = id;
            if (missingReportBool == "True")
            {

                // Get the value of MySessionVariable from the session state
                int itemReservID = Convert.ToInt32(HttpContext.Session.GetString("ItemReservationForLogMethod"));

                // Get the value of MySessionVariable from the session state
                int inventoryID = Convert.ToInt32(HttpContext.Session.GetString("InventoryForLogMethod"));

                // Get the value of MySessionVariable from the session state
                int userQuantity = Convert.ToInt32(HttpContext.Session.GetString("UserQuantityForLogMethod"));

                // Get the value of MySessionVariable from the session state
                int itemQtyVariance = Convert.ToInt32(HttpContext.Session.GetString("ItemQtyVarianceForLogMethod"));

                //Go get the ItemReservation to update
                var itemReservationToRemove = await _context.ItemReservations.Include(ir => ir.Event).Include(ir => ir.Item).Include(i => i.Location)
                    .Where(i => i.Id == itemReservID)
                    .FirstOrDefaultAsync();
                // Update the inventory quantity
                var inventory = await _context.Inventories.Include(i => i.Location).Include(i => i.Item)
                    .Where(i => i.Id == inventoryID)
                    .FirstOrDefaultAsync();

                //_toastNotification.AddSuccessToastMessage($"Notes: {Notes}, Reason: {Reason}, Item: {itemReservationToRemove.Item.Name}, Qty: {itemReservationToRemove.Quantity}, UserQty: {userQuantity}, Inv Record: {inventory.Item.Name} - {inventory.Location.Name}");

                //FOR GETTING THE EMPLOYEE THAT LOGGED THIS AND CAUSED A QUANTITY VARIANCE
                var email = User.Identity.Name;
                var employee = _context.Employees.FirstOrDefault(e => e.Email == email);

                //MAKING A MISSINGITEMLOG RECORD FOR THE VARIANCE OF THIS ITEM
                var addMissingItemLog = new MissingItemLog();
                addMissingItemLog.Notes = Notes;
                addMissingItemLog.Reason = Reason;
                addMissingItemLog.Quantity = itemQtyVariance;
                addMissingItemLog.ItemId = itemReservationToRemove.ItemId;
                addMissingItemLog.EventId = itemReservationToRemove.EventId;
                addMissingItemLog.LocationID = itemReservationToRemove.LocationID;
                addMissingItemLog.EmployeeID = employee.ID;
                addMissingItemLog.Date = DateTime.Now;

                //SINCE WE ARE GOING TO LOGGED THIS ITEM BACK IN AND ADD THE QUANTITY, I WILL ASSIGN THE BOOL TO TRUE AS IF TECHNICALLY DELETED AND THE USER WILL SEE ITS LOGGED
                itemReservationToRemove.IsLoggedIn = true;
                itemReservationToRemove.LoggedInQuantity = userQuantity;
                inventory.Quantity += Convert.ToInt32(userQuantity); //ADDING THE INVENTORY QUANTITY BACK FOR ITS ASSIGNED LOCATION

                //ADDING EVERYTHING IN THE DATABASE.
                _context.Add(addMissingItemLog);
                //_context.Update(inventory);
                //_context.Update(itemReservationToRemove);
                await _context.SaveChangesAudit();


            }
            else if (Convert.ToInt32(QuantityCheck) < 0)
            {
                _toastNotification.AddErrorToastMessage("Oops, You cant input negative values, review your selections and try again");
                return RedirectToAction("LogBackInMultiple", "Events", id);
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




                //Go get the ItemReservation to update
                var itemReservationToRemove = await _context.ItemReservations.Include(ir => ir.Event).Include(ir => ir.Item).Include(i => i.Location)
                    .Where(i => i.ItemId == Convert.ToInt32(itemID) && i.LocationID == location.Id && i.EventId == EventID && i.IsLoggedIn == false)
                    .FirstOrDefaultAsync();
                // Update the inventory quantity
                var inventory = await _context.Inventories.Include(i => i.Location).Include(i => i.Item)
                    .Where(i => i.ItemID == item.ID && i.LocationID == location.Id)
                    .FirstOrDefaultAsync();


                if (itemReservationToRemove == null)
                {
                    _toastNotification.AddErrorToastMessage($"Reservation Not Found");
                }
                else if (inventory == null)
                {
                    _toastNotification.AddErrorToastMessage($"Inventory Record Not Found");
                }
                else
                {
                    if (itemReservationToRemove.Quantity < Convert.ToInt32(Quantity))
                    {
                        _toastNotification.AddErrorToastMessage($"Oops, you entered a quantity that is more than what you logged in. Please enter the correct amount.");
                    }
                    //THIS METHOD IS TO POP UP THE MODEL FOR A DISCREPANCY LOG, TOOK A WHILE BUT MADE IT WORK.
                    else if (itemReservationToRemove.Quantity != Convert.ToInt32(Quantity))
                    {
                        TempData["InitateMissingItemLog"] = "Data received";

                        var missingamount = itemReservationToRemove.Quantity - Convert.ToInt32(Quantity);
                        _toastNotification.AddErrorToastMessage($"{itemReservationToRemove.Quantity} {itemReservationToRemove.Item.Name}'s were logged out, you only logged in {Quantity}. Please specify the reason why <u>{missingamount}</u> of {itemReservationToRemove.Item.Name}'s aren't logged.");


                        // Get the value of MySessionVariable from the session state
                        HttpContext.Session.SetString("ItemReservationForLogMethod", itemReservationToRemove.Id.ToString());

                        // Get the value of MySessionVariable from the session state
                        HttpContext.Session.SetString("InventoryForLogMethod", inventory.Id.ToString());

                        // Get the value of MySessionVariable from the session state
                        HttpContext.Session.SetString("UserQuantityForLogMethod", Quantity.ToString());

                        // Get the value of MySessionVariable from the session state
                        HttpContext.Session.SetString("ItemQtyVarianceForLogMethod", missingamount.ToString());

                    }

                    else
                    {
                        itemReservationToRemove.IsLoggedIn = true;
                        itemReservationToRemove.LoggedInQuantity = Convert.ToInt32(Quantity);
                        inventory.Quantity += Convert.ToInt32(Quantity);
                        // _toastNotification.AddErrorToastMessage($"{inventory.Item.Name}, {inventory.Location.Name}");
                        //_context.Update(inventory);
                        //_context.Update(itemReservationToRemove);
                        await _context.SaveChangesAudit();
                    }

                }

            }

            return RedirectToAction("LogBackInMultiple", "Events", id);
        }

        // GET: SelectItems
        public async Task<IActionResult> EditOverview(int? Id)
        {
            HttpContext.Session.SetString("EventIDForEditOverView", Id.ToString());

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

            var events = await _context.Events
                .FindAsync(Id);

            if (events == null)
            {
                return NotFound();
            }

            var eventReservations = await _context.Events
               .Include(e => e.ItemReservations).ThenInclude(i => i.Item)
               .Include(e => e.ItemReservations).ThenInclude(i => i.Location)
               .Include(i => i.ItemReservations).ThenInclude(i => i.Item).ThenInclude(i => i.Inventories)
               .Include(i => i.ItemReservations).ThenInclude(i => i.Item).ThenInclude(i => i.Inventories).ThenInclude(i => i.Location)
               .FirstOrDefaultAsync(m => m.ID == Id);

            //Since the user clicked on the items and modified it, it will take affect.
            if (HttpContext.Session.GetString("TypeOfOperationReservations") == "EditedItems")
            {

                //GETTING THE ITEMS THAT ARE IN THE RESERVATIONS AND CHANGING THE BOOL TO TRUE FOR OUTPUTTING THE SELECTEDITEMS TO THE PAGE
                var itemReservations = _context.ItemReservations.Include(i => i.Item).Include(i => i.Event)
                    .Where(i => i.EventId == Convert.ToInt32(HttpContext.Session.GetString("EventIDForEditOverView")))
                    .ToList();
                foreach (var itemRes in itemReservations)
                {
                    foreach (var item in _context.Items)
                    {
                        if (itemRes.Item.ID == item.ID)
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




                ViewBag.eventReservations = eventReservations.ItemReservations;
                ViewBag.SelectedItems = SelectedItems;
            }
            else
            {
                //GETTING THE ITEMS THAT ARE IN THE RESERVATIONS AND CHANGING THE BOOL TO TRUE FOR OUTPUTTING THE SELECTEDITEMS TO THE PAGE
                var itemReservations = _context.ItemReservations.Include(i => i.Item).Include(i => i.Event)
                    .Where(i => i.EventId == Convert.ToInt32(HttpContext.Session.GetString("EventIDForEditOverView")))
                    .ToList();
                foreach (var itemRes in itemReservations)
                {
                    foreach (var item in _context.Items)
                    {
                        if (itemRes.Item.ID == item.ID)
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




                ViewBag.eventReservations = eventReservations.ItemReservations;
                ViewBag.SelectedItems = SelectedItems;
            }




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
                    if (!EventExists(eventToUpdate.ID))
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



        // GET: SelectItems
        public async Task<IActionResult> EditSelectItems(int? EventID, int? CategoryID, string SearchString, int? page, int? pageSizeID
            , string actionButton, string sortDirection = "asc", string sortField = "Item")
        {


            _toastNotification.AddInfoToastMessage("Add or Remove The Items That Are In This Event");
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


            var SelectedItems = await _context.Items.Include(i => i.Supplier).Include(i => i.Category).Include(i => i.ItemImages).Include(i => i.ItemThumbNail)
            .ToListAsync();


            var LoggedInItems = _context.ItemReservations
            //.Where(item => item.Inventories.Count(i => i.LocationID == toLocationID) > 1)
            .Include(i => i.Item)
            .Include(i => i.Event)
            .Where(i => i.EventId == Convert.ToInt32(HttpContext.Session.GetString("EventIDForEditOverView")) && i.IsLoggedIn == true)
            .ToList();


            ViewBag.LoggedInItems = LoggedInItems;


            ViewBag.SelectedItems = SelectedItems;
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
        public async Task<IActionResult> EditSelectItems(int ItemID)
        {
            if (ModelState.IsValid)
            {
                bool flag = false;
                var itemsupdate = _context.Items.Include(i => i.ItemImages).Include(i => i.ItemThumbNail).Where(i => i.ID == ItemID).FirstOrDefault();
                var itemresTomake = _context.ItemReservations.Include(i => i.Item).Include(i => i.Event)
                    .Where(i => i.EventId == Convert.ToInt32(HttpContext.Session.GetString("EventIDForEditOverView")))
                    .ToList();

                foreach (var res in itemresTomake)
                {
                    if (res.ItemId == ItemID)
                    {
                        flag = true;
                        break;
                    }
                }
                if (flag == false)
                {
                    var availablelocations = _context.Inventories
                        .Where(i => i.ItemID == ItemID)
                        .FirstOrDefault();
                    //CREATING THE RECORDS NOW:
                    ItemReservation createItemReserv = new ItemReservation();
                    createItemReserv.ItemId = ItemID;
                    createItemReserv.EventId = Convert.ToInt32(HttpContext.Session.GetString("EventIDForEditOverView"));
                    createItemReserv.LocationID = availablelocations.LocationID;
                    createItemReserv.Quantity = 0;
                    createItemReserv.LoggedOutDate = DateTime.Now;
                    _context.Add(createItemReserv);
                    _context.SaveChanges();

                    var eventName = _context.Events
                        .Where(i => i.ID == Convert.ToInt32(HttpContext.Session.GetString("EventIDForEditOverView")))
                        .FirstOrDefault();
                    // Create a new event log entry
                    var eventLog = new EventLog
                    {
                        EventName = eventName.Name,
                        ItemName = createItemReserv.Item.Name,
                        Quantity = createItemReserv.Quantity,
                        LogDate = DateTime.Now,
                        ItemReservation = createItemReserv
                    };
                    _context.Add(eventLog);
                }




                itemsupdate.isSlectedForEvent = true;
                //_context.Update(itemsupdate);
                await _context.SaveChangesAudit();



                _context.SaveChanges();

                HttpContext.Session.SetString("TypeOfOperationReservations", "EditedItems");
                return RedirectToAction("SelectItemsEdit", "Events");
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

            if (ModelState.IsValid)
            {

                _toastNotification.AddInfoToastMessage(ItemID.ToString());
                var itemsupdate = _context.Items.Include(i => i.ItemImages).Include(i => i.ItemThumbNail).Where(i => i.ID == ItemID).FirstOrDefault();
                var itemresToRemove = _context.ItemReservations.Include(i => i.Item).Include(i => i.Event)
                    .Where(i => i.EventId == Convert.ToInt32(HttpContext.Session.GetString("EventIDForEditOverView")))
                    .ToList();

                foreach (var res in itemresToRemove)
                {
                    if (res.ItemId == ItemID)
                    {
                        if (res.Event.ReservedEventDate <= DateTime.Today)
                        {
                            var inventoryToUpdate = await _context.Inventories.Include(i => i.Location).Include(i => i.Item)
                            .Where(i => i.ItemID == res.ItemId && i.LocationID == res.LocationID)
                            .FirstOrDefaultAsync();
                            if (inventoryToUpdate != null)
                            {
                                //Updating the Inventory before deleting the reservation record
                                inventoryToUpdate.Quantity += res.Quantity;

                                _context.Update(inventoryToUpdate);
                            }
                        }
                        var eventlog = _context.EventLogs
                            .Where(i => i.EventName == res.Event.Name && i.ItemName == res.Item.Name && i.ItemReservationId == res.Id)
                            .FirstOrDefault();


                        _context.Remove(eventlog);
                        _context.Remove(res);
                        _context.SaveChanges();
                    }
                }



                itemsupdate.isSlectedForEvent = false;
                _context.Update(itemsupdate);
                _context.SaveChanges();

                //_context.SaveChanges();

                return RedirectToAction("SelectItems", "Events");
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

            _toastNotification.AddInfoToastMessage("Modify The Locations And/Or Quantities For The Event");


            var events = _context.Items.Include(i => i.Supplier)
                .AsNoTracking();

            //var SelectedItems = await _context.Items
            //    .Include(i => i.Supplier)
            //    .Include(i => i.Category)
            //    .Include(i => i.Inventories)
            //    .Include(i => i.ItemImages)
            //    .Include(i => i.ItemThumbNail)
            //    .Include(i => i.Inventories).ThenInclude(i => i.Location)
            //    .Where(i => i.isSlectedForEvent == true)
            //.ToListAsync();

            var ItemReservations = await _context.ItemReservations
                .Include(i => i.Item).ThenInclude(i => i.Supplier)
                .Include(i => i.Item).ThenInclude(i => i.ItemImages)
                .Include(i => i.Item).ThenInclude(i => i.ItemThumbNail)
                .Include(i => i.Item).ThenInclude(i => i.Category)
                .Include(i => i.Item).ThenInclude(i => i.Inventories).ThenInclude(i => i.Location)
                .Where(i => i.EventId == Convert.ToInt32(HttpContext.Session.GetString("EventIDForEditOverView")))
            .ToListAsync();


            return View(ItemReservations);




        }


        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> ChooseItemQuantitiesEdit(int id)
        {
            bool OverQuantityFlag = false;
            bool OverBookingFlag = false;
            int EventIDForOverBookCheck = Convert.ToInt32(HttpContext.Session.GetString("EventIDForEditOverView"));
            var EventCheck = _context.Events
                .Where(i => i.ID == EventIDForOverBookCheck)
                .FirstOrDefault();
            var ReserveEventDate = EventCheck.ReservedEventDate;
            foreach (var item in _context.Items)
            {
                if (Request.Form.ContainsKey("itemId" + item.ID.ToString()))
                {
                    //Getting the quantity of the item and location selected
                    int Quantity = int.Parse(Request.Form["itemId" + item.ID.ToString()]);

                    var itemName = _context.Items.FirstOrDefault(i => i.ID == item.ID);

                    //This code is for when the user doesnt select anything, the outfilled location does not retrive the value here, so this is a shortcut to get its value.
                    var itemres = _context.ItemReservations
                    .Where(i => i.ItemId == itemName.ID && i.EventId == Convert.ToInt32(HttpContext.Session.GetString("EventIDForEditOverView")))
                    .Include(i=>i.Location)
                    .FirstOrDefault();

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

                            locationID = itemres.LocationID;
                        }
                    }
                    // Update the inventory quantity
                    var inventory = await _context.Inventories.Include(i => i.Location).Include(i => i.Item)
                        .Where(i => i.ItemID == item.ID && i.LocationID == locationID)
                        .FirstOrDefaultAsync();
                    string OverQtyOutput = "";
                    if (inventory.Location.Name != itemres.Location.Name)
                    {
                        if ((inventory.Quantity) < Quantity)
                        {
                            OverQuantityFlag = true;
                            _toastNotification.AddErrorToastMessage($"Oops, You entered <u>invalid quantity</u> {Quantity}. This exceeds the {inventory.Item.Name} stock, of {inventory.Quantity}.  <br/> please review your numbers and try again." +
                                $"");
                        }
                    }
                    else if ((inventory.Quantity + itemres.Quantity) < Quantity)
                    {
                        OverQuantityFlag = true;
                        _toastNotification.AddErrorToastMessage($"Oops, You entered <u>invalid quantity</u> {Quantity}. This exceeds the {inventory.Item.Name} stock, of {inventory.Quantity}.  <br/> please review your numbers and try again." +
                            $"");
                    }
                    if (Quantity < 0)
                    {
                        OverQuantityFlag = true;
                        _toastNotification.AddErrorToastMessage($"Oops, You cant enter negative numbers... Please Try Again" +
                            $"");
                    }

                }
            }

            if (ReserveEventDate > DateTime.Today)
            {
                //FOR THE OVERBOOKINGFLAG
                foreach (var item in _context.Items)
                {
                    if (Request.Form.ContainsKey("itemId" + item.ID.ToString()))
                    {
                        //Getting the quantity of the item and location selected
                        int Quantity = int.Parse(Request.Form["itemId" + item.ID.ToString()]);

                        var itemName = _context.Items.FirstOrDefault(i => i.ID == item.ID);

                        //This code is for when the user doesnt select anything, the outfilled location does not retrive the value here, so this is a shortcut to get its value.
                        var itemres = _context.ItemReservations
                        .Where(i => i.ItemId == itemName.ID && i.EventId == Convert.ToInt32(HttpContext.Session.GetString("EventIDForEditOverView")))
                        .FirstOrDefault();

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

                                locationID = itemres.LocationID;
                            }
                        }
                        // Update the inventory quantity
                        var inventory = await _context.Inventories.Include(i => i.Location).Include(i => i.Item)
                            .Where(i => i.ItemID == item.ID && i.LocationID == locationID)
                            .FirstOrDefaultAsync();
                        //MAKING SURE THE QUANTITES OF THE INVENTORIES FOR THE EVENT DONT OVERLAP THE QUANITIES OF OTHER EVENTS AND PREVENT OVERBOOKING

                        var Event = _context.Events
                            .Where(i => i.ID == EventIDForOverBookCheck)
                            .FirstOrDefault();

                        var ReservedDate = Event.ReservedEventDate;
                        var ReturnDate = Event.ReturnEventDate;

                        //GETTING THE ITEMS THAT ARE IN THE RESERVATIONS AND CHANGING THE BOOL TO TRUE FOR OUTPUTTING THE SELECTEDITEMS TO THE PAGE
                        var itemResOverBookingCaution = _context.ItemReservations
                            .Include(i => i.Item)
                            .Include(i => i.Event)
                            .Where(i =>
                                (i.Event.ReservedEventDate >= ReservedDate && i.Event.ReservedEventDate < ReturnDate) ||
                                (i.Event.ReturnEventDate > ReservedDate && i.Event.ReturnEventDate <= ReturnDate) ||
                                (i.Event.ReturnEventDate == ReservedDate && i.Event.ReturnEventDate == ReturnDate) ||
                                (i.Event.ReservedEventDate < ReservedDate && i.Event.ReturnEventDate > ReturnDate))
                            .ToList();



                        //////GETTING THE ITEMS THAT ARE IN THE RESERVATIONS AND CHANGING THE BOOL TO TRUE FOR OUTPUTTING THE SELECTEDITEMS TO THE PAGE
                        ////var itemResOverBookingCaution = _context.ItemReservations
                        ////    .Include(i => i.Item)
                        ////    .Include(i => i.Event)
                        ////    .Where(i =>
                        ////        (i.Event.ReservedEventDate >= ReservedDate && i.Event.ReturnEventDate <= ReturnDate) ||
                        ////        (i.Event.ReservedEventDate <= ReservedDate && i.Event.ReturnEventDate >= ReservedDate && i.Event.ReturnEventDate <= ReturnDate) ||
                        ////        (i.Event.ReservedEventDate <= ReservedDate && i.Event.ReturnEventDate >= ReturnDate) ||
                        ////        (i.Event.ReturnEventDate <= ReturnDate && i.Event.ReservedEventDate >= ReturnDate && i.Event.ReservedEventDate <= ReservedDate) ||
                        ////        (i.Event.ReservedEventDate <= ReservedDate && i.Event.ReturnEventDate >= ReturnDate))
                        ////    .ToList();
                        bool flag = false;
                        List<ItemReservation> itemresCheck = new List<ItemReservation>();
                        int potentialItemQuantity = 0;
                        foreach (var res in itemResOverBookingCaution)
                        {
                            if (res.ItemId == inventory.ItemID && res.LocationID == inventory.LocationID)
                            {
                                potentialItemQuantity += res.Quantity;
                            }
                        }
                        foreach (var resOverBook in itemResOverBookingCaution)
                        {
                            if (resOverBook.ItemId == inventory.ItemID && resOverBook.LocationID == inventory.LocationID)
                            {

                                if ((potentialItemQuantity + Quantity) > inventory.Quantity)
                                {
                                    flag = true;
                                    itemresCheck.Add(resOverBook);
                                }
                            }
                        }
                        if (flag == true)
                        {
                            OverBookingFlag = true;
                            string output2 = "OverBooking Error: Other events reserved: <br/> ";
                            foreach (var outputOverBook in itemresCheck)
                            {
                                var inv = _context.Inventories.Where(i => i.ItemID == outputOverBook.ItemId && i.LocationID == outputOverBook.LocationID).FirstOrDefault();
                                int qty = inv.Quantity - potentialItemQuantity;
                                output2 += $"<u>{outputOverBook.Event.Name} - {outputOverBook.Item.Name}</u> ({qty} available at {outputOverBook.Location.Name}) <br/>";
                            }
                            _toastNotification.AddErrorToastMessage(output2);
                        }
                    }
                }
            }

            if (OverQuantityFlag == false && OverBookingFlag == false)
            {
                string output = "";
                int EventID = HttpContext.Session.GetInt32("EventID") ?? default;
                foreach (var item in _context.Items)
                {
                    if (Request.Form.ContainsKey("itemId" + item.ID.ToString()))
                    {
                        var itemName = _context.Items.FirstOrDefault(i => i.ID == item.ID);
                        //Getting the quantity of the item and location selected
                        int Quantity = int.Parse(Request.Form["itemId" + item.ID.ToString()]);

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
                                //This code is for when the user doesnt select anything, the outfilled location does not retrive the value here, so this is a shortcut to get its value.
                                var itemres = _context.ItemReservations
                                .Where(i => i.ItemId == itemName.ID && i.EventId == Convert.ToInt32(HttpContext.Session.GetString("EventIDForEditOverView")))
                                .FirstOrDefault();
                                locationID = itemres.LocationID;
                            }
                        }

                        //Getting the Name of the location they selected by id
                        var location = _context.Locations
                            .Where(i => i.Id == locationID)
                            .FirstOrDefault();



                        //UPDATING THE INVENTORY BEFORE UPDATING THE RESERVATION
                        var OldItemReservation = _context.ItemReservations.Include(i => i.Event)
                        .Where(i => i.ItemId == itemName.ID && i.EventId == Convert.ToInt32(HttpContext.Session.GetString("EventIDForEditOverView")))
                        .FirstOrDefault();

                        if (OldItemReservation.IsLoggedIn == true)
                        {
                            _toastNotification.AddInfoToastMessage($"{OldItemReservation.Item.Name}'s Reservation Was Modified!");
                        }
                        else
                        {
                            // Update the inventory quantity
                            var Oldfrominventory = await _context.Inventories.Include(i => i.Location).Include(i => i.Item)
                                .Where(i => i.ItemID == OldItemReservation.Item.ID && i.LocationID == OldItemReservation.LocationID)
                                .FirstOrDefaultAsync();

                            var newItemReserv = await _context.ItemReservations.Include(i => i.Location).Include(i => i.Item)
                            .Where(i => i.ItemId == item.ID && i.LocationID == locationID && i.EventId == Convert.ToInt32(HttpContext.Session.GetString("EventIDForEditOverView")))
                            .FirstOrDefaultAsync();


                            //THIS LOGIC IS FINDING IF THE USER SELECTED A DIFFERENT LOCATION, SO WE WILL ADD THE RESEREVED QTY BACK TO ITS INVENTORY
                            if (newItemReserv == null)
                            {

                                if (Oldfrominventory != null && OldItemReservation.Event.ReservedEventDate <= DateTime.Today)
                                {

                                    Oldfrominventory.Quantity += OldItemReservation.Quantity;
                                    //Oldfrominventory.Quantity -= Convert.ToInt32(Quantity);
                                    //_toastNotification.AddSuccessToastMessage(Oldfrominventory.Quantity.ToString());
                                }

                            }


                            // Update the inventory quantity
                            var inventory = await _context.Inventories.Include(i => i.Location).Include(i => i.Item)
                                .Where(i => i.ItemID == item.ID && i.LocationID == locationID)
                                .FirstOrDefaultAsync();


                            //MAKING SURE THE RESERVE DATE IS AFTER TODAY TO DEDUCT INVENTORY, IF NOT THEN NOTHING WILL HAPPEN REGARD SUBTRACTIONS
                            if (OldItemReservation.Event.ReservedEventDate <= DateTime.Today)
                            {
                                if (inventory != null)
                                {
                                    if (newItemReserv != null)
                                    {

                                        inventory.Quantity += OldItemReservation.Quantity;


                                    }
                                    inventory.Quantity -= Quantity;
                                    //_toastNotification.AddSuccessToastMessage($"Inventory For {inventory.Item.Name}: {inventory.Quantity}");
                                    _context.Update(inventory);
                                    _context.SaveChanges();

                                }
                            }


                            //CAR WINTER SCRAPER: STOCK - 398

                            ////CREATING THE RECORDS NOW:
                            ///
                            var UpdateItemReserv = _context.ItemReservations.Include(i => i.Event)
                                    .Where(i => i.ItemId == itemName.ID && i.EventId == Convert.ToInt32(HttpContext.Session.GetString("EventIDForEditOverView")))
                                    .FirstOrDefault();
                            if (UpdateItemReserv != null)
                            {
                                UpdateItemReserv.Quantity = Quantity;
                                UpdateItemReserv.LocationID = locationID;
                                UpdateItemReserv.LoggedOutDate = DateTime.Now;

                                if (UpdateItemReserv.Event.ReservedEventDate <= DateTime.Today)
                                {
                                    //_toastNotification.AddInfoToastMessage(createItemReserv.Event.ReservedEventDate.ToString());
                                    UpdateItemReserv.IsInventoryDeducted = true;
                                    _context.Update(UpdateItemReserv);

                                }
                                //_context.Update(UpdateItemReserv);


                                var eventlog = _context.EventLogs
                                    .Where(i => i.ItemReservationId == UpdateItemReserv.Id)
                                    .FirstOrDefault();


                                if (eventlog.Quantity != Quantity)
                                {

                                    eventlog.Quantity = Quantity;
                                    eventlog.LogDate = DateTime.Now;
                                }


                            }
                            else
                            {

                                var getEventDate = _context.Events
                                    .Where(i => i.ID == EventID)
                                    .FirstOrDefault();



                                ItemReservation createItemReserv = new ItemReservation();

                                createItemReserv.ItemId = item.ID;
                                createItemReserv.EventId = EventID;
                                createItemReserv.LocationID = locationID;
                                createItemReserv.Quantity = Quantity;
                                createItemReserv.LoggedOutDate = DateTime.Now;

                                _context.Add(createItemReserv);

                                if (getEventDate.ReservedEventDate <= DateTime.Today)
                                {
                                    //_toastNotification.AddInfoToastMessage(createItemReserv.Event.ReservedEventDate.ToString());
                                    createItemReserv.IsInventoryDeducted = true;
                                    _context.Update(createItemReserv);

                                }
                                // Create a new event log entry
                                var eventLog = new EventLog
                                {
                                    EventName = createItemReserv.Event.Name,
                                    ItemName = createItemReserv.Item.Name,
                                    Quantity = createItemReserv.Quantity,
                                    LogDate = DateTime.Now,
                                    ItemReservation = createItemReserv
                                };
                                _context.Add(eventLog);
                            }


                            _context.SaveChanges();
                        }
                    }
                    foreach (var item1 in _context.Items)
                    {
                        item1.isSlectedForEvent = false;
                        _context.Update(item1);

                    }
                    _context.SaveChanges();

                    //_toastNotification.AddErrorToastMessage($"{output} EventID: {EventID.ToString()}");


                }
                _toastNotification.AddSuccessToastMessage("Event Record Updated Successfully");
                return RedirectToAction("EditOverview", "Events", new { Id = Convert.ToInt32(HttpContext.Session.GetString("EventIDForEditOverView")) });

            }
            else
            {

                return RedirectToAction("ChooseItemQuantitiesEdit", "Events");
            }



        }


        public int[] HasReservationStarted()
        {
            int[] wasDeducted = new int[0];
            foreach (var itemRes in _context.ItemReservations.Include(i => i.Event))
            {
                if (itemRes.Event.ReservedEventDate.HasValue && itemRes.Event.ReservedEventDate.Value <= DateTime.Now && !itemRes.IsInventoryDeducted && itemRes.IsLoggedIn == false)
                {
                    var inventory = _context.Inventories
                        .Where(i => i.ItemID == itemRes.ItemId && i.LocationID == itemRes.LocationID)
                        .FirstOrDefault();

                    if (inventory != null)
                    {
                        inventory.Quantity -= itemRes.Quantity;
                    }
                    itemRes.IsInventoryDeducted = true; // Set the flag to indicate that inventory has been deducted
                    _context.Update(itemRes);
                    wasDeducted = wasDeducted.Append(itemRes.Id).ToArray();
                }
            }
            _context.SaveChanges();
            return wasDeducted;
        }

        private string ControllerName()
        {
            return this.ControllerContext.RouteData.Values["controller"].ToString();
        }
        private void ViewDataReturnURL()
        {
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, ControllerName());
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.ID == id);
        }
    }
}