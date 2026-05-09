using GymBro.Application.Orders;
using GymBro.Application.Payments;
using GymBro.Core;
using GymBro.Infrastructure;
using GymBro.Infrastructure.Orders;
using GymBro.Infrastructure.Payments;
using GymBro.Infrastructure.Shopping;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Tests;

public class CheckoutOrderPaymentFlowTests
{
    [Fact]
    public async Task PlaceOrderAsync_BankTransfer_CreatesOrderPaymentAndReservesStock()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var auditService = new TestPaymentAuditService();

        int userId;
        int productId;
        int paymentMethodId;

        await using (var seedContext = database.CreateContext())
        {
            var seeded = await SeedCheckoutScenarioAsync(
                seedContext,
                paymentMethodName: "Chuyen khoan QR",
                initialStock: 10,
                unitPrice: 100_000m);

            userId = seeded.UserId;
            productId = seeded.ProductId;
            paymentMethodId = seeded.PaymentMethodId;
        }

        int createdOrderId;

        await using (var serviceContext = database.CreateContext())
        {
            var service = CreateCartCheckoutService(serviceContext, auditService);

            var addToCart = await service.AddToCartAsync([], productId, 2);
            Assert.True(addToCart.Succeeded);

            var result = await service.PlaceOrderAsync(userId, addToCart.Items, paymentMethodId);

            Assert.True(result.Succeeded);
            Assert.True(result.RedirectToPayment);
            createdOrderId = Assert.IsType<int>(result.OrderId);
        }

        await using var verifyContext = database.CreateContext();

        var order = await verifyContext.Orders
            .Include(item => item.OrderDetails)
            .Include(item => item.Payments)
            .SingleAsync(item => item.Id == createdOrderId);

        var payment = Assert.Single(order.Payments);
        var orderDetail = Assert.Single(order.OrderDetails);
        var product = await verifyContext.Products.SingleAsync(item => item.Id == productId);
        var inventory = await verifyContext.InventoryTransactions.SingleAsync(item => item.OrderId == createdOrderId);

