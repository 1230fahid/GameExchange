using System.Diagnostics;
using System.Security.Claims;
using GameExchange.DataAccess.Repository;
using GameExchange.DataAccess.Repository.IRepository;
using GameExchange.Models;
using GameExchange.Models.ViewModels;
using GameExchange.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace GameExchangeWeb.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize]
	public class OrderController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IEmailSender _emailSender;

		[BindProperty]
		public OrderVM OrderVM { get; set; }
		public OrderController(IUnitOfWork unitOfWork, IEmailSender emailSender)
		{
			_unitOfWork= unitOfWork;
			_emailSender = emailSender;
		}
		public IActionResult Index()
		{
			return View();
		}

        public IActionResult Details(int orderId)
        {
			OrderVM = new OrderVM()
			{
				OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == orderId, includeProperties: "ApplicationUser"),
				OrderDetailList = _unitOfWork.OrderDetail.GetAll(u => u.OrderId == orderId, includeProperties: "Product"),
			};
            return View(OrderVM);
        }

		[ActionName("Details")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Details_PAY_NOW(int orderId)
		{
			OrderVM.OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
			OrderVM.OrderDetailList = _unitOfWork.OrderDetail.GetAll(u => u.OrderId == OrderVM.OrderHeader.Id, includeProperties: "Product");

			//stripe settings
			var domain = "https://localhost:44322/";
			var options = new SessionCreateOptions
			{
				LineItems = new List<SessionLineItemOptions>()
				,
				Mode = "payment",
				SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader.Id}",
				CancelUrl = domain + $"admin/order/details?orderId={OrderVM.OrderHeader.Id}",
			};

			foreach (var item in OrderVM.OrderDetailList)
			{
				double price = 0;
				if (item.Count <= 50)
				{
					price = item.Product.Price;
				}
				else if (item.Count > 50 && item.Count <= 100)
				{
					price = item.Product.Price50;
				}
				else if (item.Count > 100)
				{
					price = item.Product.Price100;
				}

				var sessionLineItem = new SessionLineItemOptions
				{
					PriceData = new SessionLineItemPriceDataOptions
					{
						UnitAmount = (long)(price * 100), //20.00 -> 2000
						Currency = "usd",
						ProductData = new SessionLineItemPriceDataProductDataOptions
						{
							Name = "Game",
						},
					},
					Quantity = item.Count,
				};
				options.LineItems.Add(sessionLineItem);
			}

			var service = new SessionService();
			Session session = service.Create(options);

			//ShoppingCartVM.OrderHeader.SessionId = session.Id;
			//ShoppingCartVM.OrderHeader.PaymentIntentId = session.PaymentIntentId;
			_unitOfWork.OrderHeader.UpdateStripePaymentID(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
			_unitOfWork.Save();

			Response.Headers.Add("Location", session.Url);
			return new StatusCodeResult(303);
		}


		public IActionResult PaymentConfirmation(int orderHeaderId)
		{
			OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == orderHeaderId);
			if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment || orderHeader.PaymentStatus == SD.PaymentStatusPending)
			{
				var service = new SessionService();
				Session session = service.Get(orderHeader.SessionId);
				//check the stripe status

				if (session.PaymentStatus.ToLower() == "paid")
				{
					if(orderHeader.OrderStatus == SD.StatusPending)
					{
						orderHeader.OrderStatus = SD.StatusApproved;
					}
					_unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
					_unitOfWork.Save();
				}
			}
			return View(orderHeaderId);
		}


		[HttpPost]
		[Authorize(Roles =SD.Role_Admin + "," + SD.Role_Employee)]
		[ValidateAntiForgeryToken]
		public IActionResult UpdateOrderDetail()
		{
			var orderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id, tracked:false);
			orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
			orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
			orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
			orderHeaderFromDb.City = OrderVM.OrderHeader.City;
			orderHeaderFromDb.State = OrderVM.OrderHeader.State;
			orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;

			if(OrderVM.OrderHeader.Carrier != null)
			{
				orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
			}

			if (OrderVM.OrderHeader.TrackingNumber != null)
			{
				orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
			}

			_unitOfWork.OrderHeader.Update(orderHeaderFromDb);
			_unitOfWork.Save();
			TempData["Success"] = "Order Details Updated Successfully";
			return RedirectToAction("Details", "Order", new { orderId = orderHeaderFromDb.Id });
		}


		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		[ValidateAntiForgeryToken]
		public IActionResult StartProcessing()
		{
			//var orderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id, tracked: false);
			_unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusInProcess);
			//_unitOfWork.OrderHeader.Update(orderHeaderFromDb);
			_unitOfWork.Save();
			TempData["Success"] = "Order Status Updated Successfully";
			return RedirectToAction("Details", "Order", new { orderId = OrderVM.OrderHeader.Id });
		}

		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		[ValidateAntiForgeryToken]
		public IActionResult ShipOrder()
		{
			var orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id, tracked: false);
			orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
			orderHeader.Carrier = OrderVM.OrderHeader.Carrier;
			orderHeader.OrderStatus = SD.StatusShipped;
			orderHeader.ShippingDate = DateTime.Now;
			if(orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
			{
				orderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
			}
			_unitOfWork.OrderHeader.Update(orderHeader);
			_unitOfWork.Save();
			TempData["Success"] = "Order Shipped Successfully";
			return RedirectToAction("Details", "Order", new { orderId = OrderVM.OrderHeader.Id });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles =SD.Role_User_Indi + "," + SD.Role_User_Comp)]
		public IActionResult RefundRequest()
		{
			OrderHeader refundedOrder = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id);
			refundedOrder.OrderStatus = SD.StatusRefundInProcess;
			refundedOrder.PaymentStatus = SD.PaymentStatusRefundInProcess;
			_unitOfWork.Save();
			return RedirectToAction("Details", "Order", new { orderId = refundedOrder.Id });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = SD.Role_User_Indi + "," + SD.Role_User_Comp)]
		public IActionResult CancelRequest()
		{
			OrderHeader refundedOrder = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id);
			refundedOrder.OrderStatus = SD.StatusCancelInProcess;
			refundedOrder.PaymentStatus = SD.PaymentStatusCancelInProcess;
			_unitOfWork.Save();
			return RedirectToAction("Details", "Order", new { orderId = refundedOrder.Id });
		}

		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		[ValidateAntiForgeryToken]
		public IActionResult CancelOrder()
		{
			var orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id, tracked: false);
			_unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
			_unitOfWork.Save();
			TempData["Success"] = "Order Cancelled Successfully";
			_emailSender.SendEmailAsync(orderHeader.ApplicationUser.UserName, "Cancel Request", "<p>Order Cancelled</p>");

			IEnumerable<OrderDetail> orderDetails = _unitOfWork.OrderDetail.GetAll(u => u.OrderId == OrderVM.OrderHeader.Id);
			foreach(var orderDetail in orderDetails)
			{
				GameExchange.Models.Product product = _unitOfWork.Product.GetFirstOrDefault(u => u.Id == orderDetail.ProductId);
				product.Qty += orderDetail.Count;
				_unitOfWork.Save();
			}


			return RedirectToAction("Details", "Order", new { orderId = orderHeader.Id });
		}

		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		[ValidateAntiForgeryToken]
		public IActionResult RefundOrder()
		{
			var orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id, tracked: false);
			var options = new RefundCreateOptions //if we don't give an amount, then the default amount is sent back, which is just however much the payment was
			{
				Reason = RefundReasons.RequestedByCustomer,
				PaymentIntent = orderHeader.PaymentIntentId,
			};
			var service = new RefundService();
			Refund refund = service.Create(options); //goes to the actual portal and makes the actual refund on stripe
			_unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusRefunded, SD.PaymentStatusRefunded);
			_unitOfWork.Save();
			TempData["Success"] = "Order Refunded Successfully";

			IEnumerable<OrderDetail> orderDetails = _unitOfWork.OrderDetail.GetAll(u => u.OrderId == OrderVM.OrderHeader.Id);
			foreach (var orderDetail in orderDetails)
			{
				GameExchange.Models.Product product = _unitOfWork.Product.GetFirstOrDefault(u => u.Id == orderDetail.ProductId);
				product.Qty += orderDetail.Count;
				_unitOfWork.Save();
			}

			ApplicationUser User = _unitOfWork.ApplicationUser.GetFirstOrDefault(u => u.Id == orderHeader.ApplicationUserId);
			_emailSender.SendEmailAsync(User.UserName, "Refund Request", "<p>Order Refunded</p>");
			return RedirectToAction("Details", "Order", new { orderId = orderHeader.Id });
		}

		#region API CALLS
		[HttpGet]
		public IActionResult GetAll(string status)
		{
			IEnumerable<OrderHeader> orderHeaders;
            //orderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser"); //include ApplicationUser properties so we can get Order Details


            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
			{
				orderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser");
			}
			else
			{
				var claimsIdentity = (ClaimsIdentity)User.Identity;
				var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
				orderHeaders = _unitOfWork.OrderHeader.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "ApplicationUser"); //gets the individual or company's specific orders.
			}
			
			switch(status)
			{
                case "pending":
					//pending = "active text-white";
					orderHeaders = orderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
					break;
				case "inprocess":
                    //inprocess = "active text-white";
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    //completed = "active text-white";
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    //completed = "active text-white";
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;
                case "all":
                    break;
            }
			
			return Json(new { data = orderHeaders});
		}
		#endregion
	}
}
