using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Linq;
using MediumClone.Models;

namespace MediumClone.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;

        public UserController(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public IActionResult Index()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                // Prevent currently logged-in admin from deleting themselves
                if (user.Id == _userManager.GetUserId(User))
                {
                    TempData["ErrorMessage"] = "You cannot delete your own admin account while logged in.";
                    return RedirectToAction("Index");
                }

                // Remove all posts by this user before deleting user
                var posts = _dbContext.Posts.Where(p => p.UserId == user.Id).ToList();
                _dbContext.Posts.RemoveRange(posts);
                _dbContext.SaveChanges();

                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "User deleted successfully.";
                }
                else
                {
                    var errorMsg = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["ErrorMessage"] = "Failed to delete user: " + errorMsg;
                }
            }
            else
            {
                TempData["ErrorMessage"] = "User not found.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Notice(string id, string notice)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(notice))
                {
                    // Always set as global notice
                    var allNotices = _dbContext.Notices.ToList();
                    _dbContext.Notices.RemoveRange(allNotices);
                    _dbContext.SaveChanges();
                    var newNotice = new Notice { UserId = null, Message = notice, CreatedAt = DateTime.UtcNow };
                    _dbContext.Notices.Add(newNotice);
                    _dbContext.SaveChanges();
                    TempData["SuccessMessage"] = $"Global notice set: {notice}";
                }
                else
                {
                    TempData["ErrorMessage"] = "Notice cannot be empty.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error saving notice: " + ex.Message;
            }
            return RedirectToAction("Index");
        }
    }
}