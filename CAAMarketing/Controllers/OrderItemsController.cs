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
using Microsoft.AspNetCore.Authorization;
using System.Drawing.Imaging;
using System.Drawing;
using ZXing.Common;
using ZXing;
using Microsoft.Extensions.Logging;

namespace CAAMarketing.Controllers
{
    [Authorize]
    public class OrderItemsController : Controller
    {
        private readonly CAAContext _context;
        private readonly IToastNotification _toastNotification;

        public OrderItemsController(CAAContext context, IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
        }

        // GET: OrderItems

        // GET: Orders
        public async Task<IActionResult> Index(int? ItemID, string SearchString, int? SupplierID, int? page, int? pageSizeID
            , string actionButton, string sortDirection = "asc", string sortField = "OrderItem")
        {
            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            var email = User.Identity.Name;

            var employee = _context.Employees.FirstOrDefault(e => e.Email == email);
            

            // Get the value of MySessionVariable from the session state
            string foundsession = HttpContext.Session.GetString("OrderandItemCreated");

            if (foundsession == "True")
            {
                _toastNotification.AddSuccessToastMessage($"New Item Completed! Take A Look At The Overview.");
            }

            //Get the URL with the last filter, sort and page parameters from THE PATIENTS Index View
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, "Items");


            if (!ItemID.HasValue)
            {
                //Go back to the proper return URL for the Patients controller
                return Redirect(ViewData["returnURL"].ToString());
            }

            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering
            //Then in each "test" for filtering, add ViewData["Filtering"] = " show" if true;


            //Populating the DropDownLists for the Search/Filtering criteria, which are the Category and Supplier DDL
            ViewData["SupplierID"] = new SelectList(_context.Suppliers, "ID", "Name");


            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "OrderItem", "UPC", "Quantity", "Cost", "DateMade", "DeliveryDate", "Location" };

            var orders = _context.Orders
                .Include(o => o.Item)
                .Include(i => i.Location)
                .Where(p => p.ItemID == ItemID.GetValueOrDefault())
                .AsNoTracking();

