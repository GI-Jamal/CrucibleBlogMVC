using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CrucibleBlogMVC.Data;
using CrucibleBlogMVC.Models;
using CrucibleBlogMVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using CrucibleBlogMVC.Helpers;
using X.PagedList;
using Microsoft.CodeAnalysis.Operations;

namespace CrucibleBlogMVC.Controllers
{
    [Authorize]
    public class BlogPostsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageService _imageService;
        private readonly IBlogService _blogService;

        public BlogPostsController(ApplicationDbContext context, IImageService imageService, IBlogService blogService)
        {
            _context = context;
            _imageService = imageService;
            _blogService = blogService;
        }


        [Authorize(Roles = "Admin")]
        // GET: BlogPosts
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.BlogPosts.Include(b => b.Category).OrderByDescending(b => b.CreatedDate);
            return View(await applicationDbContext.ToListAsync());
        }

        [AllowAnonymous]
        // GET: BlogPosts/Details/5
        public async Task<IActionResult> Details(string? slug)
        {
            if (string.IsNullOrEmpty(slug))
            {
                return NotFound();
            }

            var blogPost = await _context.BlogPosts
                .Include(b => b.Category)
                .Include(b => b.Comments)
                    .ThenInclude(c => c.Author)
                .Include(b => b.Tags)
                .Include(b => b.Likes)
                .FirstOrDefaultAsync(m => m.Slug == slug);

            if (blogPost == null)
            {
                return NotFound();
            }

            string? adminRoleId;
            string? adminId;
            BlogUser? blogAuthor;

            adminRoleId = await _context.Roles.Where(u => u.Name == "Admin").Select(u => u.Id).FirstOrDefaultAsync();
            adminId = await _context.UserRoles.Where(u => u.RoleId == adminRoleId).Select(u => u.UserId).FirstOrDefaultAsync();
            blogAuthor = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminId);
            ViewData["BlogAuthor"] = blogAuthor;

            return View(blogPost);
        }

        [Authorize(Roles = "Admin")]
        // GET: BlogPosts/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            ViewData["TagId"] = new MultiSelectList(_context.Tags, "Id", "Name");

            BlogPost blogPost = new();

            return View(blogPost);
        }

        // POST: BlogPosts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Abstract,Content,CreatedDate,UpdatedDate,Slug,IsDeleted,IsPublished,CategoryId,ImageFile")] BlogPost blogPost, string? stringTags)
        {

            ModelState.Remove("Slug");
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", blogPost.CategoryId);

            if (ModelState.IsValid)
            {
                if (!await _blogService.ValidSlugAsync(blogPost.Title, blogPost.Id))
                {
                    ModelState.AddModelError("Title", "A similar Title/Slug is already in use.");
                    return View(blogPost);
                }

                blogPost.Slug = StringHelper.BlogPostSlug(blogPost.Title);

                blogPost.CreatedDate = DateTime.UtcNow;

                if (blogPost.ImageFile != null)
                {
                    blogPost.ImageData = await _imageService.ConvertFileToByteArrayAsync(blogPost.ImageFile);
                    blogPost.ImageType = blogPost.ImageFile.ContentType;
                }

                _context.Add(blogPost);
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(stringTags))
                {
                    await _blogService.AddTagsToBlogPostAsync(stringTags, blogPost.Id);
                }

                return RedirectToAction(nameof(Index));
            }
            return View(blogPost);
        }

        // GET: BlogPosts/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.BlogPosts == null)
            {
                return NotFound();
            }

            BlogPost? blogPost = await _context.BlogPosts.Include(b => b.Tags).FirstOrDefaultAsync(b => b.Id == id);

            if (blogPost == null)
            {
                return NotFound();
            }

            List<string> tagNames = blogPost.Tags.Select(t => t.Name!).ToList();

            string tags = string.Join(", ", tagNames) + ", ";

            if (!string.Equals(tags, ", "))
            {
                ViewData["Tags"] = tags;
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", blogPost.CategoryId);
            return View(blogPost);
        }

        // POST: BlogPosts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Abstract,Content,IsDeleted,CreatedDate,IsPublished,CategoryId,ImageData,ImageType,ImageFile")] BlogPost blogPost, string? stringTags)
        {
            if (id != blogPost.Id)
            {
                return NotFound();
            }

            ModelState.Remove("Slug");
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", blogPost.CategoryId);
            ViewData["Tags"] = stringTags + ", ";

            if (ModelState.IsValid)
            {
                try
                {
                    if (!await _blogService.ValidSlugAsync(blogPost.Title, blogPost.Id))
                    {
                        ModelState.AddModelError("Title", "A similar Title/Slug is already in use.");
                        return View(blogPost);
                    }

                    blogPost.CreatedDate = DateTime.SpecifyKind(blogPost.CreatedDate, DateTimeKind.Utc);
                    blogPost.UpdatedDate = DateTime.UtcNow;

                    blogPost.Slug = StringHelper.BlogPostSlug(blogPost.Title);

                    if (blogPost.ImageFile != null)
                    {
                        blogPost.ImageData = await _imageService.ConvertFileToByteArrayAsync(blogPost.ImageFile);
                        blogPost.ImageType = blogPost.ImageFile.ContentType;
                    }

                    _context.Update(blogPost);
                    await _context.SaveChangesAsync();

                    await _blogService.RemoveAllBlogPostTagsAsync(blogPost.Id);

                    if (!string.IsNullOrEmpty(stringTags?.Trim()))
                    {
                        await _blogService.AddTagsToBlogPostAsync(stringTags, blogPost.Id);
                    }
                    
                    return RedirectToAction(nameof(Details), new { blogPost.Slug });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BlogPostExists(blogPost.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                
            }
            
            return View(blogPost);
        }

        // GET: BlogPosts/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.BlogPosts == null)
            {
                return NotFound();
            }

            var blogPost = await _context.BlogPosts
                .Include(b => b.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (blogPost == null)
            {
                return NotFound();
            }

            return View(blogPost);
        }

        // POST: BlogPosts/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.BlogPosts == null)
            {
                return Problem("Entity set 'ApplicationDbContext.BlogPosts'  is null.");
            }
            var blogPost = await _context.BlogPosts.FindAsync(id);
            if (blogPost != null)
            {
                blogPost.IsDeleted = true;
                blogPost.IsPublished = false;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> LikeBlogPost(int blogPostId, string blogUserId)
        {
            BlogUser? blogUser = await _context.Users.Include(u => u.BlogLikes).FirstOrDefaultAsync(u => u.Id == blogUserId);
            bool result = false;
            BlogLike? blogLike = new();

            if (blogUser != null)
            {
                if (!blogUser.BlogLikes.Any(bl => bl.BlogPostId == blogPostId))
                {
                    blogLike = new BlogLike() { BlogPostId = blogPostId, IsLiked = true };

                    blogUser.BlogLikes.Add(blogLike);
                }
                else
                {
                    blogLike = await _context.BlogLikes.FirstOrDefaultAsync(bl => bl.BlogPostId == blogPostId && bl.BlogUserId == blogUserId);

                    blogLike!.IsLiked = !blogLike.IsLiked;
                }
                result = blogLike.IsLiked;
                await _context.SaveChangesAsync();

            }
            return Json(new
            {
                isLiked = result,
                count = _context.BlogLikes.Where(bl => bl.BlogPostId == blogPostId && bl.IsLiked == true).Count()
            });
        }

        private bool BlogPostExists(int id)
        {
            return (_context.BlogPosts?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
