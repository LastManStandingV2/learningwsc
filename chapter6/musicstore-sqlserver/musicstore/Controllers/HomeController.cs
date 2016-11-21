using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MusicStore.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace MusicStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppSettings _appSettings;
        private IHostingEnvironment _environment;

        public HomeController(IOptions<AppSettings> options, IHostingEnvironment environment)
        {
            _appSettings = options.Value;
            _environment = environment;
        }

        //
        // GET: /Home/
        public async Task<IActionResult> Index(
            [FromServices] MusicStoreContext dbContext,
            [FromServices] IMemoryCache cache)
        {
            // Get most popular albums
            var cacheKey = "topselling";
            List<Album> albums;
            if (!cache.TryGetValue(cacheKey, out albums))
            {
                albums = await GetTopSellingAlbumsAsync(dbContext, 6);

                if (albums != null && albums.Count > 0)
                {
                    if (_appSettings.CacheDbResults)
                    {
                        // Refresh it every 10 minutes.
                        // Let this be the last item to be removed by cache if cache GC kicks in.
                        cache.Set(
                            cacheKey,
                            albums,
                            new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromMinutes(10))
                            .SetPriority(CacheItemPriority.High));
                    }
                }
            }

            return View(albums);
        }

        public async Task<IActionResult> Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(string albumName)
        {
            var uploads = Path.Combine(_appSettings.StorageLocation);
            var file = Request.Form.Files.Count > 0 ? Request.Form.Files[0] : null;
            if(file != null)
            {
                var fileName = file.FileName.Substring(file.FileName.LastIndexOf(@"\") + 1,
                               file.FileName.Length - file.FileName.LastIndexOf(@"\") - 1);
                using (var fileStream = new FileStream(Path.Combine(uploads, fileName), FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
            }
            return View();
        }

        public IActionResult Error()
        {
            return View("~/Views/Shared/Error.cshtml");
        }

        public IActionResult StatusCodePage()
        {
            return View("~/Views/Shared/StatusCodePage.cshtml");
        }

        public IActionResult AccessDenied()
        {
            return View("~/Views/Shared/AccessDenied.cshtml");
        }

        private Task<List<Album>> GetTopSellingAlbumsAsync(MusicStoreContext dbContext, int count)
        {
            // Group the order details by album and return
            // the albums with the highest count

            return dbContext.Albums
                .OrderByDescending(a => a.OrderDetails.Count)
                .Take(count)
                .ToListAsync();
        }
    }
}