using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FEENALOoFINALE.Data;
using FEENALOoFINALE.Models;
using Microsoft.AspNetCore.Authorization;

namespace FEENALOoFINALE.Controllers
{
    [Authorize]
    public class FailurePredictionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FailurePredictionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: FailurePrediction
        public async Task<IActionResult> Index()
        {
            return View(await _context.FailurePredictions
                .Include(f => f.Equipment)
                .OrderByDescending(f => f.PredictedFailureDate)
                .ToListAsync());
        }

        // GET: FailurePrediction/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prediction = await _context.FailurePredictions
                .Include(f => f.Equipment)
                .FirstOrDefaultAsync(m => m.PredictionId == id);

            if (prediction == null)
            {
                return NotFound();
            }

            return View(prediction);
        }

        // GET: FailurePrediction/Create
        public IActionResult Create()
        {
            ViewBag.Equipment = _context.Equipment.ToList();
            return View();
        }

        // POST: FailurePrediction/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EquipmentId,PredictedFailureDate,ConfidenceLevel,Status,AnalysisNotes,ContributingFactors")] FailurePrediction prediction)
        {
            if (ModelState.IsValid)
            {
                prediction.CreatedDate = DateTime.Now;
                _context.Add(prediction);
                await _context.SaveChangesAsync();

                // Create an alert if prediction status is High
                if (prediction.Status == PredictionStatus.High)
                {
                    var alert = new Alert
                    {
                        EquipmentId = prediction.EquipmentId,
                        Title = "High Risk of Failure",
                        Description = $"Equipment predicted to fail on {prediction.PredictedFailureDate.ToShortDateString()} with {prediction.ConfidenceLevel}% confidence",
                        Priority = AlertPriority.High,
                        CreatedDate = DateTime.Now,
                        Status = AlertStatus.Open
                    };
                    _context.Alerts.Add(alert);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }
            ViewBag.Equipment = _context.Equipment.ToList();
            return View(prediction);
        }

        // GET: FailurePrediction/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prediction = await _context.FailurePredictions.FindAsync(id);
            if (prediction == null)
            {
                return NotFound();
            }
            ViewBag.Equipment = _context.Equipment.ToList();
            return View(prediction);
        }

        // POST: FailurePrediction/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PredictionId,EquipmentId,PredictedFailureDate,ConfidenceLevel,Status,AnalysisNotes,ContributingFactors,CreatedDate")] FailurePrediction prediction)
        {
            if (id != prediction.PredictionId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(prediction);
                    await _context.SaveChangesAsync();

                    // Update or create alert based on status
                    if (prediction.Status == PredictionStatus.High)
                    {
                        var existingAlert = await _context.Alerts
                            .FirstOrDefaultAsync(a => a.EquipmentId == prediction.EquipmentId && 
                                                    a.Title == "High Risk of Failure" &&
                                                    a.Status != AlertStatus.Closed);

                        if (existingAlert == null)
                        {
                            var alert = new Alert
                            {
                                EquipmentId = prediction.EquipmentId,
                                Title = "High Risk of Failure",
                                Description = $"Equipment predicted to fail on {prediction.PredictedFailureDate.ToShortDateString()} with {prediction.ConfidenceLevel}% confidence",
                                Priority = AlertPriority.High,
                                CreatedDate = DateTime.Now,
                                Status = AlertStatus.Open
                            };
                            _context.Alerts.Add(alert);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FailurePredictionExists(prediction.PredictionId))
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
            ViewBag.Equipment = _context.Equipment.ToList();
            return View(prediction);
        }

        // GET: FailurePrediction/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prediction = await _context.FailurePredictions
                .Include(f => f.Equipment)
                .FirstOrDefaultAsync(m => m.PredictionId == id);
            if (prediction == null)
            {
                return NotFound();
            }

            return View(prediction);
        }

        // POST: FailurePrediction/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var prediction = await _context.FailurePredictions.FindAsync(id);
            if (prediction != null)
            {
                _context.FailurePredictions.Remove(prediction);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: FailurePrediction/ByEquipment/5
        public async Task<IActionResult> ByEquipment(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var predictions = await _context.FailurePredictions
                .Include(f => f.Equipment)
                .Where(f => f.EquipmentId == id)
                .OrderByDescending(f => f.PredictedFailureDate)
                .ToListAsync();

            ViewBag.Equipment = await _context.Equipment.FindAsync(id);
            return View(predictions);
        }

        private bool FailurePredictionExists(int id)
        {
            return _context.FailurePredictions.Any(e => e.PredictionId == id);
        }
    }
}