using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApp.Data;
using RestaurantApp.Models;
using static RestaurantApp.Models.Dish;

namespace RestaurantApp.Controllers
{
    public class DishesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public DishesController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Dishes
        public async Task<IActionResult> Index(string searchString, MealType? category)
        {
            var dishes = _context.Dishes
            .Include(d => d.DishImages)
            .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                var search = searchString.ToLower();
                dishes = dishes.Where(d => d.Name.ToLower().Contains(search) || d.Description.ToLower().Contains(search));
            }

            if (category.HasValue)
                dishes = dishes.Where(d => d.Category == category.Value);

            return View(await dishes.ToListAsync());
        }

        // GET: Dishes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var dish = await _context.Dishes
                .Include(d => d.Ratings)
                .Include(d => d.DishImages)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (dish == null) return NotFound();

            return View(dish);
        }

        // GET: Dishes/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        // POST: Dishes/Create
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Price,Category")] Dish dish, List<IFormFile>? imageFiles)
        {
            if (ModelState.IsValid)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "dishes");
                Directory.CreateDirectory(uploadsFolder);

                if (imageFiles != null && imageFiles.Count > 0)
                {
                    dish.DishImages = new List<DishImage>();

                    foreach (var file in imageFiles)
                    {
                        if (file.Length > 0)
                        {
                            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                            }

                            // pierwsze zdjęcie jako główne
                            if (string.IsNullOrEmpty(dish.ImagePath))
                                dish.ImagePath = "/images/dishes/" + uniqueFileName;

                            dish.DishImages.Add(new DishImage
                            {
                                ImagePath = "/images/dishes/" + uniqueFileName,
                                IsMainImage = dish.DishImages.Count == 0
                            });
                        }
                    }
                    if (dish.DishImages.Any() && !dish.DishImages.Any(d => d.IsMainImage))
                    {
                        var main = dish.DishImages.First();
                        main.IsMainImage = true;
                        dish.ImagePath = main.ImagePath;
                    }
                }

                _context.Add(dish);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(dish);
        }

        // GET: Dishes/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var dish = await _context.Dishes
                .Include(d => d.DishImages)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dish == null) return NotFound();

            return View(dish);
        }

        // POST: Dishes/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,Category")] Dish dish, List<IFormFile>? images)
        {
            if (id != dish.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var dishToUpdate = await _context.Dishes
                        .Include(d => d.DishImages)
                        .FirstOrDefaultAsync(d => d.Id == id);

                    if (dishToUpdate == null) return NotFound();

                    dishToUpdate.Name = dish.Name;
                    dishToUpdate.Description = dish.Description;
                    dishToUpdate.Price = dish.Price;
                    dishToUpdate.Category = dish.Category;

                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "dishes");
                    Directory.CreateDirectory(uploadsFolder);

                    if (images != null)
                    {
                        foreach (var image in images)
                        {
                            if (image.Length > 0)
                            {
                                var uniqueFileName = Guid.NewGuid() + "_" + image.FileName;
                                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                                using var stream = new FileStream(filePath, FileMode.Create);
                                await image.CopyToAsync(stream);

                                dishToUpdate.DishImages.Add(new DishImage
                                {
                                    ImagePath = "/images/dishes/" + uniqueFileName
                                });
                            }
                        }
                    }

                    _context.Update(dishToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DishExists(dish.Id)) return NotFound();
                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(dish);
        }

        // GET: Dishes/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var dish = await _context.Dishes.FirstOrDefaultAsync(m => m.Id == id);
            if (dish == null) return NotFound();

            return View(dish);
        }

        // POST: Dishes/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dish = await _context.Dishes
                .Include(d => d.DishImages)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dish != null)
            {
                // Usuń wszystkie zdjęcia
                foreach (var image in dish.DishImages)
                {
                    var path = Path.Combine(_environment.WebRootPath, image.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(path))
                        System.IO.File.Delete(path);
                }

                _context.Dishes.Remove(dish);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Rate dish
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rate(int dishId, int stars, string? comment)
        {
            if (stars < 1 || stars > 5)
            {
                ModelState.AddModelError("", "Nieprawidłowa liczba gwiazdek.");
            }

            var dish = await _context.Dishes.Include(d => d.Ratings).FirstOrDefaultAsync(d => d.Id == dishId);
            if (dish == null) return NotFound();

            _context.Ratings.Add(new Rating
            {
                DishId = dishId,
                Stars = stars,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = dishId });
        }

        private bool DishExists(int id) => _context.Dishes.Any(e => e.Id == id);

        // POST: Delete a single image
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var image = await _context.DishImages.FindAsync(imageId);
            if (image != null)
            {
                var path = Path.Combine(_environment.WebRootPath, image.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);

                _context.DishImages.Remove(image);
                await _context.SaveChangesAsync();
                return RedirectToAction("Edit", new { id = image.DishId });
            }

            return NotFound();
        }
    }
}
