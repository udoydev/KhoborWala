using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediumClone.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;

namespace MediumClone.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PostController : Controller
    {
        private readonly ApplicationDbContext _context;
        public PostController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var posts = await _context.Posts.ToListAsync();
            return View(posts);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();
            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Post post)
        {
            if (ModelState.IsValid)
            {
                // Attach the post and mark the fields as modified
                _context.Attach(post);
                _context.Entry(post).Property(x => x.Title).IsModified = true;
                _context.Entry(post).Property(x => x.Content).IsModified = true;
                _context.Entry(post).Property(x => x.Category).IsModified = true;
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(post);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();
            return View(post);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post != null)
            {
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Details(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();
            return View(post);
        }
    }
}