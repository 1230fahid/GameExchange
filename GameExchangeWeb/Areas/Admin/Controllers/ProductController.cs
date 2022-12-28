using GameExchange.DataAccess.Data;
using GameExchange.DataAccess.Repository.IRepository;
using GameExchange.Models;
using GameExchange.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GameExchangeWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        //private readonly IProductRepository _db;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment hostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _hostEnvironment = hostEnvironment;
        }
        public IActionResult Index()
        {
            IEnumerable<Product> objProductList = _unitOfWork.Product.GetAll();
            return View(objProductList);

            //return View();
        }

        
        //GET action method
        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new()
            {
                Product = new(),
                CategoryList = _unitOfWork.Category.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
                CoverTypeList = _unitOfWork.CoverType.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                })
            };

			if (id == null || id == 0)
            {
                //here we would want to create product.
                //ViewBag.CategoryList = CategoryList;
                //ViewData["CoverTypeList"] = CoverTypeList;
                return View(productVM);
            }
            else
            {
                productVM.Product = _unitOfWork.Product.GetFirstOrDefault(i => i.Id == id);
                return View(productVM);
                //update product
            }
        }

        //POST action method
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductVM obj, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                //_db.CoverTypes.Update(obj);
                //_db.SaveChanges();
                //return View();

                //_db.Update(obj);
                //_db.Save();

                //_unitOfWork.Product.Update(obj);

                string wwwRootPath = _hostEnvironment.WebRootPath;
                if(file != null)
                {
                    string fileName = Guid.NewGuid().ToString();
                    var uploads = Path.Combine(wwwRootPath, @"images\products");
                    var extension = Path.GetExtension(file.FileName);

                    if(obj.Product.ImageUrl!= null)
                    {
                        var oldImagePath = Path.Combine(wwwRootPath, obj.Product.ImageUrl.TrimStart('\\'));
                        if(System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    using (var fileStreams = new FileStream(Path.Combine(uploads, fileName + extension), FileMode.Create))
                    {
                        file.CopyTo(fileStreams);
                    }
                    obj.Product.ImageUrl= @"\images\products\" + fileName + extension;
                }
                if(obj.Product.Id == 0)
                {
                    _unitOfWork.Product.Add(obj.Product);
                }
                else
                {
                    _unitOfWork.Product.Update(obj.Product);
                }
                _unitOfWork.Save();

                TempData["success"] = "Product created successfully";
                return RedirectToAction("Index");
            }

            return View(obj);
        }


        /*public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var productFromDb = _unitOfWork.Product.GetFirstOrDefault(c => c.Id == id);

            if (productFromDb == null)
            {
                return NotFound();
            }

            return View(productFromDb);
        }*/

        /*
		//POST action method
		[HttpPost, ActionName("Delete")] //This way we can handle a Delete POST without having the asp-action name and method name be the same.
		[ValidateAntiForgeryToken]
		public IActionResult DeletePOST(int? id)
		{
			//var obj = _db.CoverTypes.Find(id);
			//var obj = _db.GetFirstOrDefault(c => c.Id == id);
			var obj = _unitOfWork.Product.GetFirstOrDefault(c => c.Id == id);
			if (obj == null)
			{
				return NotFound();
			}
			//_db.CoverTypes.Remove(obj);
			//_db.SaveChanges();

			//_db.Remove(obj);
			//_db.Save();

			_unitOfWork.Product.Remove(obj);
			_unitOfWork.Save();
			TempData["success"] = "Product deleted successfully";
			return RedirectToAction("Index");

		}
        */

		#region API CALLS
		[HttpGet]
		public IActionResult GetAll()
		{
            var productList = _unitOfWork.Product.GetAll(includeProperties:"Category,CoverType");
            return Json(new {data = productList});

		}

        //POST action method
        //[HttpPost, ActionName("Delete")] //This way we can handle a Delete POST without having the asp-action name and method name be the same.
        [HttpDelete]
		public IActionResult Delete(int? id)
		{
			//var obj = _db.CoverTypes.Find(id);
			//var obj = _db.GetFirstOrDefault(c => c.Id == id);
			var obj = _unitOfWork.Product.GetFirstOrDefault(c => c.Id == id);
			if (obj == null)
			{
                //return NotFound();
                return Json(new { success = false, message = "Error while deleting" });
			}
			//_db.CoverTypes.Remove(obj);
			//_db.SaveChanges();

			//_db.Remove(obj);
			//_db.Save();

			var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, obj.ImageUrl.TrimStart('\\'));
			if (System.IO.File.Exists(oldImagePath))
			{
				System.IO.File.Delete(oldImagePath);
			}

			_unitOfWork.Product.Remove(obj);
			_unitOfWork.Save();
            return Json(new { success = true, message = "Delete Successful" });

		}

		#endregion

	}
}
