using GymBro.Application.Orders;
using GymBro.Core;
using GymBro.Web.Features.Orders;
using Microsoft.AspNetCore.Mvc;

namespace GymBro.Web.Controllers
{
    public class OrderDetailsController : BaseAdminController
    {
        private readonly IOrderDetailReadService _orderDetailReadService;

        public OrderDetailsController(IOrderDetailReadService orderDetailReadService)
        {
            _orderDetailReadService = orderDetailReadService;
        }

        public async Task<IActionResult> Index(int? orderId)
        {
            var result = await _orderDetailReadService.GetListAsync(orderId);
            return View(OrderPresentationFactory.CreateDetailIndex(result.OrderId, result.Items));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (!id.HasValue)
            {
                return NotFound();
            }

            var orderDetail = await _orderDetailReadService.GetByIdAsync(id.Value);
            if (orderDetail == null)
            {
                return NotFound();
            }

            return View(OrderPresentationFactory.CreateDetailReadOnly(orderDetail));
        }

        public IActionResult Create()
        {
            TempData["InfoMessage"] = "Them chi tiet don hang thu cong da bi khoa de tranh lech ton kho va thanh toan.";
            return RedirectToAction("Index", "Orders");
        }

        [HttpPost]
        [ActionName("Create")]
        [ValidateAntiForgeryToken]
        public IActionResult CreatePost()
        {
            TempData["InfoMessage"] = "Them chi tiet don hang thu cong da bi khoa.";
            return RedirectToAction("Index", "Orders");
        }

        public IActionResult Edit(int? id)
        {
            TempData["InfoMessage"] = "Sua chi tiet don hang truc tiep da bi khoa. Hay dung workflow don hang chuan.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public IActionResult EditPost()
        {
            TempData["InfoMessage"] = "Sua chi tiet don hang truc tiep da bi khoa.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(int? id)
        {
            TempData["InfoMessage"] = "Xoa chi tiet don hang truc tiep da bi khoa.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            TempData["InfoMessage"] = "Xoa chi tiet don hang truc tiep da bi khoa.";
            return RedirectToAction(nameof(Index));
        }
    }
}
