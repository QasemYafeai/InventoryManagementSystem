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
    public class MissingItemLogsController : Controller
    {
        private readonly CAAContext _context;

        public MissingItemLogsController(CAAContext context)
        {
            _context = context;
        }

        // GET: MissingItemLogs
        public async Task<IActionResult> Index()
        {
            var cAAContext = _context.MissingItemLogs.Include(m => m.Employee).Include(m => m.Event).Include(m => m.Item).Include(m => m.Location);
            
            List<MissingTransitItem> MissingTransits = _context.MissingTransitItems
               .Include(e => e.Item)
               .Include(e => e.FromLocation)
               .Include(e => e.ToLocation)
               .Include(e => e.Employee)
               .AsNoTracking()
               .ToList();

            ViewBag.MissingTransits = MissingTransits;

            return View(await cAAContext.ToListAsync());
        }

        // GET: MissingItemLogs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.MissingItemLogs == null)
            {
                return NotFound();
            }

            var missingItemLog = await _context.MissingItemLogs
                .Include(m => m.Employee)
                .Include(m => m.Event)
                .Include(m => m.Item)
                .Include(m => m.Location)
                .FirstOrDefaultAsync(m => m.ID == id);



            

            if (missingItemLog == null)
            {
                return NotFound();
            }

            return View(missingItemLog);
        }

        // GET: MissingItemLogs/Create
        public IActionResult Create()
        {
            ViewData["EmployeeID"] = new SelectList(_context.Employees, "ID", "Email");
            ViewData["EventId"] = new SelectList(_context.Events, "ID", "Name");
            ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name");
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Address");
            return View();
        }

        // POST: MissingItemLogs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Reason,Notes,Date,Quantity,EventId,ItemId,LocationID,EmployeeID")] MissingItemLog missingItemLog)
        {
            if (ModelState.IsValid)
            {
                _context.Add(missingItemLog);
                await _context.SaveChangesAudit();
                return RedirectToAction(nameof(Index));
            }
            ViewData["EmployeeID"] = new SelectList(_context.Employees, "ID", "Email", missingItemLog.EmployeeID);
            ViewData["EventId"] = new SelectList(_context.Events, "ID", "Name", missingItemLog.EventId);
            ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name", missingItemLog.ItemId);
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Address", missingItemLog.LocationID);
            return View(missingItemLog);
        }

        // GET: MissingItemLogs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.MissingItemLogs == null)
            {
                return NotFound();
            }

            var missingItemLog = await _context.MissingItemLogs.FindAsync(id);
            if (missingItemLog == null)
            {
                return NotFound();
            }
            ViewData["EmployeeID"] = new SelectList(_context.Employees, "ID", "Email", missingItemLog.EmployeeID);
            ViewData["EventId"] = new SelectList(_context.Events, "ID", "Name", missingItemLog.EventId);
            ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name", missingItemLog.ItemId);
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Address", missingItemLog.LocationID);
            return View(missingItemLog);
        }

        // POST: MissingItemLogs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Reason,Notes,Date,Quantity,EventId,ItemId,LocationID,EmployeeID")] MissingItemLog missingItemLog)
        {
            if (id != missingItemLog.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(missingItemLog);
                    await _context.SaveChangesAudit();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MissingItemLogExists(missingItemLog.ID))
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
            ViewData["EmployeeID"] = new SelectList(_context.Employees, "ID", "Email", missingItemLog.EmployeeID);
            ViewData["EventId"] = new SelectList(_context.Events, "ID", "Name", missingItemLog.EventId);
            ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name", missingItemLog.ItemId);
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Address", missingItemLog.LocationID);
            return View(missingItemLog);
        }

        // GET: MissingItemLogs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.MissingItemLogs == null)
            {
                return NotFound();
            }

            var missingItemLog = await _context.MissingItemLogs
                .Include(m => m.Employee)
                .Include(m => m.Event)
                .Include(m => m.Item)
                .Include(m => m.Location)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (missingItemLog == null)
            {
                return NotFound();
            }

            return View(missingItemLog);
        }

        // POST: MissingItemLogs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.MissingItemLogs == null)
            {
                return Problem("Entity set 'CAAContext.MissingItemLogs'  is null.");
            }
            var missingItemLog = await _context.MissingItemLogs.FindAsync(id);
            if (missingItemLog != null)
            {
                _context.MissingItemLogs.Remove(missingItemLog);
            }
            
            await _context.SaveChangesAudit();
            return RedirectToAction(nameof(Index));
        }

        private bool MissingItemLogExists(int id)
        {
          return _context.MissingItemLogs.Any(e => e.ID == id);
        }
    }
}
