using GymBro.Contracts;
using GymBro.Service;
using Microsoft.AspNetCore.Mvc;

namespace GymBro.Web.Controllers
{
    public class PaymentMethodsController : BaseAdminController
    {
        private readonly IPaymentService _paymentService;

        public PaymentMethodsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        // 1. Danh sách phương thức thanh toán
        public async Task<IActionResult> Index()
        {
            var methods = await _paymentService.GetAllPaymentMethodsAsync();
            return View(methods);
        }

        // 2. Thêm mới (GET)
        public IActionResult Create() => View();

        // 3. Thêm mới (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PaymentMethodDto methodDto)
        {
            if (ModelState.IsValid)
            {
                var success = await _paymentService.CreatePaymentMethodAsync(methodDto);
                if (success)
                {
                    TempData["SuccessMessage"] = "Thêm phương thức thanh toán thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            return View(methodDto);
        }

        // 4. Cập nhật (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var method = await _paymentService.GetPaymentMethodByIdAsync(id);
            if (method == null) return NotFound();
            return View(method);
        }

        // 5. Cập nhật (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PaymentMethodDto methodDto)
        {
            if (id != methodDto.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                var success = await _paymentService.UpdatePaymentMethodAsync(id, methodDto);
                if (success)
                {
                    TempData["SuccessMessage"] = "Cập nhật thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            return View(methodDto);
        }

        // 6. Xóa (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _paymentService.DeletePaymentMethodAsync(id);
            if (success) TempData["SuccessMessage"] = "Đã xóa phương thức thanh toán!";
            return RedirectToAction(nameof(Index));
        }
    }
}