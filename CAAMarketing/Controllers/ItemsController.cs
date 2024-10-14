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
using OfficeOpenXml.Style;
using OfficeOpenXml;
using System.Drawing;
using NToastNotify;
using CAAMarketing.ViewModels;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography.Xml;
using ZXing.Common;
using ZXing;
using Newtonsoft.Json;
using System.Diagnostics.Metrics;

namespace CAAMarketing.Controllers
{
    [Authorize]
    public class ItemsController : Controller
    {
        private readonly CAAContext _context;
        private readonly IToastNotification _toastNotification;

        public ItemsController(CAAContext context, IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
        }

        // GET: Inventories
        public async Task<IActionResult> Index(string SearchString1, string SearchString2, int?[] LocationID, bool? LowQty, int? MinQuantity, int? MaxQuantity,
           int? page, int? pageSizeID, string actionButton, string sortDirection = "asc", string sortField = "Item", string scannedBarcode = "")
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

            var inventories = _context.Items
                .Include(p => p.Inventories).ThenInclude(p => p.Location)
                .Include(i => i.Category)
                .Include(i => i.Supplier)
                .Include(i => i.Employee)
                .Include(p => p.ItemThumbNail)
                .Include(i => i.ItemLocations).ThenInclude(i => i.Location)
                .Include(i => i.ItemReservations)
                .Include(i => i.InventoryTransfers)
                .AsNoTracking();

            inventories = inventories.Where(p => p.Archived == false);


            //Populating the DropDownLists for the Search/Filtering criteria, which are the Location
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name");

            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "Item", "Location", "UPC", "Quantity", "Cost" };

            //Add as many filters as needed
            if (LocationID.Length > 0)
            {
                inventories = inventories.Where(p => p.Inventories.Any(i => LocationID.Contains(i.LocationID)));

                ViewData["Filtering"] = "btn-danger";
            }
            if (!String.IsNullOrEmpty(SearchString1))
            {
                long searchUPC;
                bool isNumeric = long.TryParse(SearchString1, out searchUPC);
                inventories = inventories.Where(p => (isNumeric && p.UPC == searchUPC));
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.UPC = SearchString1;
            }