        Assert.True(OrderStatus.Equals(order.Status, OrderStatus.Pending));
        Assert.Equal(200_000m, order.TotalAmount);
        Assert.Equal(2, orderDetail.Quantity);
        Assert.Equal(100_000m, orderDetail.Price);
        Assert.Equal("Banking", payment.PaymentMethod);
        Assert.True(PaymentStatus.Equals(payment.Status, PaymentStatus.AwaitingPayment));
        Assert.Equal(200_000m, payment.Amount);
        Assert.Equal(8, product.StockQuantity);
        Assert.Equal(-2, inventory.QuantityChange);
    }

    [Fact]
    public async Task ConfirmPaymentAsync_UpdatesPaymentAndOrderStatuses()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var auditService = new TestPaymentAuditService();

        int userId;
        int orderId;

        await using (var seedContext = database.CreateContext())
        {
            var seeded = await SeedPendingTransferOrderAsync(seedContext);
            userId = seeded.UserId;
            orderId = seeded.OrderId;
        }

        await using (var serviceContext = database.CreateContext())
        {
            var service = CreateCartCheckoutService(serviceContext, auditService);
            var result = await service.ConfirmPaymentAsync(userId, orderId, "customer01");

            Assert.True(result.Succeeded);
            Assert.Equal(orderId, result.OrderId);
        }

        await using var verifyContext = database.CreateContext();

        var order = await verifyContext.Orders
            .Include(item => item.Payments)
            .SingleAsync(item => item.Id == orderId);

        var payment = Assert.Single(order.Payments);

        Assert.True(OrderStatus.Equals(order.Status, OrderStatus.PendingPaymentVerification));
        Assert.True(PaymentStatus.Equals(payment.Status, PaymentStatus.PendingVerification));
        Assert.Contains(
            auditService.Entries,
            entry => entry.PaymentId == payment.Id && entry.ActionType == PaymentAuditAction.CustomerConfirmed);
    }

    [Fact]
    public async Task ApprovePaymentAsync_MarksPaymentPaidAndMovesOrderToProcessing()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var auditService = new TestPaymentAuditService();

        int paymentId;
        int orderId;

        await using (var seedContext = database.CreateContext())
        {
            var seeded = await SeedOrderForPaymentWorkflowAsync(
                seedContext,
                orderStatus: OrderStatus.PendingPaymentVerification,
                paymentStatus: PaymentStatus.PendingVerification,
                reserveStock: true);

            paymentId = seeded.PaymentId;
            orderId = seeded.OrderId;
        }

        await using (var serviceContext = database.CreateContext())
        {
            var service = CreatePaymentWorkflowService(serviceContext, auditService);
            var result = await service.ApprovePaymentAsync(paymentId, "admin01", "Da doi soat");

            Assert.True(result.Succeeded);
        }

        await using var verifyContext = database.CreateContext();

        var payment = await verifyContext.Payments.SingleAsync(item => item.Id == paymentId);
        var order = await verifyContext.Orders.SingleAsync(item => item.Id == orderId);

        Assert.True(PaymentStatus.Equals(payment.Status, PaymentStatus.Paid));
        Assert.True(OrderStatus.Equals(order.Status, OrderStatus.Processing));
        Assert.Contains(
            auditService.Entries,
            entry => entry.PaymentId == paymentId && entry.ActionType == PaymentAuditAction.AdminApproved);
    }

    [Fact]
    public async Task RejectPaymentAsync_RestoresStockAndCancelsOrder()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var auditService = new TestPaymentAuditService();

        int paymentId;
        int orderId;
        int productId;

        await using (var seedContext = database.CreateContext())
        {
            var seeded = await SeedOrderForPaymentWorkflowAsync(
                seedContext,
                orderStatus: OrderStatus.PendingPaymentVerification,
                paymentStatus: PaymentStatus.PendingVerification,
                reserveStock: true);

            paymentId = seeded.PaymentId;
            orderId = seeded.OrderId;
            productId = seeded.ProductId;
        }

        await using (var serviceContext = database.CreateContext())
        {
            var service = CreatePaymentWorkflowService(serviceContext, auditService);
            var result = await service.RejectPaymentAsync(paymentId, "admin01", "Khong tim thay giao dich");

            Assert.True(result.Succeeded);
        }

        await using var verifyContext = database.CreateContext();

        var payment = await verifyContext.Payments.SingleAsync(item => item.Id == paymentId);
        var order = await verifyContext.Orders.SingleAsync(item => item.Id == orderId);
        var product = await verifyContext.Products.SingleAsync(item => item.Id == productId);
        var inventoryChanges = await verifyContext.InventoryTransactions
            .Where(item => item.OrderId == orderId)
            .OrderBy(item => item.Id)
            .ToListAsync();

        Assert.True(PaymentStatus.Equals(payment.Status, PaymentStatus.Failed));
        Assert.True(OrderStatus.Equals(order.Status, OrderStatus.CancelledPaymentFailed));
        Assert.Equal(5, product.StockQuantity);
        Assert.Equal(2, inventoryChanges.Count);
        Assert.Equal(-2, inventoryChanges[0].QuantityChange);
        Assert.Equal(2, inventoryChanges[1].QuantityChange);
        Assert.Contains(
            auditService.Entries,
            entry => entry.PaymentId == paymentId && entry.ActionType == PaymentAuditAction.AdminRejected);
    }

    [Fact]
    public async Task UpdateStatusAsync_CancellingOrder_RestoresStockAndCancelsPayment()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var auditService = new TestPaymentAuditService();

        int orderId;
        int paymentId;
        int productId;

        await using (var seedContext = database.CreateContext())
        {
            var seeded = await SeedOrderForPaymentWorkflowAsync(
                seedContext,
                orderStatus: OrderStatus.Pending,
                paymentStatus: PaymentStatus.AwaitingPayment,
                reserveStock: true);

            orderId = seeded.OrderId;
            paymentId = seeded.PaymentId;
            productId = seeded.ProductId;
        }

        await using (var serviceContext = database.CreateContext())
        {
            var service = CreateOrderAdminService(serviceContext, auditService);
            var result = await service.UpdateStatusAsync(new UpdateOrderStatusRequest
            {
                Id = orderId,
                Status = OrderStatus.Cancelled,
                AdminUsername = "admin01"
            });

            Assert.True(result.Succeeded);
        }

        await using var verifyContext = database.CreateContext();

        var order = await verifyContext.Orders.SingleAsync(item => item.Id == orderId);
        var payment = await verifyContext.Payments.SingleAsync(item => item.Id == paymentId);
        var product = await verifyContext.Products.SingleAsync(item => item.Id == productId);
        var inventoryChanges = await verifyContext.InventoryTransactions
            .Where(item => item.OrderId == orderId)
            .OrderBy(item => item.Id)
            .ToListAsync();

        Assert.True(OrderStatus.Equals(order.Status, OrderStatus.Cancelled));
        Assert.True(PaymentStatus.Equals(payment.Status, PaymentStatus.Cancelled));
        Assert.Equal(5, product.StockQuantity);
        Assert.Equal(2, inventoryChanges.Count);
        Assert.Equal(-2, inventoryChanges[0].QuantityChange);
        Assert.Equal(2, inventoryChanges[1].QuantityChange);
        Assert.Contains(
            auditService.Entries,
            entry => entry.PaymentId == paymentId && entry.ActionType == PaymentAuditAction.OrderCancelled);
    }

    private static CartCheckoutService CreateCartCheckoutService(
        GymBroDbContext context,
        TestPaymentAuditService auditService)
    {
        return new CartCheckoutService(
            context,
            new OrderInventoryService(context),
            auditService,
            SqliteTestDatabase.CreateConfiguration());
    }

    private static PaymentWorkflowService CreatePaymentWorkflowService(
        GymBroDbContext context,
        TestPaymentAuditService auditService)
    {
        return new PaymentWorkflowService(
            context,
            auditService,
            new OrderInventoryService(context));
    }

    private static OrderAdminService CreateOrderAdminService(
        GymBroDbContext context,
        TestPaymentAuditService auditService)
    {
        return new OrderAdminService(
            context,
            auditService,
            new OrderInventoryService(context));
    }

    private static async Task<(int UserId, int ProductId, int PaymentMethodId)> SeedCheckoutScenarioAsync(
        GymBroDbContext context,
        string paymentMethodName,
        int initialStock,
        decimal unitPrice)
    {
        var category = new Category
        {
            CategoryName = "Strength"
        };

        var product = new Product
        {
            ProductName = "Barbell",
            Category = category,
            Price = unitPrice,
            StockQuantity = initialStock,
            ImageURL = "/images/barbell.png"
        };

        var user = new User
        {
            Username = "customer01",
            Password = "hashed-password",
            FullName = "Customer 01",
            Email = "customer01@test.local",
            Address = "Ho Chi Minh",
            Role = "User"
        };

        var paymentMethod = new PaymentMethod
        {
            MethodName = paymentMethodName,
            Description = paymentMethodName,
            IsActive = true
        };

        context.AddRange(category, product, user, paymentMethod);
        await context.SaveChangesAsync();

        return (user.Id, product.Id, paymentMethod.Id);
    }

    private static async Task<(int UserId, int OrderId)> SeedPendingTransferOrderAsync(
        GymBroDbContext context)
    {
        var seeded = await SeedOrderForPaymentWorkflowAsync(
            context,
            orderStatus: OrderStatus.Pending,
            paymentStatus: PaymentStatus.AwaitingPayment,
            reserveStock: false);

        return (seeded.UserId, seeded.OrderId);
    }

    private static async Task<(int UserId, int OrderId, int PaymentId, int ProductId)> SeedOrderForPaymentWorkflowAsync(
        GymBroDbContext context,
        string orderStatus,
        string paymentStatus,
        bool reserveStock)
    {
        var category = new Category
        {
            CategoryName = "Cardio"
        };

        var product = new Product
        {
            ProductName = "Treadmill",
            Category = category,
            Price = 150_000m,
            StockQuantity = reserveStock ? 3 : 5,
            ImageURL = "/images/treadmill.png"
        };

        var user = new User
        {
            Username = "workflow-user",
            Password = "hashed-password",
            FullName = "Workflow User",
            Email = "workflow@test.local",
            Address = "Da Nang",
            Role = "User"
        };

        var order = new Order
        {
            User = user,
            OrderDate = DateTime.Now.AddHours(-1),
            Status = orderStatus,
            TotalAmount = 300_000m,
            OrderDetails =
            {
                new OrderDetail
                {
                    Product = product,
                    Quantity = 2,
                    Price = 150_000m
                }
            }
        };

        var payment = new Payment
        {
            Order = order,
            PaymentDate = DateTime.Now.AddMinutes(-30),
            Amount = 300_000m,
            PaymentMethod = "Banking",
            Status = paymentStatus
        };

        context.AddRange(category, user, order, payment);
        await context.SaveChangesAsync();

        if (reserveStock)
        {
            context.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = product.Id,
                OrderId = order.Id,
                QuantityChange = -2,
                TransactionType = "Ban hang",
                Note = "Reserved for test order",
                CreatedDate = DateTime.Now.AddMinutes(-29)
            });

            await context.SaveChangesAsync();
        }

        return (user.Id, order.Id, payment.Id, product.Id);
    }
}
