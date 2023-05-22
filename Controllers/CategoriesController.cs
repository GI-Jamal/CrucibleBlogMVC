using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CrucibleBlogMVC.Data;
using CrucibleBlogMVC.Models;
using X.PagedList;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using CrucibleBlogMVC.Services.Interfaces;
using CrucibleBlogMVC.Services;

namespace CrucibleBlogMVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageService _imageService;


        public CategoriesController(ApplicationDbContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            return _context.Categories != null ?
                        View(await _context.Categories.ToListAsync()) :
                        Problem("Entity set 'ApplicationDbContext.Categories'  is null.");
        }

        [AllowAnonymous]
        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id, int? pageNum)
        {
            if (id == null || _context.Categories == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.Include(c => c.BlogPosts).FirstOrDefaultAsync(m => m.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            int pageSize = 3;
            int page = pageNum ?? 1;

            IPagedList<BlogPost> blogPosts = await category.BlogPosts.Where(b => b.IsPublished == true && b.IsDeleted == false).OrderByDescending(b => b.CreatedDate).ToPagedListAsync(page, pageSize);

            string? adminRoleId;
            string? adminId;
            BlogUser? blogAuthor;

            adminRoleId = await _context.Roles.Where(u => u.Name == "Admin").Select(u => u.Id).FirstOrDefaultAsync();
            adminId = await _context.UserRoles.Where(u => u.RoleId == adminRoleId).Select(u => u.UserId).FirstOrDefaultAsync();
            blogAuthor = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminId);

            ViewData["BlogAuthor"] = blogAuthor;

            ViewData["CategoryName"] = category.Name;

            return View(blogPosts);
        }

        // GET: Categories/Create
        public IActionResult Create()
        {
            Category category = new();
            return View(category);
        }

        // POST: Categories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,ImageFile")] Category category)
        {
            if (ModelState.IsValid)
            {
                Category? dbCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Name!.Trim().ToLower() == category.Name!.Trim().ToLower());

                if (dbCategory != null)
                {
                    ModelState.AddModelError("Name", "A similar Name is already in use.");
                    return View(category);
                }


                if (category.ImageFile != null)
                {
                    category.ImageData = await _imageService.ConvertFileToByteArrayAsync(category.ImageFile);
                    category.ImageType = category.ImageFile.ContentType;
                }


                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Categories == null)
            {
                return NotFound();
            }

            Category? category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Categories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,ImageFile,ImageData,ImageType")] Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    Category? dbCategory = await _context.Categories.Where(c => c.Id != category.Id).FirstOrDefaultAsync(c => c.Name!.Trim().ToLower() == category.Name!.Trim().ToLower());

                    if (dbCategory != null)
                    {
                        ModelState.AddModelError("Name", "A similar Name is already in use.");
                        return View(category);
                    }

                    if (category.ImageFile != null)
                    {
                        category.ImageData = await _imageService.ConvertFileToByteArrayAsync(category.ImageFile);
                        category.ImageType = category.ImageFile.ContentType;
                    }

                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Categories == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Categories == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Categories'  is null.");
            }
            Category? category = await _context.Categories.Include(c => c.BlogPosts).FirstOrDefaultAsync(c => c.Id == id);
            if (category != null)
            {

                Category? generalCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Name!.Trim().ToLower() == "general");
                if (generalCategory == null)
                {
                    generalCategory = new() { Name = "General" };
                    _context.Categories.Add(generalCategory);
                    await _context.SaveChangesAsync();
                }



                foreach (BlogPost blogPost in category.BlogPosts)
                {
                    blogPost.CategoryId = generalCategory.Id;
                    blogPost.Category = generalCategory;
                }

                await _context.SaveChangesAsync();

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return (_context.Categories?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
