using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CrucibleBlogMVC.Data;
using CrucibleBlogMVC.Models;

namespace CrucibleBlogMVC.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogPostsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BlogPostsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("portfolio/{count}")]
        public async Task<ActionResult<IEnumerable<BlogPost>>> GetPortfolioBlogPosts(int? count)
        {
            if (_context.BlogPosts == null || count == null)
            {
                return NotFound();
            }

            IEnumerable<BlogPost>? result = await _context.BlogPosts.Take(count.Value).ToListAsync();

            if (result.Any())
            {
                return Ok(result);
            }

            return BadRequest();
        }
    }
}
