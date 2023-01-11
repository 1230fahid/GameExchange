using System.Security.Claims;
using GameExchange.DataAccess.Repository.IRepository;
using GameExchange.Models;
using GameExchange.Models.ViewModels;
using GameExchange.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameExchangeWeb.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
	public class RefundController : Controller
	{
        [BindProperty]
        public OrderVM OrderVM { get; set; }
        private readonly IUnitOfWork _unitOfWork;
		public RefundController(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public IActionResult Index()
		{
			return View();
		}
        
        public IActionResult Details(int orderHeaderId)
        {
            return View();
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> orderHeaders;
            orderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser");
            switch (status)
            {
                case "inprocess":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusRefundInProcess && u.PaymentStatus == SD.PaymentStatusRefundInProcess);
                    break;
                case "refunded":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusRefunded && u.PaymentStatus == SD.PaymentStatusRefunded);
                    break;
                case "all":
                    orderHeaders = orderHeaders.Where(u => (u.OrderStatus == SD.StatusRefunded && u.PaymentStatus == SD.PaymentStatusRefunded) || (u.OrderStatus == SD.StatusRefundInProcess && u.PaymentStatus == SD.PaymentStatusRefundInProcess));
                    break;
            }
            return Json(new { data = orderHeaders });
        }
        #endregion
    }
}
