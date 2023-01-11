using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameExchange.DataAccess.Data;
using GameExchange.DataAccess.Repository.IRepository;
using GameExchange.Models;

namespace GameExchange.DataAccess.Repository
{
	public class OrderDetailRepository : Repository<OrderDetail>, IOrderDetailRepository
	{
		private ApplicationDbContext _db;

		public OrderDetailRepository(ApplicationDbContext db) : base(db)
		{
			_db = db;
		}

		public void Update(OrderDetail obj)
		{
			_db.OrderDetails.Update(obj);
		}

	}
}
