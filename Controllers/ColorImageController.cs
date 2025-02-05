using Mailo.Data;
using Mailo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Mailo.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ColorImageController : Controller
    {
        private readonly AppDbContext _context;

        public ColorImageController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ عرض جميع الصور المرتبطة بالألوان
        public async Task<IActionResult> Index()
        {
            var colorImages = await _context.ColorImages
                .Include(ci => ci.Color)
                .Include(ci => ci.Product)
                .ToListAsync();

            return View(colorImages);
        }

        // ✅ عرض صفحة إضافة صورة جديدة
        public async Task<IActionResult> Create()
        {
            ViewBag.Colors = new SelectList(await _context.Colors.ToListAsync(), "Id", "ColorName");
            ViewBag.Products = new SelectList(await _context.Products.ToListAsync(), "ID", "Name");
            return View();
        }

        // ✅ إضافة صورة جديدة
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(List<int> ColorIds, List<IFormFile> ImageFiles, int ProductId)
        {
            if (ColorIds == null || ImageFiles == null || !ColorIds.Any() || !ImageFiles.Any())
            {
                TempData["Error"] = "Please select at least one color and upload at least one image.";
                return RedirectToAction("Create");
            }

            for (int i = 0; i < ColorIds.Count; i++)
            {
                var colorImage = new ColorImage
                {
                    ColorId = ColorIds[i],
                    ProductId = ProductId
                };

                if (ImageFiles.Count > i && ImageFiles[i] != null)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await ImageFiles[i].CopyToAsync(memoryStream);
                        colorImage.dbImage = memoryStream.ToArray();
                    }
                }

                _context.ColorImages.Add(colorImage);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Images Added Successfully";
            return RedirectToAction("Index");
        }

        // ✅ حذف صورة معينة
        public async Task<IActionResult> Delete(int id)
        {
            var colorImage = await _context.ColorImages.FindAsync(id);
            if (colorImage == null)
            {
                return NotFound();
            }

            _context.ColorImages.Remove(colorImage);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Color Image Deleted Successfully";
            return RedirectToAction("Index");
        }
    }
}