            if (!String.IsNullOrEmpty(scannedBarcode))
            {
                long scannedUPC;
                bool isNumeric = long.TryParse(scannedBarcode, out scannedUPC);
                inventories = inventories.Where(p => (isNumeric && p.UPC == scannedUPC));
                ViewData["Filtering"] = "btn-danger";
            }
            if (!String.IsNullOrEmpty(SearchString2))
            {
                inventories = inventories.Where(p => p.Inventories.FirstOrDefault().Item.Name.ToUpper().Contains(SearchString2.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.ItemName = SearchString2;
            }

            // Filter by quantity range
            if (MinQuantity != null && MaxQuantity != null)
            {
                inventories = inventories.Where(x => x.Inventories.FirstOrDefault().Quantity >= MinQuantity && x.Inventories.FirstOrDefault().Quantity <= MaxQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinQuantity = MinQuantity;
                ViewBag.MaxQuantity = MaxQuantity;
            }
            else if (MinQuantity != null)
            {
                inventories = inventories.Where(x => x.Inventories.FirstOrDefault().Quantity >= MinQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MinQuantity = MinQuantity;
            }
            else if (MaxQuantity != null)
            {
                inventories = inventories.Where(x => x.Inventories.FirstOrDefault().Quantity <= MaxQuantity);
                ViewData["Filtering"] = "btn-danger";
                // Pass the values back to the view
                ViewBag.MaxQuantity = MaxQuantity;
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

            if (sortField == "Quantity")
            {
                if (sortDirection == "asc")
                {
                    inventories = inventories
                        .OrderByDescending(i => i.Inventories.FirstOrDefault().Quantity);
                }
                else
                {
                    inventories = inventories
                        .OrderBy(i => i.Inventories.FirstOrDefault().Quantity);
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
            else if (sortField == "Location")
            {
                if (sortDirection == "asc")
                {
                    inventories = inventories.OrderBy(p => p.Inventories.FirstOrDefault().Location.Name);

                }
                else
                {
                    inventories = inventories
                        .OrderByDescending(p => p.Inventories.FirstOrDefault().Location.Name);
                }
            }
            else //Sorting by Item Name
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

        // GET: Items/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Items == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Supplier)
                .Include(i => i.Employee)
                .Include(p => p.ItemImages)
                .Include(i => i.ItemLocations).ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        public IActionResult Create(string searchString)
        {
            // Add all (unchecked) options for the locations
            var item = new Item();
            PopulateAssignedLocationData(item);

            _toastNotification.AddAlertToastMessage($"Please Start By Entering Information Of The Item, You Can Cancel By Clicking The Exit Button.");

            // URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            ViewData["SupplierID"] = new SelectList(_context.Suppliers
                .Where(s => string.IsNullOrEmpty(searchString) || s.Name.Contains(searchString))
                .OrderBy(s => s.Name), "ID", "Name");

            ViewData["CategoryID"] = new SelectList(_context.Categories
                .Where(c => string.IsNullOrEmpty(searchString) || c.Name.Contains(searchString))
                .OrderBy(c => c.Name), "Id", "Name");

            // Auto-generate a unique 12-digit UPC starting with 1 and the rest being random.
            Random random = new Random();
            long newUpc;
            do
            {
                string prefix = "1";
                string manufacturerCode = random.Next(100000, 999999).ToString();
                string productCode = random.Next(10000, 100000).ToString();
                string upcString = prefix + manufacturerCode + productCode;
                int checkDigit = CalculateUPCChecksum(upcString);
                upcString = upcString + checkDigit.ToString();
                newUpc = long.Parse(upcString);
            } while (_context.Items.Any(i => i.UPC == newUpc));

            ViewData["GeneratedUPC"] = newUpc;

            return View();
        }

        private static int CalculateEAN13Checksum(string upcString)
        {
            int sumOdd = 0;
            int sumEven = 0;

            for (int i = 0; i < upcString.Length; i++)
            {
                int digit = int.Parse(upcString[i].ToString());

                if (i % 2 == 0) // Even position (0-based index)
                {
                    sumEven += digit;
                }
                else // Odd position
                {
                    sumOdd += digit;
                }
            }

            int totalSum = sumOdd * 3 + sumEven;
            int mod = totalSum % 10;

            return mod == 0 ? 0 : 10 - mod;
        }

        private HashSet<long> upcSet = new HashSet<long>();

        private static long GenerateUniqueUPC(HashSet<long> upcSet)
        {
            Random random = new Random();
            long upc;
            do
            {
                string upcString = "1" + random.Next(100_000_000, 999_999_999).ToString();
                upcString = upcString + CalculateUPCChecksum(upcString);
                upc = long.Parse(upcString);
            } while (upcSet.Contains(upc));
            upcSet.Add(upc);
            return upc;
        }

        private static long GenerateUniqueEAN13(HashSet<long> upcSet)
        {
            Random random = new Random();
            long upc;
            do
            {
                string upcString = "1" + random.Next(100_000_000, 999_999_999).ToString();
                upcString = upcString + CalculateEAN13Checksum(upcString);
                upc = long.Parse(upcString);
            } while (upcSet.Contains(upc));
            upcSet.Add(upc);
            return upc;
        }


        private static int CalculateUPCChecksum(string upcString)
        {
            int sumOdd = 0;
            int sumEven = 0;

            for (int i = 0; i < upcString.Length; i++)
            {
                int digit = int.Parse(upcString[i].ToString());

                if (i % 2 == 0) // Even position (0-based index)
                {
                    sumEven += digit;
                }
                else // Odd position
                {
                    sumOdd += digit;
                }
            }

            int totalSum = sumOdd * 3 + sumEven;
            int mod = totalSum % 10;

            return mod == 0 ? 0 : 10 - mod;
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Name,Description,Notes,CategoryID,UPC,DateReceived,SupplierID")] Item item, IFormFile thePicture, string[] selectedOptions)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            try
            {
                var email = User.Identity.Name;

                var employee = _context.Employees.FirstOrDefault(e => e.Email == email);
                item.EmployeeID = employee.ID;
                item.DateReceived = DateTime.Now;




                _context.Add(item);
                await AddPicture(item, thePicture);
                await _context.SaveChangesAudit();
                _context.SaveChanges();

                ViewData["CategoryID"] = new SelectList(_context.Category, "Id", "Name", item.CategoryID);
                ViewData["SupplierID"] = new SelectList(_context.Suppliers, "ID", "Name", item.SupplierID);

                HttpContext.Session.SetString("GetItemIDForSkipOrder", item.ID.ToString());
            }
            catch (DbUpdateException dex)
            {
                if (dex.GetBaseException().Message.Contains("UNIQUE constraint failed: Items.UPC"))
                {
                    ModelState.AddModelError("UPC", "Unable to save changes. You can have duplicate UPC's");
                    _toastNotification.AddErrorToastMessage("There was an issue saving to the database, looks like you have a duplicate UPC number with another item. Please enter a different UPC");
                }
                else
                {
                    ModelState.AddModelError("", "Unable to save changes after multiple attempts. Try again, and if the problem persists, see your system administrator.");
                    _toastNotification.AddErrorToastMessage("There was an issue saving to the database, Please try again later");
                }
            }
            catch (RetryLimitExceededException /* dex */)
            {
                ModelState.AddModelError("", "Unable to save changes after multiple attempts. Try again, and if the problem persists, see your system administrator.");
                _toastNotification.AddErrorToastMessage("There was an issue saving to the database, Please try again later. (Database rolled back record 5+ times).");
            }

            return RedirectToAction("Create", "Receiving", new { id = item.ID });
        }


        // GET: Items/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            //Get the URL with the last filter, sort and page parameters from THE itemS Index View
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, "Items");


            if (!id.HasValue)
            {
                //Go back to the proper return URL for the items controller
                return Redirect(ViewData["returnURL"].ToString());
            }

            if (id == null || _context.Items == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .Include(p => p.ItemImages)
                //.Include(p=>p.Supplier)
                .FirstOrDefaultAsync(p => p.ID == id);
            if (item == null)
            {
                return NotFound();
            }

            ViewData["CategoryID"] = new SelectList(_context.Category, "Id", "Name", item.CategoryID);
            ViewData["SupplierID"] = new SelectList(_context.Suppliers, "ID", "Name", item.SupplierID);
            return View(item);
        }

        // POST: Items/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Byte[] RowVersion, string removeImage, IFormFile thePicture,
            string InvCost, string InvQty, string[] selectedOptions)
        {
            //Get the URL with the last filter, sort and page parameters from THE itemS Index View
            ViewDataReturnURL();


            //Go get the item to update
            var itemToUpdate = await _context.Items
                .Include(p => p.ItemImages)

                .Include(i => i.ItemLocations).ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(p => p.ID == id);





            //Check that you got it or exit with a not found error
            if (itemToUpdate == null)
            {
                return NotFound();

            }

            //Put the original RowVersion value in the OriginalValues collection for the entity
            _context.Entry(itemToUpdate).Property("RowVersion").OriginalValue = RowVersion;




            //Try updating it with the values posted
            if (await TryUpdateModelAsync<Item>(itemToUpdate, "",
                p => p.Name, p => p.Description, p => p.Notes, p => p.CategoryID, p => p.UPC, p => p.Cost,
                p => p.DateReceived, p => p.SupplierID))
            {
                try
                {
                    //var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ItemID == itemToUpdate.ID);
                    //if (inventory != null)
                    //{
                    //    inventory.Quantity = itemToUpdate.Quantity;
                    //    //inventory.Cost = itemToUpdate.Cost;

                    //    _context.Update(inventory);
                    //    await _context.SaveChangesAudit();

                    //}

                    var email = User.Identity.Name;

                    var employee = _context.Employees.FirstOrDefault(e => e.Email == email);

                    if (employee != null) { itemToUpdate.EmployeeNameUser = employee.FullName; }



                    //inventoryToUpdate.Cost = Convert.ToDecimal(InvCost);
                    //inventoryToUpdate.Quantity = Convert.ToInt32(InvQty);


                    //For the image
                    if (removeImage != null)
                    {
                        //If we are just deleting the two versions of the photo, we need to make sure the Change Tracker knows
                        //about them both so go get the Thumbnail since we did not include it.
                        itemToUpdate.ItemThumbNail = _context.ItemThumbNails.Where(p => p.ItemID == itemToUpdate.ID).FirstOrDefault();
                        //Then, setting them to null will cause them to be deleted from the database.
                        itemToUpdate.ItemImages = null;
                        itemToUpdate.ItemThumbNail = null;
                    }
                    else
                    {
                        await AddPicture(itemToUpdate, thePicture);
                    }

                    await _context.SaveChangesAudit();

                    //_context.Add(inventoryToUpdate);
                    //_context.SaveChanges();
                    // return RedirectToAction(nameof(Index));

                    _toastNotification.AddSuccessToastMessage("Item Record Updated!");
                    return RedirectToAction("Index", "OrderItems", new { ItemID = itemToUpdate.ID });



                }
                catch (RetryLimitExceededException /* dex */)
                {
                    ModelState.AddModelError("", "Unable to save changes after multiple attempts. Try again, and if the problem persists, see your system administrator.");
                    _toastNotification.AddErrorToastMessage("There was an issue saving to the database, Please try again later. (Database rolled back record 5+ times).");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ItemExists(itemToUpdate.ID))
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
                        ModelState.AddModelError("UPC", "Unable to save changes. Remember, you cannot have duplicate UPC numbers.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                    }
                }
                catch (Exception ex)
                {

                    ModelState.AddModelError("", ex.Message.ToString());

                }
            }
            ViewData["CategoryID"] = new SelectList(_context.Category, "Id", "Name", itemToUpdate.CategoryID);
            ViewData["SupplierID"] = new SelectList(_context.Suppliers, "ID", "Name", itemToUpdate.SupplierID);
            return View(itemToUpdate);
        }
        // GET: Items/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Items == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .Include(i => i.Inventories).ThenInclude(i => i.Location)
                .Include(i => i.ItemThumbNail)
                .Include(i => i.ItemImages)
                .Include(i => i.Employee)
                .Include(i => i.Supplier)
                .Include(i => i.Category)
                .Include(i => i.ItemLocations).ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (item == null)
            {
                return NotFound();
            }
            return View(item);
        }

        // POST: Items/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {

            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (_context.Items == null)
            {
                return Problem("Entity set 'CAAContext.Items'  is null.");
            }
            var item = await _context.Items.FindAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            try
            {
                //_context.Add(archive);
                //_context.Items.Remove(item);
                item.Archived = true;
                await _context.SaveChangesAudit();
                _toastNotification.AddSuccessToastMessage("Item record Archived. <br/> <a href='/Archives/'>View Item Archives.</a>");

                // return RedirectToAction(nameof(Index));
                //return Redirect(ViewData["returnURL"].ToString());
                return RedirectToAction("Index", "Items");

            }
            catch (DbUpdateException)
            {
                //Note: there is really no reason a delete should fail if you can "talk" to the database.
                ModelState.AddModelError("", "Unable to delete record. Try again, and if the problem persists see your system administrator.");
            }
            return View(item);

        }



        public static string GenerateBarcodeSvg(long upc)
        {
            var upcString = upc.ToString();

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
            return svgContent.Content;
        }


        public IActionResult GetPrintableItems([FromQuery] List<long> upcCodes)
        {
            IQueryable<Item> itemsQuery = _context.Items
                .Include(p => p.Inventories).ThenInclude(p => p.Location)
                .Include(i => i.Category)
                .Include(i => i.Supplier)
                .Include(i => i.Employee)
                .Include(p => p.ItemThumbNail)
                .Include(i => i.ItemLocations).ThenInclude(i => i.Location);

            if (upcCodes.Count > 0)
            {
                itemsQuery = itemsQuery.Where(i => upcCodes.Contains(i.UPC));
            }

            var items = itemsQuery
                .Where(i => !i.Archived) // exclude archived items
                .OrderBy(i => i.Name) // sort items by Name in ascending order
                .Select(i => new PrintableViewModel
                {
                    ID = i.ID,
                    Name = i.Name,
                    UPC = i.UPC,
                    Quantity = i.Inventories.Sum(inv => inv.Quantity), // Calculate the total quantity for each item
                    BarcodeSvg = GenerateBarcodeSvg(i.UPC)
                }).ToList();

            if (upcCodes == null || upcCodes.Count == 0)
            {
                return View("GetPrintableItems", items);
            }
            else
            {
                return PartialView("_PrintableItems", items);
            }
        }


        public IActionResult PrintableItems()
        {
            var partialViewResult = GetPrintableItems(null) as PartialViewResult;
            return View("PrintableItems", partialViewResult.ViewData.Model);
        }

        [HttpGet]
        public IActionResult GetItemIDByUPC(string upc)
        {
            if (string.IsNullOrWhiteSpace(upc))
            {
                return BadRequest("Invalid UPC.");
            }

            long upcAsLong;
            if (!long.TryParse(upc, out upcAsLong))
            {
                return BadRequest("Invalid UPC.");
            }

            var item = _context.Items.FirstOrDefault(i => i.UPC == upcAsLong);
            if (item == null)
            {
                return NotFound();
            }

            return Ok(item.ID);
        }



        //for full audit
        public async Task<PartialViewResult> ItemAuditHistory(int id)
        {
            const string primaryEntity = "Item";
            //Get audit data 
            string pkFilter = "\"ID\":" + id.ToString();
            string fKFilter = "\"" + primaryEntity + "ID\":" + id.ToString();
            var audits = await _context.AuditLogs
                .Where(a => (a.EntityName == primaryEntity && a.PrimaryKey.Contains(pkFilter))
                        || a.PrimaryKey.Contains(fKFilter)
                        || a.ForeignKeys.Contains(fKFilter)
                        || a.OldValues.Contains(fKFilter)
                        || a.NewValues.Contains(fKFilter))
                .ToListAsync();

            List<AuditRecordVM> auditRecords = new List<AuditRecordVM>();
            if (audits.Count > 0)
            {
                foreach (var a in audits)
                {
                    AuditRecordVM ar = a.ToAuditRecord();

                    //Get the collection of keys
                    Dictionary<string, string> primaryKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(a.PrimaryKey + String.Empty);
                    //Get the collection of foreign keys
                    Dictionary<string, string> foreignKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(a.ForeignKeys + String.Empty);

                    if (ar.Type == "Updated")
                    {
                        //Here is where we will handle changes to any "loaded" entities related to
                        //the primary entity that have properties to track.  You will need a "if" for each one.

                        if (ar.Entity == primaryEntity)//Audit changes to the actual primary entity
                        {
                            ar.Type += " " + primaryEntity;
                            //Update to the primary entity so lookup all the foreign keys 
                            //Go and get each "lookup" Value.  Only one in this case.
                            foreach (var value in ar.AuditValues)
                            {
                                if (value.PropertyName == "CategoryID")
                                {
                                    Category category = await _context.Categories.FindAsync(int.Parse(value.OldValue));
                                    value.OldValue = (category != null) ? category.Name : "Deleted Category";
                                    category = await _context.Categories.FindAsync(int.Parse(value.NewValue));
                                    value.NewValue = (category != null) ? category.Name : "Deleted Category";
                                    value.PropertyName = "Category";
                                }
                                else if (value.PropertyName == "SupplierID")
                                {
                                    Supplier supplier = await _context.Suppliers.FindAsync(int.Parse(value.OldValue));
                                    value.OldValue = (supplier != null) ? supplier.Name : "Deleted Supplier";
                                    supplier = await _context.Suppliers.FindAsync(int.Parse(value.NewValue));
                                    value.NewValue = (supplier != null) ? supplier.Name : "Deleted Supplier";
                                    value.PropertyName = "Supplier";
                                }
                                else if (value.PropertyName == "EmployeeID")
                                {
                                    Employee empployee = await _context.Employees.FindAsync(int.Parse(value.OldValue));
                                    value.OldValue = (empployee != null) ? empployee.FullName : "Deleted Employee";
                                    empployee = await _context.Employees.FindAsync(int.Parse(value.NewValue));
                                    value.NewValue = (empployee != null) ? empployee.FullName : "Deleted Employee";
                                    value.PropertyName = "Employee";
                                }
                            }
                        }
                        else if (ar.Entity == "InventoryTransfer")//Audit changes to the related Item Reservation
                        {
                            //This is a design decision.  We are going to report all changes to
                            //a item on the reservation.
                            //You might decide to leave that to the item reservations audit history.

                            ////Go and get each "lookup" Value to add to the Type
                            Location fromLocation = await _context.Locations.FindAsync(int.Parse(foreignKeys["FromLocationId"]));
                            ar.Type += (fromLocation != null) ? " Location: " + fromLocation.InventoryTransfersFrom : " Deleted Location";
                            Location toLocation = await _context.Locations.FindAsync(int.Parse(foreignKeys["ToLocationId"]));
                            ar.Type += (toLocation != null) ? " Location: " + toLocation.InventoryTransfersTo : " Deleted Location";
                            Transfer transfer = await _context.Transfers.FindAsync(int.Parse(foreignKeys["TransferId"]));
                            ar.Type += (transfer != null) ? " Transfer: " + transfer.Title : " Deleted Transfer";
                            Item item = await _context.Items.FindAsync(int.Parse(foreignKeys["ItemId"]));
                            ar.Type += (item != null) ? " Item: " + item.Name + ")" : " Deleted Item";
                        }
                        else if (ar.Entity == "Inventory")//Audit changes to the related Item Reservation
                        {
                            //This is a design decision.  We are going to report all changes to
                            //a item on the reservation.
                            //You might decide to leave that to the item reservations audit history.

                            ////Go and get each "lookup" Value to add to the Type
                            Location location = await _context.Locations.FindAsync(int.Parse(foreignKeys["LocationID"]));
                            ar.Type += (location != null) ? " Location: " + location.Name : " Deleted Location";
                            Item item = await _context.Items.FindAsync(int.Parse(foreignKeys["ItemID"]));
                            ar.Type += (item != null) ? " Item: " + item.Name + ")" : " Deleted Item";
                        }
                        else if (ar.Entity == "ItemReservation")//Audit changes to the related Item Reservation
                        {
                            //This is a design decision.  We are going to report all changes to
                            //a item on the reservation.
                            //You might decide to leave that to the item reservations audit history.

                            //Go and get each "lookup" Value to add to the Type
                            Event events = await _context.Events.FindAsync(int.Parse(foreignKeys["EventId"]));
                            ar.Type += (events != null) ? " Event: " + events.Name : " Deleted Event";
                            Location location = await _context.Locations.FindAsync(int.Parse(foreignKeys["LocationID"]));
                            ar.Type += (location != null) ? " Location: " + location.Name : " Deleted Location";
                            Item item = await _context.Items.FindAsync(int.Parse(foreignKeys["ItemId"]));
                            ar.Type += (item != null) ? " Item: " + item.Name + ")" : " Deleted Item";
                        }
                        else if (ar.Entity == "MissingItemLog")//Audit changes to the related Item Reservation
                        {
                            //This is a design decision.  We are going to report all changes to
                            //a item on the reservation.
                            //You might decide to leave that to the item reservations audit history.

                            ////Go and get each "lookup" Value to add to the Type
                            Event events = await _context.Events.FindAsync(int.Parse(foreignKeys["EventId"]));
                            ar.Type += (events != null) ? " Event: " + events.Name : " Deleted Event";
                            Location location = await _context.Locations.FindAsync(int.Parse(foreignKeys["LocationID"]));
                            ar.Type += (location != null) ? " Location: " + location.Name : " Deleted Location";
                            Item item = await _context.Items.FindAsync(int.Parse(foreignKeys["ItemId"]));
                            ar.Type += (item != null) ? " Item: " + item.Name + ")" : " Deleted Item";
                            Employee emplyee = await _context.Employees.FindAsync(int.Parse(foreignKeys["EmployeeID"]));
                            ar.Type += (emplyee != null) ? " Employee: " + emplyee.FullName + ")" : " Deleted Employee";
                        }
                        else if (ar.Entity == "MissingTransitItem")//Audit changes to the related Item Reservation
                        {
                            //This is a design decision.  We are going to report all changes to
                            //a item on the reservation.
                            //You might decide to leave that to the item reservations audit history.

                            ////Go and get each "lookup" Value to add to the Type
                            //Event events = await _context.Events.FindAsync(int.Parse(primaryKeys["EventId"]));
                            //ar.Type += (events != null) ? " Event: " + events.Name : " Deleted Event";
                            Location fromLocation = await _context.Locations.FindAsync(int.Parse(foreignKeys["FromLocationID"]));
                            ar.Type += (fromLocation != null) ? " Location: " + fromLocation.InventoryTransfersFrom : " Deleted Location";
                            Location toLocation = await _context.Locations.FindAsync(int.Parse(foreignKeys["ToLocationID"]));
                            ar.Type += (toLocation != null) ? " Location: " + toLocation.InventoryTransfersTo : " Deleted Location";
                            Transfer transfer = await _context.Transfers.FindAsync(int.Parse(foreignKeys["TransferID"]));
                            ar.Type += (transfer != null) ? " Transfer: " + transfer.Title : " Deleted Transfer";
                            Item item = await _context.Items.FindAsync(int.Parse(foreignKeys["ItemId"]));
                            ar.Type += (item != null) ? " Item: " + item.Name + ")" : " Deleted Item";
                        }
                        else if (ar.Entity == "Receiving")//Audit changes to the related Item Reservation
                        {
                            //This is a design decision.  We are going to report all changes to
                            //a item on the reservation.
                            //You might decide to leave that to the item reservations audit history.

                            ////Go and get each "lookup" Value to add to the Type
                            Location location = await _context.Locations.FindAsync(int.Parse(foreignKeys["LocationID"]));
                            ar.Type += (location != null) ? " Location: " + location.Name : " Deleted Location";
                            Item item = await _context.Items.FindAsync(int.Parse(foreignKeys["ItemID"]));
                            ar.Type += (item != null) ? " Item: " + item.Name + ")" : " Deleted Item";
                        }
                        else if (ar.Entity == "ItemLocation")//Audit changes to the related Item Reservation
                        {
                            //This is a design decision.  We are going to report all changes to
                            //a item on the reservation.
                            //You might decide to leave that to the item reservations audit history.

                            ////Go and get each "lookup" Value to add to the Type
                            Location location = await _context.Locations.FindAsync(int.Parse(foreignKeys["LocationID"]));
                            ar.Type += (location != null) ? " Location: " + location.Name : " Deleted Location";
                            Item item = await _context.Items.FindAsync(int.Parse(foreignKeys["ItemID"]));
                            ar.Type += (item != null) ? " Item: " + item.Name + ")" : " Deleted Item";
                        }
                    }
                    else if (ar.Type == "Added" || ar.Type == "Removed")
                    {
                        //In this section we will handle when entities are added or 
                        //removed in relation to the primary entity.

                        //Get the values from either Old or New
                        //Note: adding String.Empty prevents null
                        string values = ar.Type == "Added" ? a.NewValues + String.Empty : a.OldValues + String.Empty;
                        //Get the collection of values of the association entity
                        Dictionary<string, string> allValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(values + String.Empty);

                        //Modify the Type of audit and include some details about the related object
                        string newComment = "";
                        //Check to see if it is an uploaded document and show the name
                        //if (ar.Entity == "ItemDocument")
                        //{
                        //    var auditValue = allValues["File Name"];
                        //    if (!String.IsNullOrEmpty(auditValue))
                        //    {
                        //        newComment += " Document (" + auditValue + ")";
                        //    }
                        //}
                        if (ar.Entity == "ItemImages")
                        {
                            newComment += " Item Photo";
                        }
                        else if (ar.Entity == "ItemThumbNail")
                        {
                            newComment += " Item Photo Thumbnail";
                        }
                        //Also audit Inventory Transfer.
                        else if (ar.Entity == "InventoryTransfer")
                        {
                            if (int.TryParse(allValues["FromLocationId"]?.ToString(), out int fromLocationID))
                            {
                                Location fromLoc = await _context.Locations.FindAsync(fromLocationID);
                                newComment += (fromLoc != null) ? " Inventory (From Location: " + fromLoc.InventoryTransfersFrom : " Item (Deleted Location";
                            }
                            if (int.TryParse(allValues["ToLocationId"]?.ToString(), out int toLocationID))
                            {
                                Location toLoc = await _context.Locations.FindAsync(toLocationID);
                                newComment += (toLoc != null) ? " Inventory (To Location: " + toLoc.InventoryTransfersTo : " Item (Deleted Location";
                            }
                            if (int.TryParse(allValues["TransferId"]?.ToString(), out int transferID))
                            {
                                Transfer t = await _context.Transfers.FindAsync(transferID);
                                newComment += (t != null) ? " Inventory (Transfer: " + t.Title : " Item (Deleted Transfer";
                            }
                            if (int.TryParse(allValues["ItemId"]?.ToString(), out int itemID))
                            {
                                Item i = await _context.Items.FindAsync(itemID);
                                string itemName = i.Name;
                                newComment += (i != null) ? ", Item: " + itemName + ")" : ", Deleted Item)";
                            }
                        }
                        //Also audit Inventory.
                        else if (ar.Entity == "Inventory")
                        {
                            if (int.TryParse(allValues["LocationID"]?.ToString(), out int locationID))
                            {
                                Location l = await _context.Locations.FindAsync(locationID);
                                newComment += (l != null) ? " Inventory (Location: " + l.Name : " Item (Deleted Location";
                            }
                            if (int.TryParse(allValues["ItemID"]?.ToString(), out int itemID))
                            {
                                Item i = await _context.Items.FindAsync(itemID);
                                string itemName = i.Name;
                                newComment += (i != null) ? ", Item: " + itemName + ")" : ", Deleted Item)";
                            }
                        }
                        //Also audit ItemReservations.
                        else if (ar.Entity == "ItemReservation")
                        {
                            if (int.TryParse(allValues["EventId"]?.ToString(), out int eventID))
                            {
                                Event e = await _context.Events.FindAsync(eventID);
                                newComment += (e != null) ? " Item (Event: " + e.Name : " Item (Deleted Event";
                            }
                            if (int.TryParse(allValues["ItemId"]?.ToString(), out int itemID))
                            {
                                Item i = await _context.Items.FindAsync(itemID);
                                string itemName = i.Name;
                                newComment += (i != null) ? ", Item: " + itemName + ")" : ", Deleted Item)";
                            }
                            if (int.TryParse(allValues["LocationID"]?.ToString(), out int locationID))
                            {
                                Location l = await _context.Locations.FindAsync(locationID);
                                newComment += (l != null) ? " Item (Location: " + l.Name : " Item (Deleted Location";
                            }
                        }
                        //Also audit Missing Item Logs.
                        else if (ar.Entity == "MissingItemLog")
                        {
                            if (int.TryParse(allValues["EventId"]?.ToString(), out int eventID))
                            {
                                Event e = await _context.Events.FindAsync(eventID);
                                newComment += (e != null) ? " Item (Event: " + e.Name : " Item (Deleted Event";
                            }
                            if (int.TryParse(allValues["ItemId"]?.ToString(), out int itemID))
                            {
                                Item i = await _context.Items.FindAsync(itemID);
                                string itemName = i.Name;
                                newComment += (i != null) ? ", Item: " + itemName + ")" : ", Deleted Item)";
                            }
                            if (int.TryParse(allValues["LocationID"]?.ToString(), out int locationID))
                            {
                                Location l = await _context.Locations.FindAsync(locationID);
                                newComment += (l != null) ? " Item (Location: " + l.Name : " Item (Deleted Location";
                            }
                            if (int.TryParse(allValues["EmployeeID"]?.ToString(), out int employeeID))
                            {
                                Employee e = await _context.Employees.FindAsync(employeeID);
                                newComment += (e != null) ? " Item (Employee: " + e.FullName : " Item (Deleted Employee";
                            }
                        }
                        //Also audit Missing Transit Items.
                        else if (ar.Entity == "MissingTransitItem")
                        {
                            if (int.TryParse(allValues["FromLocationID"]?.ToString(), out int fromLocationID))
                            {
                                Location fromLoc = await _context.Locations.FindAsync(fromLocationID);
                                newComment += (fromLoc != null) ? " Inventory (From Location: " + fromLoc.InventoryTransfersFrom : " Item (Deleted Location";
                            }
                            if (int.TryParse(allValues["ToLocationID"]?.ToString(), out int toLocationID))
                            {
                                Location toLoc = await _context.Locations.FindAsync(toLocationID);
                                newComment += (toLoc != null) ? " Inventory (To Location: " + toLoc.InventoryTransfersTo : " Item (Deleted Location";
                            }
                            if (int.TryParse(allValues["EmployeeID"]?.ToString(), out int employeeID))
                            {
                                Employee e = await _context.Employees.FindAsync(employeeID);
                                newComment += (e != null) ? " Item (Employee: " + e.FullName : " Item (Deleted Employee";
                            }
                            if (int.TryParse(allValues["ItemID"]?.ToString(), out int itemID))
                            {
                                Item i = await _context.Items.FindAsync(itemID);
                                string itemName = i.Name;
                                newComment += (i != null) ? ", Item: " + itemName + ")" : ", Deleted Item)";
                            }
                        }
                        //Also audit Orders/Receiving.
                        else if (ar.Entity == "Receiving")
                        {
                            if (int.TryParse(allValues["LocationID"]?.ToString(), out int locationID))
                            {
                                Location l = await _context.Locations.FindAsync(locationID);
                                newComment += (l != null) ? " Inventory (Location: " + l.Name : " Item (Deleted Location";
                            }
                            if (int.TryParse(allValues["ItemID"]?.ToString(), out int itemID))
                            {
                                Item i = await _context.Items.FindAsync(itemID);
                                string itemName = i.Name;
                                newComment += (i != null) ? ", Item: " + itemName + ")" : ", Deleted Item)";
                            }
                        }
                        //Also audit Item Locations.
                        else if (ar.Entity == "ItemLocation")
                        {
                            if (int.TryParse(allValues["LocationID"]?.ToString(), out int locationID))
                            {
                                Location l = await _context.Locations.FindAsync(locationID);
                                newComment += (l != null) ? " Inventory (Location: " + l.Name : " Item (Deleted Location";
                            }
                            if (int.TryParse(allValues["ItemID"]?.ToString(), out int itemID))
                            {
                                Item i = await _context.Items.FindAsync(itemID);
                                string itemName = i.Name;
                                newComment += (i != null) ? ", Item: " + itemName + ")" : ", Deleted Item)";
                            }
                        }
                        ar.Type += " " + newComment;
                    }
                    auditRecords.Add(ar);
                }
            }
            return PartialView("_AuditHistory", auditRecords.OrderByDescending(a => a.DateTime));
        }


        //For Adding Supplier
        [HttpGet]
        public JsonResult GetSuppliers(int? id)
        {
            return Json(SupplierSelectList(id));
        }
        //For Adding Category
        [HttpGet]
        public JsonResult GetCategories(int? id)
        {
            return Json(CategorySelectList(id));
        }
        //For Adding Supplier
        private SelectList SupplierSelectList(int? selectedId)
        {
            return new SelectList(_context
                .Suppliers
                .OrderBy(c => c.Name), "ID", "Name", selectedId);
        }
        //For Adding Category
        private SelectList CategorySelectList(int? selectedId)
        {
            return new SelectList(_context
                .Categories
                .OrderBy(c => c.Name), "Id", "Name", selectedId);
        }


        private string ControllerName()
        {
            return this.ControllerContext.RouteData.Values["controller"].ToString();
        }
        private void ViewDataReturnURL()
        {
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, ControllerName());
        }
        private bool ItemExists(int id)
        {
            return _context.Items.Any(e => e.ID == id);
        }
        private async Task AddPicture(Item item, IFormFile thePicture)
        {
            //Get the picture and save it with the item (2 sizes)
            if (thePicture != null)
            {
                string mimeType = thePicture.ContentType;
                long fileLength = thePicture.Length;
                if (!(mimeType == "" || fileLength == 0))//Looks like we have a file!!!
                {
                    if (mimeType.Contains("image"))
                    {
                        using var memoryStream = new MemoryStream();
                        await thePicture.CopyToAsync(memoryStream);
                        var pictureArray = memoryStream.ToArray();//Gives us the Byte[]

                        //Check if we are replacing or creating new
                        if (item.ItemImages != null)
                        {
                            //We already have pictures so just replace the Byte[]
                            item.ItemImages.Content = ResizeImage.shrinkImageWebp(pictureArray, 500, 600);

                            //Get the Thumbnail so we can update it.  Remember we didn't include it
                            item.ItemThumbNail = _context.ItemThumbNails.Where(p => p.ItemID == item.ID).FirstOrDefault();
                            item.ItemThumbNail.Content = ResizeImage.shrinkImageWebp(pictureArray, 100, 120);
                        }
                        else //No pictures saved so start new
                        {
                            item.ItemImages = new ItemImages
                            {
                                Content = ResizeImage.shrinkImageWebp(pictureArray, 500, 600),
                                MimeType = "image/webp"
                            };
                            item.ItemThumbNail = new ItemThumbNail
                            {
                                Content = ResizeImage.shrinkImageWebp(pictureArray, 100, 120),
                                MimeType = "image/webp"
                            };
                        }
                    }
                }
            }
        }

        private void PopulateAssignedLocationData(Item item)
        {
            //For this to work, you must have Included the itemLocations 
            //in the item
            var allOptions = _context.Locations;
            var currentOptionIDs = new HashSet<int>(item.ItemLocations.Select(b => b.LocationID));
            var checkBoxes = new List<CheckOptionsManyToManyVM>();
            foreach (var locationoption in allOptions)
            {
                checkBoxes.Add(new CheckOptionsManyToManyVM
                {
                    ID = locationoption.Id,
                    DisplayText = locationoption.Name,
                    Assigned = currentOptionIDs.Contains(locationoption.Id)
                });
            }
            ViewData["LocationOptions"] = checkBoxes;
        }
        private void UpdateItemLocations(string[] selectedOptions, Item itemToUpdate)
        {
            int locationCount = 0;
            string LocationName = "";
            if (selectedOptions == null)
            {
                itemToUpdate.ItemLocations = new List<ItemLocation>();
                return;
            }

            var selectedOptionsHS = new HashSet<string>(selectedOptions);
            var itemOptionsHS = new HashSet<int>
                (itemToUpdate.ItemLocations.Select(c => c.LocationID));//IDs of the currently selected Locations
            foreach (var locationoption in _context.Locations)
            {
                if (selectedOptionsHS.Contains(locationoption.Id.ToString())) //It is checked
                {
                    if (!itemOptionsHS.Contains(locationoption.Id))  //but not currently in the history
                    {
                        locationCount++;
                        var location = _context.Locations.FirstOrDefault(c => c.Id == locationoption.Id);
                        if (location != null)
                        {
                            LocationName += location.Name + ", ";
                        }
                        itemToUpdate.ItemLocations.Add(new ItemLocation { ItemID = itemToUpdate.ID, LocationID = locationoption.Id });
                    }
                }
                else
                {
                    //Checkbox Not checked
                    if (itemOptionsHS.Contains(locationoption.Id)) //but it is currently in the history - so remove it
                    {
                        ItemLocation conditionToRemove = itemToUpdate.ItemLocations.SingleOrDefault(c => c.LocationID == locationoption.Id);
                        _context.Remove(conditionToRemove);
                    }
                }

            }
            HttpContext.Session.SetString("LocationNames", LocationName);
            HttpContext.Session.SetString("NumOfLocationsSelected", locationCount.ToString());

        }

        private void CreatingInventoryLocations(string[] selectedOptions, Inventory invToCreate, int ItemID)
        {


        }






    }
}
