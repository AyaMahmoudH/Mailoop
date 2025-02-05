﻿using Mailo.Data.Enums;
using Mailo.Data;
using Mailo.IRepo;
using Mailo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Mailo.Controllers
{
    public class UserController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context; 
        private readonly ISearchRepo _search;

        public UserController(IUnitOfWork unitOfWork, AppDbContext context, ISearchRepo search)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _search = search;
        }

        public async Task<IActionResult> Search(string text, int? categoryId)
        {
            // احصل على المنتجات التي تطابق البحث
            var query = _context.Products
                .Include(p => p.Variants)
                .ThenInclude(v => v.Size)
                .Include(p => p.Variants)
                .ThenInclude(v => v.Color)
                .AsQueryable();

            // تصفية حسب الفئة إذا تم اختيارها
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // تصفية حسب النص إذا تم إدخاله
            if (!string.IsNullOrEmpty(text))
            {
                query = query.Where(p => p.Name.Contains(text) || p.Description.Contains(text));
            }

            var products = await query.ToListAsync();

            // تمرير بيانات الفئات للمظهر
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
            ViewBag.SelectedCategoryId = categoryId;

            // عرض المنتجات بناءً على نوع المستخدم
            var user = await _unitOfWork.userRepo.GetUser(User.Identity.Name);
            if (user == null || user.UserType == UserType.Client)
            {
                return View("Index_U", products);
            }
            else
            {
                return View("Index_A", products);
            }
        }



        #region Admin 

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
          
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> Index_A(int? categoryId, string searchText)
        {
            var query = _context.Products
                 .Include(p => p.Variants)
                 .ThenInclude(v => v.Size)
                 .Include(p => p.Variants)
                 .ThenInclude(v => v.Color)
                 .AsQueryable();
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(p => p.Name.Contains(searchText) || p.Description.Contains(searchText));
            }

            var products = query.ToList();

            ViewBag.Categories = new SelectList(await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync(), "Id", "Name");

            return View(products);
        }

        public async Task<IActionResult> New()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
         
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> New(Product product)
        {
            if (ModelState.IsValid)
            {
                if (product.clientFile != null)
                {
                    MemoryStream stream = new MemoryStream();
                    product.clientFile.CopyTo(stream);
                    product.dbImage = stream.ToArray();
                }

                _unitOfWork.products.Insert(product);

                

                _context.SaveChanges();
                TempData["Success"] = "Product Has Been Added Successfully";

                return RedirectToAction("Index_A");
            }

           
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");

            return View(product);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id != 0)
            {
                return View(await _unitOfWork.products.GetByID(id));
            }
            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Product product)
        {
            if (product != null)
            {
                _unitOfWork.products.Delete(product);
                TempData["Success"] = "product Has Been Deleted Successfully";
                return RedirectToAction("Index_A");
            }
            return View(product);

        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _unitOfWork.products.GetByID(id);
            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
            ViewBag.Colors = _context.Colors.ToList();
            ViewBag.Sizes = _context.Sizes.ToList();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Product product)
        {
            if (ModelState.IsValid)
            {
                if (product.clientFile != null)
                {
                    MemoryStream stream = new MemoryStream();
                    product.clientFile.CopyTo(stream);
                    product.dbImage = stream.ToArray();
                }
                // معالجة رفع الصورة إذا كانت موجودة
            //    if (product.clientFile != null && product.clientFile.Length > 0)
            //{
            //    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", product.clientFile.FileName);

            //    using (var stream = new FileStream(filePath, FileMode.Create))
            //    {
            //        await product.clientFile.CopyToAsync(stream);
            //    }

            //    existingProduct.imageSrc = "~/images/" + product.clientFile.FileName;
            //}

            //if (ModelState.IsValid)
            //{
                //existingProduct.Name = product.Name;
                //existingProduct.Price = product.Price;
                //existingProduct.Description = product.Description;
                //existingProduct.CategoryId = product.CategoryId;
                //existingProduct.Discount = product.Discount;

              
               

                _unitOfWork.products.Update(product);

                TempData["Success"] = "Product Has Been Updated Successfully";
                return RedirectToAction("Index_A");
            }

            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
        

            return View(product);
        }
        
        #endregion

        #region Client
        [HttpGet]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Index_U(int? categoryId, string searchText)
        {
            var query = _context.Products
                .Include(p => p.Variants)
                .ThenInclude(v => v.Size)
                .Include(p => p.Variants)
                .ThenInclude(v => v.Color)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(p => p.Name.Contains(searchText) || p.Description.Contains(searchText));
            }

            var products = await query.ToListAsync();

            ViewBag.Categories = new SelectList(await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync(), "Id", "Name");

            return View(products);
        }





        #endregion
    }
}
