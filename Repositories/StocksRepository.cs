﻿using Entities;
using RepositoryContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ServiceContracts.DTO;
using System.Runtime.InteropServices;

namespace Repositories
{
    public class StocksRepository : IStocksRepository
    {
        private readonly OrdersDbContext _db;

        public StocksRepository(OrdersDbContext db)
        {
            _db = db;
        }

        public async Task<BuyOrder> CreateBuyOrder(BuyOrder buyOrder)
        {
            _db.BuyOrders.Add(buyOrder);
            await _db.SaveChangesAsync();
            return buyOrder;
        }

        public async Task<SellOrder> CreateSellOrder(SellOrder sellOrder)
        {
            _db.SellOrders.Add(sellOrder);
            await _db.SaveChangesAsync();
            return sellOrder;
        }

        public async Task<List<BuyOrder>> GetBuyOrders()
        {
            List<BuyOrder> all_buy_orders = await _db.BuyOrders.ToListAsync();
            return all_buy_orders;
        }

        public async Task<List<SellOrder>> GetSellOrders()
        {
            List<SellOrder> all_sell_orders = await _db.SellOrders.ToListAsync();
            return all_sell_orders;
        }
    }
}