            //Add as many filters as needed
            if (SupplierID.HasValue)
            {
                orders = orders.Where(p => p.Item.SupplierID == SupplierID);
                ViewData["Filtering"] = "btn-danger";
            }
            if (!String.IsNullOrEmpty(SearchString))
            {
                long searchUPC;
                bool isNumeric = long.TryParse(SearchString, out searchUPC);

                orders = orders.Where(p => p.Item.Name.ToUpper().Contains(SearchString.ToUpper())
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
            if (sortField == "DeliveryDate")
            {
                if (sortDirection == "asc")
                {
                    orders = orders
                        .OrderBy(p => p.DeliveryDate);
                }
                else
                {
                    orders = orders
                        .OrderByDescending(p => p.DeliveryDate);
                }
            }
            else if (sortField == "DateMade")
            {
                if (sortDirection == "asc")
                {
                    orders = orders
                        .OrderByDescending(p => p.DateMade);
                }
                else
                {
                    orders = orders
                        .OrderBy(p => p.DateMade);
                }
            }
            else if (sortField == "UPC")
            {
                if (sortDirection == "asc")
                {
                    orders = orders
                        .OrderBy(p => p.Item.UPC);
                }
                else
                {
                    orders = orders
                        .OrderByDescending(p => p.Item.UPC);
                }
            }
            else if (sortField == "Cost")
            {
                if (sortDirection == "asc")
                {
                    orders = orders
                        .OrderBy(p => p.Cost.ToString());
                }
                else
                {
                    orders = orders
                        .OrderByDescending(p => p.Cost.ToString());
                }
            }
            else if (sortField == "Quantity")
            {
                if (sortDirection == "asc")
                {
                    orders = orders
                        .OrderBy(p => p.Quantity);
                }
                else
                {
                    orders = orders
                        .OrderByDescending(p => p.Quantity);
                }
            }
            else if (sortField == "Location")
            {
                if (sortDirection == "asc")
                {
                    orders = orders
                        .OrderBy(p => p.Location);
                }
                else
                {
                    orders = orders
                        .OrderByDescending(p => p.Location);
                }
            }
            else //Sorting by Patient Name
            {
                if (sortDirection == "asc")
                {
                    orders = orders
                        .OrderBy(p => p.Item.Name);
                }
                else
                {
                    orders = orders
                        .OrderByDescending(p => p.Item.Name);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            //Handle Paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "Receiving");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<Receiving>.CreateAsync(orders.AsNoTracking(), page ?? 1, pageSize);


            Item item = _context.Items
                .Include(i => i.Inventories).ThenInclude(i => i.Location)
               .Include(i => i.Category)
               .Include(i => i.Supplier)
               .Include(i => i.Employee)
               .Include(p => p.ItemThumbNail)
               .Include(i => i.ItemImages)
               .Include(i => i.ItemReservations).ThenInclude(i => i.Event)
               .Include(i => i.ItemReservations).ThenInclude(i => i.Location)
               .Include(I => I.InventoryTransfers).ThenInclude(i => i.FromLocation)
               .Include(I => I.InventoryTransfers).ThenInclude(i => i.ToLocation)
               .Where(p => p.ID == ItemID.GetValueOrDefault())
               .AsNoTracking()
               .FirstOrDefault();


            Item itemUpdate = _context.Items
               .Include(i => i.Category)
               .Include(i => i.Supplier)
               .Include(i => i.Employee)
               .Include(p => p.ItemThumbNail)
               .Include(i => i.ItemImages)
               .Include(i => i.ItemReservations)
               .Where(p => p.ID == ItemID.GetValueOrDefault())
               .AsNoTracking()
               .FirstOrDefault();


            Inventory inventory = _context.Inventories
                .Include(i => i.Location)
                 .Where(p => p.ItemID == ItemID.GetValueOrDefault())
                 .FirstOrDefault();


            if (inventory == null)
            {
                item.Quantity = 0;
            }

            //_context.Update(itemUpdate);
            //_context.SaveChanges();


            var itemReservations = await _context.ItemReservations
            .Where(ir => ir.ItemId == ItemID.GetValueOrDefault() && !ir.IsDeleted)
            .ToListAsync();

            ViewBag.ItemReservations = itemReservations;

            ViewBag.Item = item;

            // Ensure the UPC is 13 digits long by padding with zeros and calculating the checksum digit.
            var upcString = item.UPC.ToString();

            // Generate the barcode image.
            var writer = new BarcodeWriterSvg
            {
                Format = BarcodeFormat.EAN_13,
                Options = new EncodingOptions
                {
                    Height = 20,
                    Width = 80,
                    Margin = 1
                }
            };


            var svgContent = writer.Write(upcString);

            // Save the SVG content as a string
            ViewBag.BarcodeImage = svgContent.Content;


            return View(pagedData);
        }

        private static char CalculateUPCChecksum(string upc)
        {
            int sum = 0;

            for (int i = 0; i < 11; i++)
            {
                sum += (upc[i] - '0') * (i % 2 == 0 ? 3 : 1);
            }

            int checkDigit = 10 - (sum % 10);
            if (checkDigit == 10)
            {
                checkDigit = 0;
            }

            return (char)(checkDigit + '0');
        }


        // GET: OrderItems/Create
        public IActionResult Add(int? ItemID, string ItemName)
        {
            if (!ItemID.HasValue)
            {
                return Redirect(ViewData["returnURL"].ToString());
            }
            ViewData["ItemName"] = ItemName;
            Receiving a = new Receiving()
            {
                ItemID = ItemID.GetValueOrDefault()
            };


            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name");

            return View(a);
        }

        // POST: OrderItems/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add([Bind("ID,Quantity,DateMade,DeliveryDate,Cost,ItemID, LocationID")] Receiving order
    , string ItemName, int ItemID)
        {
            //Get the URL with the last filter, sort and page parameters
            ViewDataReturnURL();
            if (order.Quantity < 0)
            {
                _toastNotification.AddErrorToastMessage("Oops, you cant have a negative quantity, please try again...");
            }
            else
            {
                try
                {
                    if (ModelState.IsValid)
                    {

                        _context.Add(order);
                        await _context.SaveChangesAudit();

                        _toastNotification.AddSuccessToastMessage("Receiving Created, The Quanitity Is Added To The Inventory");
                        // Get the corresponding inventory item
                        var inventoryItem = await _context.Inventories.Include(i => i.Item).Where(i => i.LocationID == order.LocationID).FirstOrDefaultAsync(i => i.ItemID == order.ItemID);
                        if (inventoryItem != null)
                        {
                            // Update the inventory with the ordered quantity and cost

                            inventoryItem.Quantity += order.Quantity;
                            inventoryItem.Cost = order.Cost;
                            inventoryItem.Item.DateReceived = DateTime.Today;
                            // Save changes to the inventory
                            _context.Update(inventoryItem);
                            await _context.SaveChangesAudit();
                        }
                        else
                        {

                            Inventory invCreate = new Inventory();

                            invCreate.LocationID = order.LocationID;
                            invCreate.ItemID = order.ItemID;
                            invCreate.Quantity = order.Quantity;
                            invCreate.Cost = order.Cost;
                            _context.Add(invCreate);

                            await _context.SaveChangesAudit();
                        }
                        ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name", order.LocationID);
                        return RedirectToAction("Index", "OrderItems", new { order.ItemID });
                    }
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem " +
                        "persists see your system administrator.");
                }

                ViewData["ItemName"] = ItemName;
                return View(order);
            }

            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name");
            return View(order);
        }
        // GET: orderitems/Update/5
        public async Task<IActionResult> UpdateAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            ViewDataReturnURL();

            var order = await _context.Orders
                .Include(o => o.Item)
                .Include(i => i.Location)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);

