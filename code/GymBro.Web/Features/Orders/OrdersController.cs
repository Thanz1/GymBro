using GymBro.Application.Orders;
using GymBro.Core;
using GymBro.Web.Features.Orders;
using GymBro.Web.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace GymBro.Web.Controllers
{
    public class OrdersController : BaseAdminController
    {
        private const int PageSize = 10;

        private readonly IOrderAdminService _orderAdminService;

        public OrdersController(IOrderAdminService orderAdminService)
        {
            _orderAdminService = orderAdminService;
        }

        public async Task<IActionResult> Index(int? page, string? searchString, string? status)
        {
            var result = await _orderAdminService.GetPageAsync(new OrderAdminPageQuery
            {
                PageNumber = page ?? 1,
                PageSize = PageSize,
                SearchString = searchString,
                Status = status
            });

            return View(new OrderManagementIndexViewModel
            {
                Items = result.Items
                    .Select(OrderPresentationFactory.CreateListItem)
                    .ToList(),
                StatusOptions = result.StatusOptions,
                SearchString = result.SearchString,
                CurrentStatus = result.CurrentStatus,
                PageNumber = result.PageNumber,
                TotalPages = result.TotalPages,
                TotalItems = result.TotalItems
            });
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (!id.HasValue)
            {
                return NotFound();
            }

            var order = await _orderAdminService.GetDetailsAsync(id.Value);
            if (order == null)
            {
                return NotFound();
            }

            return View(OrderPresentationFactory.CreateReadOnly(order));
        }

        public IActionResult Create()
        {
            TempData["InfoMessage"] = "Tao don hang thu cong da bi khoa. Hay de don duoc tao tu luong checkout de giu dung ton kho va thanh toan.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ActionName("Create")]
        [ValidateAntiForgeryToken]
        public IActionResult CreatePost()
        {
            TempData["InfoMessage"] = "Tao don hang thu cong da bi khoa. Hay dung luong checkout chuan.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!id.HasValue)
            {
                return NotFound();
            }

            var data = await _orderAdminService.GetEditDataAsync(id.Value);
            if (data == null)
            {
                return NotFound();
            }

            return View(BuildEditViewModel(data));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OrderManagementEditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var result = await _orderAdminService.UpdateStatusAsync(new UpdateOrderStatusRequest
            {
                Id = id,
                Status = model.Status,
                AdminUsername = GetAdminAuditActorName()
            });

            if (result.NotFound)
            {
                return NotFound();
            }

            if (!result.Succeeded)
            {
                AddModelErrors(result.Errors);

                var editData = await _orderAdminService.GetEditDataAsync(id);
                if (editData == null)
                {
                    return NotFound();
                }

                return View(BuildEditViewModel(editData, model.Status));
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (!id.HasValue)
            {
                return NotFound();
            }

            var order = await _orderAdminService.GetDeleteModelAsync(id.Value);
            if (order == null)
            {
                return NotFound();
            }

            return View(OrderPresentationFactory.CreateReadOnly(order));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _orderAdminService.DeleteAsync(id);
            if (result.NotFound)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        private OrderManagementEditViewModel BuildEditViewModel(
            OrderAdminEditData data,
            string? selectedStatus = null)
        {
            return new OrderManagementEditViewModel
            {
                Id = data.Order.Id,
                UserId = data.Order.UserId,
                CustomerDisplayName = data.Order.User == null
                    ? $"ID: {data.Order.UserId}"
                    : string.IsNullOrWhiteSpace(data.Order.User.FullName)
                        ? data.Order.User.Username
                        : data.Order.User.FullName,
                OrderDate = data.Order.OrderDate,
                TotalAmount = data.Order.TotalAmount,
                Status = string.IsNullOrWhiteSpace(selectedStatus)
                    ? data.Order.Status
                    : selectedStatus,
                StatusOptions = data.EditableStatuses
            };
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

        private string GetAdminAuditActorName()
        {
            var admin = HttpContext.Session.GetObject<User>("User");
            return string.IsNullOrWhiteSpace(admin?.Username) ? "Admin" : admin.Username;
        }
    }
}
