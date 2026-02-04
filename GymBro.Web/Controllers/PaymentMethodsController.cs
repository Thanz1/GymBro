using GymBro.Core;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Web.Controllers
{
    public class PaymentMethodsController : BaseAdminController
    {
        private readonly GymBroDbContext _context;
        public PaymentMethodsController(GymBroDbContext context) { _context = context; }

        public async Task<IActionResult> Index() => View(await _context.PaymentMethods.ToListAsync());

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(PaymentMethod method)
        {
            if (ModelState.IsValid) { _context.Add(method); await _context.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
            return View(method);
        }

        // Edit/Delete tương tự các controller khác
    }
}