            if (order == null)
            {
                return NotFound();
            }

            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name", order.LocationID);
            return View(order);
        }


        // POST: orderitems/Update/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id)
        {
            ViewDataReturnURL();

            var orderToUpdate = await _context.Orders.FirstOrDefaultAsync(o => o.ID == id);

            if (orderToUpdate == null)
            {
                return NotFound();
            }
            var oldOrderQuantity = orderToUpdate.Quantity;
            if (await TryUpdateModelAsync<Receiving>(orderToUpdate, "",
                o => o.Quantity, o => o.DateMade, o => o.DeliveryDate, o => o.Cost, o => o.ItemID, o => o.LocationID))
            {
                try
                {
                    if (orderToUpdate.Quantity < 0)
                    {
                        _toastNotification.AddErrorToastMessage("Oops, you cannot have negative numbers, please try again.");

                        return RedirectToAction("Update", "OrderItems", new { ItemID = orderToUpdate.ItemID });
                    }
                    else
                    {
                        _context.Update(orderToUpdate);

                        var inventory = await _context.Inventories.Where(i => i.ItemID == orderToUpdate.ItemID && i.LocationID == orderToUpdate.LocationID).FirstOrDefaultAsync();
                        var item = await _context.Items.Where(i => i.ID == orderToUpdate.ItemID).FirstOrDefaultAsync();
                        if (inventory != null)
                        {
                            var newInventoryQuantity = inventory.Quantity + (orderToUpdate.Quantity - oldOrderQuantity);
                            if (newInventoryQuantity >= 0)
                            {
                                inventory.Quantity = newInventoryQuantity;
                                inventory.Cost = orderToUpdate.Cost;
                                //inventory.Item.DateReceived = orderToUpdate.DeliveryDate.Value;
                                item.Cost = orderToUpdate.Cost;

                            }
                            else
                            {
                                _context.Inventories.Remove(inventory);
                            }
                            //_context.Update(inventory);
                            //_context.Update(item);
                            await _context.SaveChangesAudit();
                        }
                        else
                        {
                            if (orderToUpdate.Quantity > 0)
                            {
                                inventory = new Inventory
                                {
                                    ItemID = orderToUpdate.ItemID,
                                    Quantity = orderToUpdate.Quantity,
                                    Cost = orderToUpdate.Cost
                                };
                                _context.Inventories.Add(inventory);
                                await _context.SaveChangesAudit();
                            }
                        }
                        await _context.SaveChangesAudit();

                        _toastNotification.AddSuccessToastMessage("Updated the receving record.");

                        return Redirect(ViewData["returnURL"].ToString());
                    }

                }

                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(orderToUpdate.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem " +
                        "persists see your system administrator." + ex.Message.ToString());
                }
            }
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var err in errors)
                {
                    // loop through the errors and display them to the user
                    _toastNotification.AddErrorToastMessage($"{err.ErrorMessage.ToString()}");
                }

            }
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name", orderToUpdate.LocationID);
            return View(orderToUpdate);
        }

        // GET: orderitems/Remove/5
        public async Task<IActionResult> Remove(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            //Get the URL with the last filter, sort and page parameters
            ViewDataReturnURL();

            var order = await _context.Orders
               .Include(o => o.Item)
               .Include(i=>i.Location)
               .AsNoTracking()
               .FirstOrDefaultAsync(m => m.ID == id);

            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        // POST: orderitems/Remove/5
        [HttpPost, ActionName("Remove")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveConfirmed(int id)
        {
            var order = await _context.Orders
              .Include(o => o.Item)
              .FirstOrDefaultAsync(m => m.ID == id);

            //Get the URL with the last filter, sort and page parameters
            ViewDataReturnURL();

            try
            {
                var inventory = await _context.Inventories.Where(i => i.ItemID == order.ItemID && i.LocationID == order.LocationID).FirstOrDefaultAsync();

                if (inventory != null)
                {
                    var newInventoryQuantity = inventory.Quantity - order.Quantity;
                    if (newInventoryQuantity > 0)
                    {
                        inventory.Quantity = newInventoryQuantity;
                        _context.Inventories.Update(inventory);
                    }
                    else
                    {
                        _context.Inventories.Remove(inventory);
                    }
                }

                _context.Orders.Remove(order);
                await _context.SaveChangesAudit();
                _toastNotification.AddSuccessToastMessage("Deleted the receiving record. The quanitity has been deducted in inventory");
                return Redirect(ViewData["returnURL"].ToString());
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem " +
                    "persists see your system administrator.");
            }

            return View(order);
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.ID == id);
        }

        private string ControllerName()
        {
            return this.ControllerContext.RouteData.Values["controller"].ToString();
        }
        private void ViewDataReturnURL()
        {
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, ControllerName());
        }

        public ActionResult LogOutItem(int itemId, int eventId, int quantity)
        {
            // Get the item and event objects from the database
            var item = _context.Items.Find(itemId);
            var @event = _context.Events.Find(eventId);

            // Check if the item is already reserved for the event
            var existingReservation = _context.ItemReservations
                .FirstOrDefault(r => r.ItemId == itemId && r.EventId == eventId);

            if (existingReservation != null)
            {
                // If the item is already reserved, update the reservation with the new quantity
                existingReservation.Quantity += quantity;
            }
            else
            {
                // If the item is not already reserved, create a new reservation
                var newReservation = new ItemReservation
                {
                    Item = item,
                    Event = @event,
                    Quantity = quantity,
                    ReservedDate = DateTime.Now
                };
                _context.ItemReservations.Add(newReservation);
            }

            // Update the quantity of the item in the inventory
            item.Quantity -= quantity;

            // Save changes to the database
            _context.SaveChanges();

            // Redirect back to the event details page
            return RedirectToAction("Details", "Event", new { id = eventId });
        }


        // GET: orderitems/EditItemLocations/5
        public async Task<IActionResult> EditItemLocations(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            ViewDataReturnURL();

            var inventory = await _context.Inventories
                .Include(o => o.Item)
                .Include(i => i.Location)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (inventory == null)
            {
                return NotFound();
            }

            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name", inventory.LocationID);
            ViewData["ItemID"] = new SelectList(_context.Locations, "ID", "Name", inventory.ItemID);
            return View(inventory);
        }


        // POST: orderitems/EditItemLocations/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditItemLocations(int id)
        {
            ViewDataReturnURL();

            var ItemLocatToUpdate = await _context.Inventories
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ItemLocatToUpdate == null)
            {
                return NotFound();
            }
            if (await TryUpdateModelAsync<Inventory>(ItemLocatToUpdate, "",
            o => o.Quantity))
            {
                try
                {
                    _context.Entry(ItemLocatToUpdate).Property(x => x.Quantity).IsModified = true;
                    await _context.SaveChangesAudit();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _toastNotification.AddErrorToastMessage(ex.Message);
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem " +
                        "persists see your system administrator.");
                }
                return Redirect(ViewData["returnURL"].ToString());
            }

            if (!ModelState.IsValid)
            {
                foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _toastNotification.AddErrorToastMessage(modelError.ErrorMessage);
                }
            }

            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name", ItemLocatToUpdate.LocationID);
            return View(ItemLocatToUpdate);
        }

        // GET: orderitems/Remove/5
        public async Task<IActionResult> RemoveLocationInv(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            //Get the URL with the last filter, sort and page parameters
            ViewDataReturnURL();

            var inventory = await _context.Inventories
                .Include(o => o.Item)
                .Include(i => i.Location)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (inventory == null)
            {
                return NotFound();
            }
            return View(inventory);
        }

        // POST: orderitems/Remove/5
        [HttpPost, ActionName("RemoveLocationInv")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLocationInvConfirmed(int id)
        {
            var inventory = await _context.Inventories
                .Include(o => o.Item)
                .Include(i => i.Location)
                .FirstOrDefaultAsync(m => m.Id == id);

            //Get the URL with the last filter, sort and page parameters
            ViewDataReturnURL();

            try
            {

                _context.Inventories.Remove(inventory);

                await _context.SaveChangesAudit();
                return Redirect(ViewData["returnURL"].ToString());
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem " +
                    "persists see your system administrator.");
            }

            return View(inventory);
        }


        // GET: AddItemReservation/Add
        public IActionResult AddItemReservation(int? ItemID, string returnUrl, string eventSearchString, string itemSearchString, int LocationID)
        {

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
            var quantity = 0;

            var existingLocationIds = _context.Inventories.Where(i => i.ItemID == ItemID).Select(i => i.LocationID).Distinct().ToList();
            var locations = _context.Locations.Where(l => existingLocationIds.Contains(l.Id)).ToList();
            var GetTotalStock = _context.Items
            .Where(i => i.ID == ItemID)
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

            ViewData["LocationID"] = new SelectList(locations, "Id", "Name", LocationID);


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
                ViewBag.ReturnUrl = returnUrl;
                return View(new ItemReservation());
            }

        }

        // POST: AddItemReservation/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItemReservation(int ItemID, [Bind("Id,EventId,ItemId,Quantity,ReservedDate,ReturnDate, LocationID")] ItemReservation itemReservation, bool isLogBack, string returnUrl, int LocationID, string EventName, string EventDescr, string EventLocation)
        {
            if (ModelState.IsValid)
            {
                if(itemReservation.Quantity < 0)
                {

                    _toastNotification.AddErrorToastMessage("Oops, you entered a negative number which is not allowed. Please try again.");
                   
                    var quantity = 0;

                    var existingLocationIds = _context.Inventories.Where(i => i.ItemID == ItemID).Select(i => i.LocationID).Distinct().ToList();
                    var locations = _context.Locations.Where(l => existingLocationIds.Contains(l.Id)).ToList();
                    var GetTotalStock = _context.Items
                    .Where(i => i.ID == ItemID)
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

                    ViewData["LocationID"] = new SelectList(locations, "Id", "Name", LocationID);
                    ViewData["EventId"] = new SelectList(_context.Events, "ID", "Name", itemReservation.EventId);
                    ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name", itemReservation.ItemId);

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
                        ViewBag.ReturnUrl = returnUrl;
                        return View(new ItemReservation());
                    }
                    
                }
                else
                {
                    // Get the associated Event and Item objects
                    //itemReservation.Event = await _context.Events.FindAsync(itemReservation.EventId);
                    Event @event = new Event();
                    @event.Name = EventName;
                    @event.Description = EventDescr;
                    @event.location = EventLocation;
                    @event.ReturnEventDate = itemReservation.ReturnDate;
                    @event.ReservedEventDate = itemReservation.ReservedDate;
                    _context.Add(@event );
                    _context.SaveChanges();

                    itemReservation.EventId = @event.ID;
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


                    // Find the inventory record for the item being transferred from the specified location
                    var fromInventory = await _context.Inventories
                        .Include(i => i.Location)
                        .Where(i => i.ItemID == itemReservation.ItemId && i.LocationID == LocationID).FirstOrDefaultAsync();

                    if (isItemAvailable == true && fromInventory.Quantity >= itemReservation.Quantity)
                    {
                        // Find the inventory record for the item being transferred from the specified location
                        var InvLocationUpdate = await _context.Inventories
                            .Include(i => i.Location)
                            .Where(i => i.ItemID == itemReservation.ItemId && i.LocationID == LocationID).FirstOrDefaultAsync();

                        InvLocationUpdate.Quantity -= itemReservation.Quantity;
                        _context.Add(itemReservation);
                        //_context.Update(InvLocationUpdate);
                        await _context.SaveChangesAudit();

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
                        await _context.SaveChangesAudit();

                        // Update the inventory quantity
                        //var inventory = await _context.Inventories.Where(i => i.ItemID == itemReservation.ItemId && i.LocationID == itemReservation.LocationID).FirstOrDefaultAsync();
                        //if (inventory != null)
                        //{
                        //    if (isLogBack)
                        //    {
                        //        inventory.Quantity += itemReservation.Quantity;
                        //        itemReservation.LogBackInDate = DateTime.Now;
                        //    }
                        //    else
                        //    {
                        //        inventory.Quantity -= itemReservation.Quantity;
                        //    }
                        //}

                        //await _context.SaveChangesAudit();
                        //if (!string.IsNullOrEmpty(returnUrl))
                        //{
                        //    return Redirect(returnUrl);
                        //}
                        _toastNotification.AddSuccessToastMessage("Reservation Created, View It In The Master Details.");
                        return RedirectToAction(nameof(Index), new { ItemID = ItemID });
                    }
                    else if (!isItemAvailable)
                    {
                        ModelState.AddModelError("", "The selected item is already reserved for another event during the same time period.");
                    }
                    
                    else
                    {
                        _toastNotification.AddErrorToastMessage($"Oops, It looks like you entered a Quantity that is over the Locations Stock Limit. Pleasee enter a valid Quantity.");
                        ModelState.AddModelError("", "There is not enough inventory available for the selected item.");

                        return RedirectToAction("AddItemReservation", "OrderItems", new { itemReservation.ItemId });
                        //return View();
                    }
                }
                
            }

            ViewData["EventId"] = new SelectList(_context.Events, "ID", "Name", itemReservation.EventId);
            ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name", itemReservation.ItemId);
            return View(itemReservation);
        }


        public ActionResult AddMoreItemsEvents()
        {
            // Perform any necessary logic here
            if (HttpContext.Session.GetString("SingleEventBooking") == "True")
            {
                HttpContext.Session.SetString("ClickedAddMultipleInOrderItems", "True");
                HttpContext.Session.SetString("SingleEventBooking", "False");
            }


            // Perform any necessary logic here
            HttpContext.Session.SetString("ClickedAddMultipleInOrderItems", "True");
            return RedirectToAction("Create", "Events");
        }

        private bool CategoryExists(int id)
        {
            return _context.Category.Any(e => e.Id == id);
        }
    }
}
