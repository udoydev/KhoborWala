using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediumClone.Areas.Admin.Models;
using System.Collections.Generic;

namespace MediumClone.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        // In-memory list for demonstration (replace with EF Core in real app)
        private static List<Category> categories = new List<Category>();

        public IActionResult Index()
        {
            return View(categories);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category category)
        {
            if (ModelState.IsValid)
            {
                category.Id = categories.Count + 1;
                categories.Add(category);
                return RedirectToAction("Index");
            }
            return View(category);
        }

        public IActionResult Edit(int id)
        {
            var category = categories.Find(c => c.Id == id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category category)
        {
            var existing = categories.Find(c => c.Id == category.Id);
            if (existing == null) return NotFound();
            if (ModelState.IsValid)
            {
                existing.Name = category.Name;
                return RedirectToAction("Index");
            }
            return View(category);
        }

        public IActionResult Delete(int id)
        {
            var category = categories.Find(c => c.Id == id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var category = categories.Find(c => c.Id == id);
            if (category != null) categories.Remove(category);
            return RedirectToAction("Index");
        }
    }
}
