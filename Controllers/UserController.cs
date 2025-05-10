// Required namespaces for authorization, MVC, identity management, and models
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using MediumClone.Models;
using System.Linq;
using System.Threading.Tasks;

namespace MediumClone.Controllers
{
    // Only allow authenticated users to access any actions in this controller
    [Authorize]
    public class UserController : Controller
    {
        // Injected dependencies for database access and user identity handling
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        // Constructor for dependency injection
        public UserController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        // Default action, redirects to the 'Notices' page
        public IActionResult Index()
        {
            // Redirects user to the Notices action to see their notices
            return RedirectToAction("Notices");
        }

        // Async action that returns the list of notices for the logged-in user
        public async Task<IActionResult> Notices()
        {
            // Get the currently logged-in user's ID from the User principal
            var userId = _userManager.GetUserId(User); // Equivalent to: HttpContext.User.Identity.Name

            // Query the Notices table to retrieve all notices where UserId matches the logged-in user's ID
            // Order the notices in descending order by Id so that recent ones come first
            var notices = _dbContext.Notices
                .Where(n => n.UserId == userId) // Filter only current user's notices
                .OrderByDescending(n => n.Id)   // Sort from newest to oldest
                .ToList(); // Execute the query and return a list

            // Return the 'Notices' view, passing the retrieved list of notices
            return View(notices);
        }

        // POST action to delete all notices for the current user
        [HttpPost] // Only allow POST requests (not GET)
        [ValidateAntiForgeryToken] // Prevent CSRF attacks
        public IActionResult ClearNotice()
        {
            // Get the current user's ID to ensure we only affect their data
            var userId = _userManager.GetUserId(User);

            // Query all notices in the database that belong to the current user
            var notices = _dbContext.Notices
                .Where(n => n.UserId == userId) // Filter notices by UserId
                .ToList(); // Execute the query and store the result in a list

            // Remove all of the user's notices from the Notices table in one go
            _dbContext.Notices.RemoveRange(notices);

            // Commit the deletion to the database
            _dbContext.SaveChanges();

            // Use TempData to pass a one-time message to the view after redirection
            TempData["SuccessMessage"] = "Notices cleared.";

            // Redirect user back to the Notices action to refresh the page
            return RedirectToAction("Notices");
        }
    }
}
