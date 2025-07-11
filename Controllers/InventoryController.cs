using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FEENALOoFINALE.Data;
using FEENALOoFINALE.Models;
using Microsoft.AspNetCore.Authorization;

namespace FEENALOoFINALE.Controllers
{
    [Authorize]
    public class InventoryController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        // GET: Inventory
        public async Task<IActionResult> Index()
        {
            var items = await _context.InventoryItems
                .Include(i => i.InventoryStocks)
                .ToListAsync();
            return View(items);
        }

        // GET: Inventory/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventoryItem = await _context.InventoryItems
                .Include(i => i.InventoryStocks)
                .Include(i => i.MaintenanceInventoryLinks)
                    .ThenInclude(mil => mil.MaintenanceLog)
                .AsSplitQuery()
                .FirstOrDefaultAsync(i => i.ItemId == id);

            if (inventoryItem == null)
            {
                return NotFound();
            }

            return View(inventoryItem);
        }

        // GET: Inventory/Create
        public IActionResult Create()
        {
            return View(new InventoryViewModel());
        }

        // POST: Inventory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InventoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var item = new InventoryItem
                    {
                        Name = model.Name,
                        Description = model.Description,
                        Category = model.Category,
                        MinimumStockLevel = 10
                    };

                    _context.InventoryItems.Add(item);
                    await _context.SaveChangesAsync();

                    if (model.InitialStock > 0)
                    {
                        var stock = new InventoryStock
                        {
                            ItemId = item.ItemId,
                            Quantity = model.InitialStock,
                            UnitCost = 0,
                            DateReceived = DateTime.Now,
                            BatchNumber = Guid.NewGuid().ToString()
                        };
                        _context.InventoryStocks.Add(stock);
                        await _context.SaveChangesAsync();
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while saving: " + ex.Message);
                }
            }
            await PopulateInventoryDropdownsAsync();
            return View(model);
        }

        // GET: Inventory/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventoryItem = await _context.InventoryItems.FindAsync(id);
            if (inventoryItem == null)
            {
                return NotFound();
            }
            return View(inventoryItem);
        }

        // POST: Inventory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ItemId,Name,Description,Category,MinimumStockLevel")] InventoryItem inventoryItem)
        {
            if (id != inventoryItem.ItemId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(inventoryItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InventoryItemExists(inventoryItem.ItemId))
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
            return View(inventoryItem);
        }

        // GET: Inventory/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventoryItem = await _context.InventoryItems
                .FirstOrDefaultAsync(m => m.ItemId == id);
            if (inventoryItem == null)
            {
                return NotFound();
            }

            return View(inventoryItem);
        }

        // POST: Inventory/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var inventoryItem = await _context.InventoryItems.FindAsync(id);
            if (inventoryItem != null)
            {
                _context.InventoryItems.Remove(inventoryItem);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Inventory/AddStock/5
        public async Task<IActionResult> AddStock(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventoryItem = await _context.InventoryItems.FindAsync(id);
            if (inventoryItem == null)
            {
                return NotFound();
            }

            ViewBag.InventoryItem = inventoryItem;
            return View(new InventoryStock { ItemId = inventoryItem.ItemId });
        }

        // POST: Inventory/AddStock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStock([Bind("ItemId,Quantity,UnitCost,DateReceived,BatchNumber")] InventoryStock stock)
        {
            if (ModelState.IsValid)
            {
                _context.Add(stock);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = stock.ItemId });
            }
            ViewBag.InventoryItem = await _context.InventoryItems.FindAsync(stock.ItemId);
            return View(stock);
        }

        private async Task PopulateInventoryDropdownsAsync(InventoryItem? item = null)
        {
            ViewData["Categories"] = new SelectList(Enum.GetValues(typeof(ItemCategory)));
            if (item != null)
            {
                ViewData["SelectedCategory"] = item.Category;
            }
            await Task.CompletedTask;
        }

        private bool InventoryItemExists(int id)
        {
            return _context.InventoryItems.Any(e => e.ItemId == id);
        }
    }
}
