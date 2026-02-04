using GymBro.Core;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Web.Controllers
{
    public class SupplierController : BaseAdminController
    {
        private readonly GymBroDbContext _context;
        public SupplierController(GymBroDbContext context) { _context = context; }

        public async Task<IActionResult> Index() => View(await _context.Suppliers.ToListAsync());

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            if (ModelState.IsValid) { _context.Add(supplier); await _context.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
            return View(supplier);
        }
        // Edit/Delete tương tự
    }
}