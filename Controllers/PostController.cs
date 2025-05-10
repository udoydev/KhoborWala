using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediumClone.Models;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace MediumClone.Controllers
{
    // This controller is restricted to authenticated users only
    [Authorize]
    public class PostController : Controller
    {
        // ApplicationDbContext handles database operations
        // UserManager manages logged-in user data and authentication
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PostController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // List all posts with optional filtering (by category) and sorting (by views, date, or title)
        public async Task<IActionResult> Index(string category, string sortOrder)
        {
            // Store current sort and category in ViewData for use in view
            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentCategory"] = category;

            // Load all posts from the database including the user who created each post
            var posts = from p in _context.Posts.Include(p => p.User)
                        select p;

            // If a category is selected and it's not "All", filter the posts by category
            if (!string.IsNullOrEmpty(category) && category != "All")
            {
                posts = posts.Where(p => p.Category.ToLower() == category.ToLower());
            }

            // Apply sorting based on sortOrder
            switch (sortOrder)
            {
                case "mostViewed":
                    // Sort by descending view count (most viewed first)
                    posts = posts.OrderByDescending(p => p.ViewCount);
                    break;
                case "recent":
                    // Sort by most recently created posts
                    posts = posts.OrderByDescending(p => p.CreatedAt);
                    break;
                default:
                    // Default sorting: alphabetically by title
                    posts = posts.OrderBy(p => p.Title);
                    break;
            }

            // Convert query to list to execute the database call
            var postList = await posts.ToListAsync();

            // Calculate like count for each post using reactions table
            var likeCounts = _context.Reactions
                .GroupBy(r => r.PostId)
                .Select(g => new { PostId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.PostId, x => x.Count);

            // Send like counts to the view via ViewBag
            ViewBag.LikeCounts = likeCounts;
            return View(postList);
        }

        // Display details of a single post by its ID
        public async Task<IActionResult> Details(int? id)
        {
            // Return 404 if post ID is null
            if (id == null)
                return NotFound();

            // Load the post with user data
            var post = await _context.Posts.Include(p => p.User).FirstOrDefaultAsync(m => m.Id == id);

            // Return 404 if no post found
            if (post == null)
                return NotFound();

            // Increase view count every time post is viewed
            post.ViewCount++;
            _context.Update(post);
            await _context.SaveChangesAsync();

            // Count total reactions (likes) for this post
            ViewBag.LikeCount = _context.Reactions.Count(r => r.PostId == post.Id);
            return View(post);
        }

        // Show the create post form
        public IActionResult Create()
        {
            return View(new Post());
        }

        // Handle the form submission for creating a new post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Content,Category")] Post post)
        {
            // Check if the form data is valid
            if (ModelState.IsValid)
            {
                // Set creation timestamp
                post.CreatedAt = DateTime.Now;

                // Initialize view count to 0
                post.ViewCount = 0;

                // Assign the current user as the author of the post
                post.UserId = _userManager.GetUserId(User);
                post.Username = User.Identity.Name;

                // Save the new post to the database
                _context.Add(post);
                await _context.SaveChangesAsync();

                // Redirect back to the list of posts
                return RedirectToAction(nameof(Index));
            }

            // If model state is invalid, return back to form with validation errors
            return View(post);
        }

        // Show the edit form for a post
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var post = await _context.Posts.FindAsync(id);
            if (post == null)
                return NotFound();

            // Prevent user from editing others' posts
            if (post.UserId != _userManager.GetUserId(User))
                return Forbid();

            return View(post);
        }

        // Handle form submission for editing post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Content,Category")] Post post)
        {
            if (id != post.Id)
                return NotFound();

            var dbPost = await _context.Posts.FindAsync(id);
            if (dbPost == null)
                return NotFound();

            // Prevent unauthorized edits
            if (dbPost.UserId != _userManager.GetUserId(User))
                return Forbid();

            if (ModelState.IsValid)
            {
                // Update only editable fields
                dbPost.Title = post.Title;
                dbPost.Content = post.Content;
                dbPost.Category = post.Category;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(post);
        }

        // Show confirmation page before deleting a post
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var post = await _context.Posts.FindAsync(id);
            if (post == null)
                return NotFound();

            // Users can only delete their own posts
            if (post.UserId != _userManager.GetUserId(User))
                return Forbid();

            return View(post);
        }

        // Confirm and perform the deletion of a post
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null)
                return NotFound();

            if (post.UserId != _userManager.GetUserId(User))
                return Forbid();

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // List trending posts based on most likes
        public async Task<IActionResult> Trending()
        {
            // Sort posts descending by reaction count (most liked first)
            var posts = await _context.Posts
                .OrderByDescending(p => _context.Reactions.Count(r => r.PostId == p.Id))
                .ToListAsync();

            // Like counts for display
            var likeCounts = _context.Reactions
                .GroupBy(r => r.PostId)
                .Select(g => new { PostId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.PostId, x => x.Count);

            ViewBag.LikeCounts = likeCounts;
            return View("Index", posts); // Reuse the Index view
        }

        // List latest posts by date
        public async Task<IActionResult> Latest()
        {
            var posts = await _context.Posts.OrderByDescending(p => p.CreatedAt).ToListAsync();

            var likeCounts = _context.Reactions
                .GroupBy(r => r.PostId)
                .Select(g => new { PostId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.PostId, x => x.Count);

            ViewBag.LikeCounts = likeCounts;
            return View("Index", posts);
        }

        // Handle like/unlike post action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> React(int id)
        {
            var userId = _userManager.GetUserId(User);

            // Check if this user already reacted to the post
            var reaction = _context.Reactions.FirstOrDefault(r => r.PostId == id && r.UserId == userId);
            var post = _context.Posts.FirstOrDefault(p => p.Id == id);

            if (reaction == null)
            {
                // Add a new reaction
                _context.Reactions.Add(new Reaction { PostId = id, UserId = userId });

                // Notify the post's owner if someone else liked their post
                if (post != null && post.UserId != userId)
                {
                    var liker = await _userManager.FindByIdAsync(userId);

                    // Create notification message
                    var notice = new Notice
                    {
                        UserId = post.UserId,
                        Message = $"{liker?.UserName ?? "Someone"} reacted to your post: {post.Title}",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notices.Add(notice);
                }
            }
            else
            {
                // If already liked, remove the like (toggle)
                _context.Reactions.Remove(reaction);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Shows a list of posts created by the currently logged-in user (for general users).
        /// </summary>
        public async Task<IActionResult> MyPosts()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var myPosts = await _context.Posts
                .Where(p => p.UserId == userId)
                .ToListAsync();
            return View(myPosts);
        }
    }
}
