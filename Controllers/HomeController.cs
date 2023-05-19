using CrucibleBlogMVC.Data;
using CrucibleBlogMVC.Models;
using CrucibleBlogMVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Diagnostics;
using X.PagedList;

namespace CrucibleBlogMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BlogUser> _userManager;
        private readonly ILogger<HomeController> _logger;
        private readonly IBlogService _blogService;
        private readonly IEmailSender _emailService;
        private readonly IConfiguration _configuration;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger, IBlogService blogService, UserManager<BlogUser> userManager, IEmailSender emailService, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _blogService = blogService;
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index(int? pageNum)
        {
            int pageSize = 4;
            int page = pageNum ?? 1;

            IPagedList<BlogPost> blogPosts = await _context.BlogPosts.Include(b => b.Category).Where(b => b.IsPublished == true).OrderByDescending(b => b.CreatedDate).ToPagedListAsync(page, pageSize);

            ViewData["ActionName"] = "Index";

            return View(blogPosts);
        }

        public async Task<IActionResult> SearchIndex(string? searchString, int? pageNum)
        {
            int pageSize = 4;
            int page = pageNum ?? 1;

            if (string.IsNullOrWhiteSpace(searchString))
            {
                searchString = TempData["SearchString"]?.ToString();
            }

            TempData["SearchString"] = searchString;

            IPagedList<BlogPost> blogPosts = await _blogService.SearchBlogPosts(searchString).ToPagedListAsync(page, pageSize);

            ViewData["ActionName"] = "SearchIndex";

            return View(nameof(Index), blogPosts);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> FavoritePosts(int? pageNum)
        {
            int pageSize = 4;
            int page = pageNum ?? 1;

            string? blogUserId = _userManager.GetUserId(User);

            IPagedList<BlogPost> blogPosts = await (await _blogService.GetFavoriteBlogPostsAsync(blogUserId)).ToPagedListAsync(page, pageSize);

            ViewData["ActionName"] = "FavoritePosts";

            return View(nameof(Index), blogPosts);
        }

        [Authorize]
        public async Task<IActionResult> ContactMe()
        {
            string? blogUserId = _userManager.GetUserId(User);

            if (blogUserId == null)
            {
                return NotFound();
            }

            BlogUser? blogUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == blogUserId);

            return View(blogUser);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ContactMe([Bind("FirstName,LastName,Email")] BlogUser blogUser, string? message)
        {
            string? swalMessage = string.Empty;

            if (ModelState.IsValid)
            {
                try
                {
                    string? userEmail = blogUser.Email;
                    await _emailService.SendEmailAsync(userEmail!, $"Contact Me Message From - {blogUser.FullName}", message!);
                    swalMessage = "Email sent successfully!";
                }
                catch (Exception)
                {

                    throw;
                }
                
                swalMessage = "Error: Unable to send email.";
            }

            return RedirectToAction("Index", new { swalMessage });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}