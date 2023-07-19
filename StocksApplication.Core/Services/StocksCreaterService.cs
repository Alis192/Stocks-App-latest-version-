﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities;
using ServiceContracts;
using ServiceContracts.DTO;
using Services.Helpers;
using Microsoft.Extensions.Logging;
using Serilog;
using SerilogTimings;
using StocksApplication.Core.Domain.RepositoryContracts;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using StocksApplication.Core.Helpers;
using Microsoft.AspNetCore.Identity;
using StocksApplication.Core.Domain.IdentityEntities;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using StocksApplication.Core.Domain.Exceptions;

namespace Services
{
    public class StocksCreaterService : IStocksCreaterService
    {
        private readonly IStocksRepository _stocksRepository;
        private readonly ILogger<StocksCreaterService> _logger;
        private readonly IDiagnosticContext _diagnosticContext; //to read data in the log
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserBalanceUpdate _userBalanceUpdate;

        public StocksCreaterService(IStocksRepository stocksRepository, ILogger<StocksCreaterService> logger, IDiagnosticContext diagnosticContext, IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager, IUserBalanceUpdate userBalanceUpdate)
        {
            _stocksRepository = stocksRepository;
            _logger = logger;
            _diagnosticContext = diagnosticContext;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _userBalanceUpdate = userBalanceUpdate;
        }

        public async Task<BuyOrderResponse> CreateBuyOrder(BuyOrderRequest? request)
        {
            if (request == null) throw new ArgumentNullException();

            ValidationHelpers.ModelValidation(request);

            request.UserId = Guid.Parse(_httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value);

            //Retrieving user
            ApplicationUser applicationUser = await _userManager.FindByIdAsync(request.UserId.ToString());

            //If user not found
            if (applicationUser == null)
            {
                throw new ArgumentException("User not found!");
            }

            BuyOrder buy_order = request.ToBuyOrder();

            double? totalAmount = TotalOrderPriceHelper.CalculateOrderPrice(buy_order);
                
            var chargedBalance = applicationUser.Balance - totalAmount;

            if (chargedBalance < 0) //
            {
                throw new InsufficientBalanceException("Insufficient balance. Please top up your account and try again.");
            } else
            {
                await _userBalanceUpdate.UpdateBalance(applicationUser, chargedBalance);
            }


            buy_order.BuyOrderID = Guid.NewGuid();

            buy_order.DateAndTimeOfOrder= DateTime.Now;

            await _stocksRepository.CreateBuyOrder(buy_order); //calling repository method

            //BuyOrderResponse order_response = buy_order.ToBuyOrderResponse();

            _diagnosticContext.Set("Buy Order", buy_order);


            return buy_order.ToBuyOrderResponse();
        }

        public async Task<SellOrderResponse> CreateSellOrder(SellOrderRequest? request)  
        {
            if (request == null) { throw new ArgumentNullException(); }

            ValidationHelpers.ModelValidation(request);

            request.UserId = Guid.Parse(_httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value);
            
            SellOrder order_from_request = request.ToSellOrder();

            order_from_request.SellOrderID = Guid.NewGuid();

            order_from_request.DateAndTimeOfOrder = DateTime.Now;

            try
            {
                await _stocksRepository.CreateSellOrder(order_from_request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding SellOrder: {ex.Message}");
                throw;
            }

            //_db.SellOrders.Add(order_from_request);
            //await _db.SaveChangesAsync();

            //SellOrderResponse sell_order_response = order_from_request.ToSellOrderResponse();

            return order_from_request.ToSellOrderResponse();

        }

    }
}
