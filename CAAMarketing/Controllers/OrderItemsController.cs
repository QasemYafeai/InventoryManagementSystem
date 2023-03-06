﻿using System;
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

namespace CAAMarketing.Controllers
{
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


            // Get the value of MySessionVariable from the session state
            string foundsession = HttpContext.Session.GetString("OrderandItemCreated");

            if (foundsession == "True")
            {
                _toastNotification.AddSuccessToastMessage($"New Item Completed! Take A Look At The Overview.");
            }

            //Get the URL with the last filter, sort and page parameters from THE PATIENTS Index View
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, "Inventories");


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
                .Include(i=>i.Location)
                .Where(p => p.ItemID == ItemID.GetValueOrDefault())
                .AsNoTracking();

            //Add as many filters as needed
            if (SupplierID.HasValue)
            {
                orders = orders.Where(p => p.Item.SupplierID == SupplierID);
                ViewData["Filtering"] = " show";
            }
            if (!String.IsNullOrEmpty(SearchString))
            {
                orders = orders.Where(p => p.Item.Name.ToUpper().Contains(SearchString.ToUpper())
                                       || p.Item.UPC.Contains(SearchString.ToUpper()));
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
               .Include(i => i.Category)
               .Include(i => i.Supplier)
               .Include(i => i.Employee)
               .Include(p => p.ItemThumbNail)
               .Include(i=>i.ItemReservations)
               .Include(i => i.ItemLocations).ThenInclude(i => i.Location)
               .Where(p => p.ID == ItemID.GetValueOrDefault())
               .AsNoTracking()
               .FirstOrDefault();

            Inventory inventory = _context.Inventories
                 .Where(p => p.ItemID == ItemID.GetValueOrDefault())
                 .FirstOrDefault();

            

            item.Cost = inventory.Cost;
            item.Quantity = inventory.Quantity;

            _context.Update(item);
            _context.SaveChanges();


            var itemReservations = await _context.ItemReservations
            .Where(ir => ir.ItemId == ItemID.GetValueOrDefault() && !ir.IsDeleted)
            .ToListAsync();

            ViewBag.ItemReservations = itemReservations;

            ViewBag.Item = item;
            ViewBag.Inventory = inventory;


            return View(pagedData);
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

            try
            {
                if (ModelState.IsValid)
                {
                    _context.Add(order);
                    await _context.SaveChangesAsync();

                    // Get the corresponding inventory item
                    var inventoryItem = _context.Inventories.Find(order.ItemID);
                    if (inventoryItem != null)
                    {
                        // Update the inventory with the ordered quantity and cost
                        inventoryItem.Quantity += order.Quantity;
                        inventoryItem.Cost = order.Cost;
                        //inventoryItem.Item.DateReceived = order.DeliveryDate.Value;

                        // Save changes to the inventory
                        _context.Update(inventoryItem);
                        await _context.SaveChangesAsync();
                    }
                    ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name", order.LocationID);
                    return RedirectToAction("Index", "OrderItems", new { ItemID = order.ItemID });
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
                .Include(i=>i.Location)
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
                o => o.Quantity, o => o.DateMade, o => o.DeliveryDate, o => o.Cost, o => o.ItemID))
            {
                try
                {
                    _context.Update(orderToUpdate);

                    var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ItemID == orderToUpdate.ItemID);
                    if (inventory != null)
                    {
                        var newInventoryQuantity = inventory.Quantity + (orderToUpdate.Quantity - oldOrderQuantity);
                        if (newInventoryQuantity > 0)
                        {
                            inventory.Quantity = newInventoryQuantity;
                            inventory.Cost = orderToUpdate.Cost;
                            //inventory.Item.DateReceived = orderToUpdate.DeliveryDate.Value;
                        }
                        else
                        {
                            _context.Inventories.Remove(inventory);
                        }
                        _context.Update(inventory);
                        await _context.SaveChangesAsync();
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
                            await _context.SaveChangesAsync();
                        }
                    }
                    await _context.SaveChangesAsync();
                    return Redirect(ViewData["returnURL"].ToString());
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
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem " +
                        "persists see your system administrator.");
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
                var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ItemID == order.ItemID);
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
                await _context.SaveChangesAsync();
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
        private bool CategoryExists(int id)
        {
            return _context.Category.Any(e => e.Id == id);
        }
    }
}
