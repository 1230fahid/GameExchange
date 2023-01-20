using System.Security.Claims;
using GameExchange.DataAccess.Repository.IRepository;
using GameExchange.Models;
using GameExchange.Models.ViewModels;
using GameExchange.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;

namespace GameExchangeWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize] //whole controller authorized
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
		private readonly IEmailSender _emailSender;
		[BindProperty]
		public ShoppingCartVM ShoppingCartVM { get; set; }

        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender)
        {
            _unitOfWork= unitOfWork;
			_emailSender= emailSender;
        }

        //[AllowAnonymous] allows non authorized users to use a method
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity; //type-cast to claims identity
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "Product"),
                OrderHeader = new()
            };

            double totalPrice = 0;

            foreach(ShoppingCart shoppingCart in ShoppingCartVM.ListCart)
            {
                if(shoppingCart.Count > 0 && shoppingCart.Count <= 50)
                {
                    totalPrice += shoppingCart.Product.Price * shoppingCart.Count;
                }
				else if (shoppingCart.Count > 50 && shoppingCart.Count <= 100)
				{
					totalPrice += shoppingCart.Product.Price50 * shoppingCart.Count;
				}
				else if (shoppingCart.Count > 100)
				{
					totalPrice += shoppingCart.Product.Price100 * shoppingCart.Count;
				}
			}

            ShoppingCartVM.OrderHeader.OrderTotal = totalPrice;

            ViewData["Total"] = Math.Round(totalPrice, 2);

            return View(ShoppingCartVM);
        }

		public IActionResult Summary()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity; //type-cast to claims identity
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

			ShoppingCartVM = new ShoppingCartVM()
			{
				ListCart = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "Product"),
				OrderHeader = new()
			};

			ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(u => u.Id == claim.Value);
			ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
			ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
			ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
			ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
			ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
			ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;
			double totalPrice = 0;

			foreach (ShoppingCart shoppingCart in ShoppingCartVM.ListCart)
			{
				if (shoppingCart.Count > 0 && shoppingCart.Count <= 50)
				{
					totalPrice += shoppingCart.Product.Price * shoppingCart.Count;
				}
				else if (shoppingCart.Count > 50 && shoppingCart.Count <= 100)
				{
					totalPrice += shoppingCart.Product.Price50 * shoppingCart.Count;
				}
				else if (shoppingCart.Count > 100)
				{
					totalPrice += shoppingCart.Product.Price100 * shoppingCart.Count;
				}
			}

			ShoppingCartVM.OrderHeader.OrderTotal = totalPrice;

			ViewData["Total"] = Math.Round(totalPrice, 2);
			return View(ShoppingCartVM);
		}

		[HttpPost]
		[ActionName("Summary")]
		[ValidateAntiForgeryToken]
		public IActionResult SummaryPOST(ShoppingCartVM ShoppingCartVM)
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity; //type-cast to claims identity
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

			ShoppingCartVM.ListCart = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "Product");
			
			ShoppingCartVM.OrderHeader.OrderDate = System.DateTime.Now;
			ShoppingCartVM.OrderHeader.ApplicationUserId = claim.Value;


			double totalPrice = 0;

			foreach (ShoppingCart shoppingCart in ShoppingCartVM.ListCart)
			{
				if (shoppingCart.Count > 0 && shoppingCart.Count <= 50)
				{
					totalPrice += shoppingCart.Product.Price * shoppingCart.Count;
				}
				else if (shoppingCart.Count > 50 && shoppingCart.Count <= 100)
				{
					totalPrice += shoppingCart.Product.Price50 * shoppingCart.Count;
				}
				else if (shoppingCart.Count > 100)
				{
					totalPrice += shoppingCart.Product.Price100 * shoppingCart.Count;
				}
			}

			ApplicationUser applicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(u => u.Id == claim.Value);
			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
				ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
				ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
			}
			else
			{
				ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
				ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
			}

			_unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
			_unitOfWork.Save();

			ShoppingCartVM.OrderHeader.OrderTotal = totalPrice;
			ViewData["Total"] = Math.Round(totalPrice, 2);

			foreach (ShoppingCart shoppingCart in ShoppingCartVM.ListCart)
			{
				OrderDetail orderDetail = new()
				{
					ProductId = shoppingCart.ProductId,
					OrderId = ShoppingCartVM.OrderHeader.Id,
					Price = totalPrice,
					Count = shoppingCart.Count
				};
				_unitOfWork.OrderDetail.Add(orderDetail);
				_unitOfWork.Save();
			}


			if (applicationUser.CompanyId.GetValueOrDefault() == 0) 
			{
				//stripe settings
				var domain = "https://localhost:44322/";
				var options = new SessionCreateOptions
				{
					LineItems = new List<SessionLineItemOptions>()
					,
					Mode = "payment",
					SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
					CancelUrl = domain + $"customer/cart/index",
				};

				foreach (var item in ShoppingCartVM.ListCart)
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
                _unitOfWork.OrderHeader.UpdateStripePaymentID(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
				_unitOfWork.Save();

				Response.Headers.Add("Location", session.Url);
				return new StatusCodeResult(303);
			}
			else
			{
				return RedirectToAction("OrderConfirmation", "Cart", new {id = ShoppingCartVM.OrderHeader.Id});
			}

		}

		public IActionResult OrderConfirmation(int id)
		{
			OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == id, includeProperties:"ApplicationUser");
			if(orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
			{
				var service = new SessionService();
				Session session = service.Get(orderHeader.SessionId);
				//check the stripe status

				if (session.PaymentStatus.ToLower() == "paid")
				{
					_unitOfWork.OrderHeader.UpdateStripePaymentID(id, orderHeader.SessionId, session.PaymentIntentId);
					_unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
					_unitOfWork.Save();
				}
			}

			//_emailSender.SendEmailAsync(orderHeader.ApplicationUser.Email, "New Order - Game Exchange", "<p>New Order Created</p>"); //send email when order is confirmed  

			List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
			HttpContext.Session.Clear(); //once order goes through
			_unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
			_unitOfWork.Save();
			return View(id);
		}

		public IActionResult Plus(int cartId)
        {
            var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(u => u.Id == cartId);
            _unitOfWork.ShoppingCart.IncrementCount(cart, 1);
			Product product = _unitOfWork.Product.GetFirstOrDefault(u => u.Id == cart.ProductId);
			product.Qty -= 1;
            _unitOfWork.Save();
            return RedirectToAction("Index");
        }

		public IActionResult Minus(int cartId)
		{
			var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(u => u.Id == cartId);
			Product product = _unitOfWork.Product.GetFirstOrDefault(u => u.Id == cart.ProductId);
			product.Qty += 1;
			if (cart.Count <= 1)
            {
				_unitOfWork.ShoppingCart.Remove(cart);
                var count = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
                HttpContext.Session.SetInt32(SD.SessionCart, count);
            }
            else
            {
				_unitOfWork.ShoppingCart.DecrementCount(cart, 1);
			}
			_unitOfWork.Save();
			return RedirectToAction("Index");
		}

		public IActionResult Remove(int cartId)
		{
			var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(u => u.Id == cartId);
			Product product = _unitOfWork.Product.GetFirstOrDefault(u => u.Id == cart.ProductId);
			product.Qty += cart.Count;
			_unitOfWork.ShoppingCart.Remove(cart);
			_unitOfWork.Save();
			var count = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
			HttpContext.Session.SetInt32(SD.SessionCart, count);
			return RedirectToAction("Index");
		}
	}
}
