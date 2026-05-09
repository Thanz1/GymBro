using GymBro.Contracts; // Đảm bảo đã có ReviewDto ở đây
using GymBro.Service;
using Microsoft.AspNetCore.Mvc;

namespace GymBro.Web.Controllers
{
    public class AdminReviewsController : BaseAdminController
    {
        private readonly IReviewService _reviewService; // Dùng Service thay vì DbContext

        public AdminReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        public async Task<IActionResult> Index()
        {
            // Gọi API để lấy danh sách đánh giá
            var reviews = await _reviewService.GetAllReviewsAsync();

            // Logic sắp xếp nên thực hiện ở API hoặc thực hiện tại đây trên List DTO
            return View(reviews.OrderByDescending(r => r.CreatedDate).ToList());
        }

        public async Task<IActionResult> Delete(int id)
        {
            // Gọi lệnh xóa qua API
            var success = await _reviewService.DeleteReviewAsync(id);

            if (success)
            {
                TempData["SuccessMessage"] = "Đã xóa đánh giá thành công.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}