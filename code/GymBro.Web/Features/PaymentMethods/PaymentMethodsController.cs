using GymBro.Application.Payments;
using GymBro.Core;
using Microsoft.AspNetCore.Mvc;

namespace GymBro.Web.Controllers
{
    public class PaymentMethodsController : BaseAdminController
    {
        private readonly IPaymentMethodAdminService _paymentMethodAdminService;

        public PaymentMethodsController(IPaymentMethodAdminService paymentMethodAdminService)
        {
            _paymentMethodAdminService = paymentMethodAdminService;
        }

        public async Task<IActionResult> Index()
        {
            var methods = await _paymentMethodAdminService.GetListAsync();
            return View(methods);
        }

        public IActionResult Create()
        {
            return View(new PaymentMethod
            {
                IsActive = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PaymentMethod method)
        {
            var result = await _paymentMethodAdminService.CreateAsync(new PaymentMethodUpsertRequest
            {
                MethodName = method.MethodName,
                Description = method.Description,
                IsActive = method.IsActive
            });

            if (!result.Succeeded)
            {
                AddModelErrors(result.Errors);
                return View(method);
            }

            TempData["SuccessMessage"] = result.Message ?? "Thêm phương thức thanh toán thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!id.HasValue)
            {
                return BadRequest();
            }

            var method = await _paymentMethodAdminService.GetByIdAsync(id.Value);
            return method == null ? NotFound() : View(method);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PaymentMethod method)
        {
            var result = await _paymentMethodAdminService.UpdateAsync(new PaymentMethodUpsertRequest
            {
                Id = method.Id,
                MethodName = method.MethodName,
                Description = method.Description,
                IsActive = method.IsActive
            });

            if (result.NotFound)
            {
                TempData["ErrorMessage"] = result.Message ?? "Không tìm thấy phương thức thanh toán cần cập nhật.";
                return RedirectToAction(nameof(Index));
            }

            if (!result.Succeeded)
            {
                AddModelErrors(result.Errors);
                return View(method);
            }

            TempData["SuccessMessage"] = result.Message ?? "Cập nhật phương thức thanh toán thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (!id.HasValue)
            {
                return BadRequest();
            }

            var method = await _paymentMethodAdminService.GetByIdAsync(id.Value);
            return method == null ? NotFound() : View(method);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _paymentMethodAdminService.DeleteAsync(id);

            if (result.NotFound)
            {
                TempData["ErrorMessage"] = result.Message ?? "Không tìm thấy phương thức thanh toán cần xóa.";
                return RedirectToAction(nameof(Index));
            }

            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        private void AddModelErrors(IReadOnlyDictionary<string, string[]> errors)
        {
            foreach (var error in errors)
            {
                foreach (var message in error.Value)
                {
                    ModelState.AddModelError(error.Key, message);
                }
            }
        }
    }
}
