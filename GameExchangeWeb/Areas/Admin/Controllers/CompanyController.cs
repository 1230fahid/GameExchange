using GameExchange.DataAccess.Repository.IRepository;
using GameExchange.Models.ViewModels;
using GameExchange.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using GameExchange.Utility;

namespace GameExchangeWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
	[Authorize(Roles = SD.Role_Admin)]
	public class CompanyController : Controller
    {
        //private readonly IProductRepository _db;
        private readonly IUnitOfWork _unitOfWork;

        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            //IEnumerable<Company> objCompanyList = _unitOfWork.Company.GetAll();
            //return View(objCompanyList);

            return View();
        }


        //GET action method
        public IActionResult Upsert(int? id)
        {
            Company company = new();

            if(id != null && id != 0)
            {
                company = _unitOfWork.Company.GetFirstOrDefault(i => i.Id == id);
            }

            return View(company);
        }

        //POST action method
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Company obj, IFormFile? file)
        {
            if (ModelState.IsValid)
            {                
                if (obj.Id == 0)
                {
                    _unitOfWork.Company.Add(obj);
                    TempData["success"] = "Company created successfully";
                }
                else
                {
                    _unitOfWork.Company.Update(obj);
                    TempData["success"] = "Company updated successfully";
                }
                _unitOfWork.Save();

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

            var productFromDb = _unitOfWork.Company.GetFirstOrDefault(c => c.Id == id);

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
			var obj = _unitOfWork.Company.GetFirstOrDefault(c => c.Id == id);
			if (obj == null)
			{
				return NotFound();
			}
			//_db.CoverTypes.Remove(obj);
			//_db.SaveChanges();

			//_db.Remove(obj);
			//_db.Save();

			_unitOfWork.Company.Remove(obj);
			_unitOfWork.Save();
			TempData["success"] = "Company deleted successfully";
			return RedirectToAction("Index");

		}
        */

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var companyList = _unitOfWork.Company.GetAll();
            return Json(new { data = companyList });

        }

        //POST action method
        //[HttpPost, ActionName("Delete")] //This way we can handle a Delete POST without having the asp-action name and method name be the same.
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var obj = _unitOfWork.Company.GetFirstOrDefault(c => c.Id == id);
            if (obj == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            _unitOfWork.Company.Remove(obj);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete Successful" });

        }
		#endregion
	}
}
