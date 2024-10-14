using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CAAMarketing.Data;
using CAAMarketing.Models;

namespace CAAMarketing.Controllers
{
    public class MissingTransitItemsController : Controller
    {
        private readonly CAAContext _context;

        public MissingTransitItemsController(CAAContext context)
        {
            _context = context;
        }

        // GET: MissingTransitItems
        public async Task<IActionResult> Index()
        {
            var cAAContext = _context.MissingTransitItems.Include(m => m.Employee).Include(m => m.FromLocation).Include(m => m.Item).Include(m => m.ToLocation);
            return View(await cAAContext.ToListAsync());
        }

        // GET: MissingTransitItems/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.MissingTransitItems == null)
            {
                return NotFound();
            }

            var missingTransitItem = await _context.MissingTransitItems
                .Include(m => m.Employee)
                .Include(m => m.FromLocation)
                .Include(m => m.Item)
                .Include(m => m.ToLocation)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (missingTransitItem == null)
            {
                return NotFound();
            }

            return View(missingTransitItem);
        }

        // GET: MissingTransitItems/Create
        public IActionResult Create()
        {
            ViewData["EmployeeID"] = new SelectList(_context.Employees, "ID", "Email");
            ViewData["FromLocationID"] = new SelectList(_context.Locations, "Id", "Address");
            ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name");
            ViewData["ToLocationID"] = new SelectList(_context.Locations, "Id", "Address");
            return View();
        }

        // POST: MissingTransitItems/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Reason,Notes,Date,Quantity,ItemId,FromLocationID,ToLocationID,EmployeeID")] MissingTransitItem missingTransitItem)
        {
            if (ModelState.IsValid)
            {
                _context.Add(missingTransitItem);
                await _context.SaveChangesAudit();
                return RedirectToAction(nameof(Index));
            }
            ViewData["EmployeeID"] = new SelectList(_context.Employees, "ID", "Email", missingTransitItem.EmployeeID);
            ViewData["FromLocationID"] = new SelectList(_context.Locations, "Id", "Address", missingTransitItem.FromLocationID);
            ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name", missingTransitItem.ItemId);
            ViewData["ToLocationID"] = new SelectList(_context.Locations, "Id", "Address", missingTransitItem.ToLocationID);
            return View(missingTransitItem);
        }

        // GET: MissingTransitItems/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.MissingTransitItems == null)
            {
                return NotFound();
            }

            var missingTransitItem = await _context.MissingTransitItems.FindAsync(id);
            if (missingTransitItem == null)
            {
                return NotFound();
            }
            ViewData["EmployeeID"] = new SelectList(_context.Employees, "ID", "Email", missingTransitItem.EmployeeID);
            ViewData["FromLocationID"] = new SelectList(_context.Locations, "Id", "Address", missingTransitItem.FromLocationID);
            ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name", missingTransitItem.ItemId);
            ViewData["ToLocationID"] = new SelectList(_context.Locations, "Id", "Address", missingTransitItem.ToLocationID);
            return View(missingTransitItem);
        }

        // POST: MissingTransitItems/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Reason,Notes,Date,Quantity,ItemId,FromLocationID,ToLocationID,EmployeeID")] MissingTransitItem missingTransitItem)
        {
            if (id != missingTransitItem.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(missingTransitItem);
                    await _context.SaveChangesAudit();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MissingTransitItemExists(missingTransitItem.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["EmployeeID"] = new SelectList(_context.Employees, "ID", "Email", missingTransitItem.EmployeeID);
            ViewData["FromLocationID"] = new SelectList(_context.Locations, "Id", "Address", missingTransitItem.FromLocationID);
            ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name", missingTransitItem.ItemId);
            ViewData["ToLocationID"] = new SelectList(_context.Locations, "Id", "Address", missingTransitItem.ToLocationID);
            return View(missingTransitItem);
        }

        // GET: MissingTransitItems/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.MissingTransitItems == null)
            {
                return NotFound();
            }

            var missingTransitItem = await _context.MissingTransitItems
                .Include(m => m.Employee)
                .Include(m => m.FromLocation)
                .Include(m => m.Item)
                .Include(m => m.ToLocation)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (missingTransitItem == null)
            {
                return NotFound();
            }

            return View(missingTransitItem);
        }

        // POST: MissingTransitItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.MissingTransitItems == null)
            {
                return Problem("Entity set 'CAAContext.MissingTransitItems'  is null.");
            }
            var missingTransitItem = await _context.MissingTransitItems.FindAsync(id);
            if (missingTransitItem != null)
            {
                _context.MissingTransitItems.Remove(missingTransitItem);
            }
            
            await _context.SaveChangesAudit();
            return RedirectToAction(nameof(Index));
        }

        private bool MissingTransitItemExists(int id)
        {
          return _context.MissingTransitItems.Any(e => e.ID == id);
        }
    }
}
