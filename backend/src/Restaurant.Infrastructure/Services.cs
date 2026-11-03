using System.Security.Cryptography;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Restaurant.Application;
using Restaurant.Domain;

namespace Restaurant.Infrastructure;

// ─────────────────────────────────────────────────────────────────────────────
// Customer Service
// ─────────────────────────────────────────────────────────────────────────────

public sealed class CustomerService(BusinessDbContext db) : ICustomerService
{
    public async Task<CustomerDto?> GetByWhatsAppId(string whatsAppId, CancellationToken ct)
    {
        return await db.Customers.AsNoTracking()
            .Where(x => x.WhatsAppId == whatsAppId)
            .Select(x => new CustomerDto(x.Id, x.WhatsAppId, x.Name, x.Phone, x.CreatedAt, x.LastActiveAt))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<CustomerDto> GetOrCreate(string whatsAppId, CancellationToken ct)
    {
        var existing = await db.Customers.SingleOrDefaultAsync(x => x.WhatsAppId == whatsAppId, ct);
        if (existing is not null)
        {
            existing.RecordActivity();
            await db.SaveChangesAsync(ct);
            return new CustomerDto(existing.Id, existing.WhatsAppId, existing.Name, existing.Phone, existing.CreatedAt, existing.LastActiveAt);
        }

        var customer = new Customer(whatsAppId);
        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        return new CustomerDto(customer.Id, customer.WhatsAppId, customer.Name, customer.Phone, customer.CreatedAt, customer.LastActiveAt);
    }

    public async Task<CustomerDto?> GetById(Guid id, CancellationToken ct)
    {
        return await db.Customers.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new CustomerDto(x.Id, x.WhatsAppId, x.Name, x.Phone, x.CreatedAt, x.LastActiveAt))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<CustomerDto>> List(int page, int pageSize, CancellationToken ct)
    {
        return await db.Customers.AsNoTracking()
            .OrderByDescending(x => x.LastActiveAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new CustomerDto(x.Id, x.WhatsAppId, x.Name, x.Phone, x.CreatedAt, x.LastActiveAt))
            .ToListAsync(ct);
    }

    public async Task<CustomerDto> Update(Guid id, UpdateCustomerRequest request, CancellationToken ct)
    {
        var customer = await db.Customers.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InvalidOperationException("Customer not found.");
        customer.UpdateProfile(request.Name, request.Phone);
        await db.SaveChangesAsync(ct);
        return new CustomerDto(customer.Id, customer.WhatsAppId, customer.Name, customer.Phone, customer.CreatedAt, customer.LastActiveAt);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Cart Service
// ─────────────────────────────────────────────────────────────────────────────

public sealed class CartService(BusinessDbContext db, ICatalogQueries catalog) : ICartService
{
    public async Task<CartDto> GetOrCreateCart(Guid customerId, CancellationToken ct)
    {
        var cart = await db.Carts.Include(x => x.Items).SingleOrDefaultAsync(x => x.CustomerId == customerId, ct);
        if (cart is null)
        {
            cart = new Cart(customerId);
            db.Carts.Add(cart);
            await db.SaveChangesAsync(ct);
        }
        return ToDto(cart);
    }

    public async Task<CartDto?> GetCartByCustomerId(Guid customerId, CancellationToken ct)
    {
        var cart = await db.Carts.AsNoTracking().Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.CustomerId == customerId, ct);
        return cart is null ? null : ToDto(cart);
    }

    public async Task<CartItemDto> AddItem(Guid customerId, AddToCartRequest request, CancellationToken ct)
    {
        var menuItem = await catalog.ItemById(request.MenuItemId, ct)
            ?? throw new InvalidOperationException("Menu item not found.");
        if (!menuItem.Available)
            throw new InvalidOperationException("Menu item is not available.");

        var cart = await db.Carts.Include(x => x.Items).SingleOrDefaultAsync(x => x.CustomerId == customerId, ct);

        if (cart is null)
        {
            // New cart - create and add via domain method
            cart = new Cart(customerId);
            db.Carts.Add(cart);
            var item = cart.AddItem(request.MenuItemId, menuItem.Name, menuItem.PriceMinor, request.Quantity, request.Instructions);
            await db.SaveChangesAsync(ct);
            return new CartItemDto(item.Id, item.MenuItemId, item.Name, item.UnitPriceMinor, item.Quantity, item.Instructions, item.LineTotalMinor);
        }

        // Existing cart - check for existing item with same menu item and instructions
        var existing = cart.Items.FirstOrDefault(x => x.MenuItemId == request.MenuItemId && x.Instructions == request.Instructions);
        if (existing is not null)
        {
            // Update existing item quantity
            cart.UpdateItemQuantity(existing.Id, existing.Quantity + request.Quantity);
            await db.SaveChangesAsync(ct);
            return new CartItemDto(existing.Id, existing.MenuItemId, existing.Name, existing.UnitPriceMinor, existing.Quantity + request.Quantity, existing.Instructions, existing.UnitPriceMinor * (existing.Quantity + request.Quantity));
        }

        // Add new item directly to DbContext to avoid cart tracking issues
        var newItem = new CartItem(cart.Id, request.MenuItemId, menuItem.Name, menuItem.PriceMinor, request.Quantity, request.Instructions);
        db.CartItems.Add(newItem);
        await db.SaveChangesAsync(ct);
        return new CartItemDto(newItem.Id, newItem.MenuItemId, newItem.Name, newItem.UnitPriceMinor, newItem.Quantity, newItem.Instructions, newItem.LineTotalMinor);
    }

    public async Task<CartDto> UpdateItemQuantity(Guid customerId, Guid itemId, int quantity, CancellationToken ct)
    {
        var cart = await db.Carts.Include(x => x.Items).SingleOrDefaultAsync(x => x.CustomerId == customerId, ct)
            ?? throw new InvalidOperationException("Cart not found.");
        cart.UpdateItemQuantity(itemId, quantity);
        await db.SaveChangesAsync(ct);
        return ToDto(cart);
    }

    public async Task<CartDto> RemoveItem(Guid customerId, Guid itemId, CancellationToken ct)
    {
        var cart = await db.Carts.Include(x => x.Items).SingleOrDefaultAsync(x => x.CustomerId == customerId, ct)
            ?? throw new InvalidOperationException("Cart not found.");
        cart.RemoveItem(itemId);
        await db.SaveChangesAsync(ct);
        return ToDto(cart);
    }

    public async Task ClearCart(Guid customerId, CancellationToken ct)
    {
        var cart = await db.Carts.Include(x => x.Items).SingleOrDefaultAsync(x => x.CustomerId == customerId, ct);
        if (cart is not null)
        {
            cart.Clear();
            await db.SaveChangesAsync(ct);
        }
    }

    private static CartDto ToDto(Cart cart) => new(
        cart.Id,
        cart.CustomerId,
        cart.Items.Select(i => new CartItemDto(i.Id, i.MenuItemId, i.Name, i.UnitPriceMinor, i.Quantity, i.Instructions, i.LineTotalMinor)).ToList(),
        cart.SubtotalMinor,
        cart.UpdatedAt
    );
}

// ─────────────────────────────────────────────────────────────────────────────
// Order Service
// ─────────────────────────────────────────────────────────────────────────────

public sealed class OrderService(BusinessDbContext db) : IOrderService
{
    private static readonly long DeliveryFeeMinor = 1500; // 15 GHS flat delivery fee

    public async Task<OrderDto> Checkout(Guid customerId, CheckoutRequest request, CancellationToken ct)
    {
        var cart = await db.Carts.Include(x => x.Items).SingleOrDefaultAsync(x => x.CustomerId == customerId, ct)
            ?? throw new InvalidOperationException("Cart not found.");
        if (cart.Items.Count == 0)
            throw new InvalidOperationException("Cart is empty.");

        var fulfillment = Enum.Parse<FulfillmentType>(request.Fulfillment, ignoreCase: true);
        var orderNumber = GenerateOrderNumber();

        var order = new Order(customerId, orderNumber, fulfillment);
        foreach (var item in cart.Items)
        {
            order.AddItem(item.MenuItemId, item.Name, item.UnitPriceMinor, item.Quantity, item.Instructions);
        }

        if (fulfillment == FulfillmentType.Delivery)
        {
            if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
                throw new InvalidOperationException("Delivery address is required.");
            order.SetDeliveryDetails(request.DeliveryAddress!, request.DeliveryLandmark, DeliveryFeeMinor);
        }

        if (request.Notes is not null)
            order.SetNotes(request.Notes);

        order.TransitionTo(OrderStatus.AwaitingConfirmation);

        db.Orders.Add(order);

        // Record status history
        var history = new OrderStatusHistory(order.Id, OrderStatus.Draft, OrderStatus.AwaitingConfirmation, null, "System");
        db.OrderStatusHistories.Add(history);

        // Clear the cart
        cart.Clear();

        await db.SaveChangesAsync(ct);

        return ToDto(order);
    }

    public async Task<OrderDto?> GetById(Guid id, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        return order is null ? null : ToDto(order);
    }

    public async Task<OrderDto?> GetByOrderNumber(string orderNumber, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.OrderNumber == orderNumber, ct);
        return order is null ? null : ToDto(order);
    }

    public async Task<IReadOnlyList<OrderSummaryDto>> GetByCustomer(Guid customerId, CancellationToken ct)
    {
        return await db.Orders.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new OrderSummaryDto(x.Id, x.OrderNumber, x.Status.ToString(), x.TotalMinor, x.CreatedAt, x.Items.Count))
            .ToListAsync(ct);
    }

    public async Task<Page<OrderSummaryDto>> List(int page, int pageSize, string? status, CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var statusEnum))
        {
            query = query.Where(x => x.Status == statusEnum);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new OrderSummaryDto(x.Id, x.OrderNumber, x.Status.ToString(), x.TotalMinor, x.CreatedAt, x.Items.Count))
            .ToListAsync(ct);

        return new Page<OrderSummaryDto>(items, page, pageSize, total);
    }

    public async Task<OrderDto> UpdateStatus(Guid id, UpdateOrderStatusRequest request, string? changedBy, CancellationToken ct)
    {
        var order = await db.Orders.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InvalidOperationException("Order not found.");

        var newStatus = Enum.Parse<OrderStatus>(request.Status, ignoreCase: true);
        var oldStatus = order.Status;

        order.TransitionTo(newStatus, request.Reason);

        var history = new OrderStatusHistory(order.Id, oldStatus, newStatus, request.Reason, changedBy);
        db.OrderStatusHistories.Add(history);

        await db.SaveChangesAsync(ct);
        return ToDto(order);
    }

    public async Task<OrderDto> Cancel(Guid id, string? reason, string? changedBy, CancellationToken ct)
    {
        return await UpdateStatus(id, new UpdateOrderStatusRequest("Cancelled", reason), changedBy, ct);
    }

    public string GenerateOrderNumber()
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = Random.Shared.Next(1000, 9999);
        return $"RST-{date}-{random}";
    }

    private static OrderDto ToDto(Order order) => new(
        order.Id,
        order.CustomerId,
        order.OrderNumber,
        order.Status.ToString(),
        order.Fulfillment.ToString(),
        order.DeliveryAddress,
        order.DeliveryLandmark,
        order.SubtotalMinor,
        order.DeliveryFeeMinor,
        order.DiscountMinor,
        order.TotalMinor,
        order.Items.Select(i => new Application.OrderItemDto(i.Id, i.MenuItemId, i.Name, i.UnitPriceMinor, i.Quantity, i.Instructions, i.LineTotalMinor)).ToList(),
        order.CreatedAt,
        order.PaidAt,
        order.CompletedAt,
        order.Notes
    );
}

// ─────────────────────────────────────────────────────────────────────────────
// Cloudinary Service
// ─────────────────────────────────────────────────────────────────────────────

public sealed class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration config)
    {
        var cloudName = config["Cloudinary:CloudName"] ?? throw new InvalidOperationException("Cloudinary:CloudName is required");
        var apiKey = config["Cloudinary:ApiKey"] ?? throw new InvalidOperationException("Cloudinary:ApiKey is required");
        var apiSecret = config["Cloudinary:ApiSecret"] ?? throw new InvalidOperationException("Cloudinary:ApiSecret is required");

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<CloudinaryUploadResult> UploadImageAsync(string base64Image, string folder, string? fileName = null, CancellationToken ct = default)
    {
        // Remove data URI prefix if present (e.g., "data:image/png;base64,")
        var base64Data = base64Image;
        if (base64Image.Contains(','))
        {
            base64Data = base64Image.Split(',')[1];
        }

        var bytes = Convert.FromBase64String(base64Data);
        using var stream = new MemoryStream(bytes);

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName ?? $"payment-{Guid.NewGuid()}", stream),
            Folder = folder,
            Transformation = new Transformation().Quality("auto").FetchFormat("auto")
        };

        var result = await _cloudinary.UploadAsync(uploadParams, ct);

        if (result.Error != null)
        {
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");
        }

        return new CloudinaryUploadResult(result.PublicId, result.AssetId, result.SecureUrl.ToString());
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Payment Service
// ─────────────────────────────────────────────────────────────────────────────

public sealed class PaymentService(BusinessDbContext db, IConfiguration config, ICloudinaryService cloudinary) : IPaymentService
{
    public async Task<PaymentInitializedDto> Initialize(InitializePaymentRequest request, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.OrderId, ct)
            ?? throw new InvalidOperationException("Order not found.");

        if (order.Status != OrderStatus.AwaitingConfirmation && order.Status != OrderStatus.AwaitingPayment)
            throw new InvalidOperationException($"Order cannot accept payment in status {order.Status}.");

        var method = Enum.Parse<PaymentMethod>(request.Method, ignoreCase: true);
        var reference = $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(10000, 99999)}";

        var payment = new PaymentAttempt(request.OrderId, reference, method, order.TotalMinor, TimeSpan.FromHours(24));

        // For Paystack, we would integrate with Paystack API here and set the checkout URL
        // For now, we'll just create the payment attempt in test mode
        if (method == PaymentMethod.Paystack)
        {
            // In test mode, generate a mock checkout URL
            payment.SetCheckoutUrl($"https://checkout.paystack.com/test/{reference}", null);
        }

        db.PaymentAttempts.Add(payment);

        // Update order status to awaiting payment
        var orderEntity = await db.Orders.SingleAsync(x => x.Id == request.OrderId, ct);
        if (orderEntity.Status == OrderStatus.AwaitingConfirmation)
        {
            orderEntity.TransitionTo(OrderStatus.AwaitingPayment);
            db.OrderStatusHistories.Add(new OrderStatusHistory(order.Id, OrderStatus.AwaitingConfirmation, OrderStatus.AwaitingPayment, null, "System"));
        }

        await db.SaveChangesAsync(ct);

        return new PaymentInitializedDto(payment.Id, payment.Reference, payment.CheckoutUrl);
    }

    public async Task<PaymentAttemptDto?> GetByReference(string reference, CancellationToken ct)
    {
        return await db.PaymentAttempts.AsNoTracking()
            .Where(x => x.Reference == reference)
            .Select(x => ToDto(x))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<PaymentAttemptDto?> GetById(Guid id, CancellationToken ct)
    {
        return await db.PaymentAttempts.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => ToDto(x))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<PaymentAttemptDto>> GetByOrder(Guid orderId, CancellationToken ct)
    {
        return await db.PaymentAttempts.AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => ToDto(x))
            .ToListAsync(ct);
    }

    public async Task MarkVerified(string reference, string externalReference, CancellationToken ct)
    {
        var payment = await db.PaymentAttempts.SingleOrDefaultAsync(x => x.Reference == reference, ct)
            ?? throw new InvalidOperationException("Payment not found.");

        payment.MarkVerified(externalReference);

        // Update order status
        var order = await db.Orders.SingleAsync(x => x.Id == payment.OrderId, ct);
        var oldStatus = order.Status;
        order.TransitionTo(OrderStatus.Paid);
        db.OrderStatusHistories.Add(new OrderStatusHistory(order.Id, oldStatus, OrderStatus.Paid, "Payment verified", "System"));

        await db.SaveChangesAsync(ct);
    }

    public async Task MarkFailed(string reference, string reason, CancellationToken ct)
    {
        var payment = await db.PaymentAttempts.SingleOrDefaultAsync(x => x.Reference == reference, ct)
            ?? throw new InvalidOperationException("Payment not found.");

        payment.MarkFailed(reason);
        await db.SaveChangesAsync(ct);
    }

    private static PaymentAttemptDto ToDto(PaymentAttempt x) => new(
        x.Id, x.OrderId, x.Reference, x.Method.ToString(), x.Status.ToString(),
        x.AmountMinor, x.Currency, x.CheckoutUrl, x.CreatedAt, x.ExpiresAt, x.VerifiedAt
    );

    public PaymentDetailsDto GetPaymentDetails()
    {
        return new PaymentDetailsDto(
            config["Payments:BankName"] ?? "",
            config["Payments:BankAccountNumber"] ?? "",
            config["Payments:BankAccountName"] ?? "",
            config["Payments:MoMoNumber"] ?? "",
            config["Payments:MoMoName"] ?? ""
        );
    }

    public async Task<PaymentEvidenceDto> UploadEvidence(UploadEvidenceRequest request, CancellationToken ct)
    {
        var payment = await db.PaymentAttempts.SingleOrDefaultAsync(x => x.Id == request.PaymentAttemptId, ct)
            ?? throw new InvalidOperationException("Payment attempt not found.");

        if (payment.Status == PaymentStatus.Verified)
            throw new InvalidOperationException("Payment is already verified.");

        // Upload to Cloudinary
        var uploadResult = await cloudinary.UploadImageAsync(
            request.Base64Image,
            "payment-evidence",
            request.FileName,
            ct
        );

        // Compute hash for duplicate detection
        var base64Data = request.Base64Image.Contains(',')
            ? request.Base64Image.Split(',')[1]
            : request.Base64Image;
        var bytes = Convert.FromBase64String(base64Data);
        var hash = Convert.ToHexString(SHA256.HashData(bytes));

        // Create evidence record
        var evidence = new PaymentEvidence(
            request.PaymentAttemptId,
            uploadResult.PublicId,
            uploadResult.AssetId,
            uploadResult.SecureUrl,
            hash
        );
        db.PaymentEvidences.Add(evidence);

        // Create pending review record
        var review = new PaymentReview(payment.Id, evidence.Id);
        db.PaymentReviews.Add(review);

        // Update order status to PaymentUnderReview
        var order = await db.Orders.SingleAsync(x => x.Id == payment.OrderId, ct);
        if (order.Status == OrderStatus.AwaitingPayment)
        {
            var oldStatus = order.Status;
            order.TransitionTo(OrderStatus.PaymentUnderReview);
            db.OrderStatusHistories.Add(new OrderStatusHistory(order.Id, oldStatus, OrderStatus.PaymentUnderReview, "Evidence uploaded", "System"));
        }

        await db.SaveChangesAsync(ct);

        return new PaymentEvidenceDto(evidence.Id, evidence.PaymentAttemptId, evidence.CloudinaryUrl, evidence.UploadedAt);
    }

    public async Task<IReadOnlyList<PaymentReviewDto>> GetPendingReviews(CancellationToken ct)
    {
        return await db.PaymentReviews.AsNoTracking()
            .Where(x => x.Approved == null)
            .Join(db.PaymentAttempts.AsNoTracking(), r => r.PaymentAttemptId, p => p.Id, (r, p) => new { Review = r, Payment = p })
            .Join(db.Orders.AsNoTracking(), rp => rp.Payment.OrderId, o => o.Id, (rp, o) => new { rp.Review, rp.Payment, Order = o })
            .GroupJoin(db.PaymentEvidences.AsNoTracking(), rpo => rpo.Review.EvidenceId, e => e.Id, (rpo, evidences) => new { rpo.Review, rpo.Payment, rpo.Order, Evidence = evidences.FirstOrDefault() })
            .OrderByDescending(x => x.Review.CreatedAt)
            .Select(x => new PaymentReviewDto(
                x.Review.Id,
                x.Payment.Id,
                x.Payment.Reference,
                x.Order.Id,
                x.Order.OrderNumber,
                x.Payment.AmountMinor,
                x.Payment.Currency,
                x.Payment.Method.ToString(),
                x.Payment.Status.ToString(),
                x.Evidence != null ? x.Evidence.Id : null,
                x.Evidence != null ? x.Evidence.CloudinaryUrl : null,
                x.Review.Approved,
                x.Review.ReviewedBy,
                x.Review.Notes,
                x.Review.CreatedAt,
                x.Review.ReviewedAt
            ))
            .ToListAsync(ct);
    }

    public async Task<PaymentReviewDto?> GetReviewById(Guid reviewId, CancellationToken ct)
    {
        return await db.PaymentReviews.AsNoTracking()
            .Where(x => x.Id == reviewId)
            .Join(db.PaymentAttempts.AsNoTracking(), r => r.PaymentAttemptId, p => p.Id, (r, p) => new { Review = r, Payment = p })
            .Join(db.Orders.AsNoTracking(), rp => rp.Payment.OrderId, o => o.Id, (rp, o) => new { rp.Review, rp.Payment, Order = o })
            .GroupJoin(db.PaymentEvidences.AsNoTracking(), rpo => rpo.Review.EvidenceId, e => e.Id, (rpo, evidences) => new { rpo.Review, rpo.Payment, rpo.Order, Evidence = evidences.FirstOrDefault() })
            .Select(x => new PaymentReviewDto(
                x.Review.Id,
                x.Payment.Id,
                x.Payment.Reference,
                x.Order.Id,
                x.Order.OrderNumber,
                x.Payment.AmountMinor,
                x.Payment.Currency,
                x.Payment.Method.ToString(),
                x.Payment.Status.ToString(),
                x.Evidence != null ? x.Evidence.Id : null,
                x.Evidence != null ? x.Evidence.CloudinaryUrl : null,
                x.Review.Approved,
                x.Review.ReviewedBy,
                x.Review.Notes,
                x.Review.CreatedAt,
                x.Review.ReviewedAt
            ))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<PaymentReviewDto> ReviewPayment(Guid reviewId, ReviewPaymentRequest request, string reviewedBy, CancellationToken ct)
    {
        var review = await db.PaymentReviews.SingleOrDefaultAsync(x => x.Id == reviewId, ct)
            ?? throw new InvalidOperationException("Review not found.");

        if (review.Approved != null)
            throw new InvalidOperationException("Review has already been processed.");

        var payment = await db.PaymentAttempts.SingleAsync(x => x.Id == review.PaymentAttemptId, ct);
        var order = await db.Orders.SingleAsync(x => x.Id == payment.OrderId, ct);

        if (request.Approved)
        {
            review.Approve(reviewedBy, request.Notes);
            payment.MarkVerified($"MANUAL-{reviewId}");

            var oldStatus = order.Status;
            order.TransitionTo(OrderStatus.Paid);
            db.OrderStatusHistories.Add(new OrderStatusHistory(order.Id, oldStatus, OrderStatus.Paid, $"Payment approved by {reviewedBy}", reviewedBy));
        }
        else
        {
            review.Reject(reviewedBy, request.Notes);
            payment.MarkFailed(request.Notes ?? "Payment rejected by admin");

            var oldStatus = order.Status;
            order.TransitionTo(OrderStatus.AwaitingPayment);
            db.OrderStatusHistories.Add(new OrderStatusHistory(order.Id, oldStatus, OrderStatus.AwaitingPayment, $"Payment rejected: {request.Notes}", reviewedBy));
        }

        await db.SaveChangesAsync(ct);

        return (await GetReviewById(reviewId, ct))!;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Conversation Service
// ─────────────────────────────────────────────────────────────────────────────

public sealed class ConversationService(BusinessDbContext db) : IConversationService
{
    public async Task<ConversationSessionDto> GetOrCreateSession(Guid customerId, CancellationToken ct)
    {
        // Try to find an active or idle session
        var session = await db.ConversationSessions
            .Where(x => x.CustomerId == customerId && (x.State == ConversationState.Active || x.State == ConversationState.Idle))
            .OrderByDescending(x => x.LastMessageAt)
            .FirstOrDefaultAsync(ct);

        if (session is not null)
        {
            if (session.State == ConversationState.Idle)
            {
                session.Resume();
                await db.SaveChangesAsync(ct);
            }
            return ToDto(session);
        }

        // Create new session
        session = new ConversationSession(customerId);
        db.ConversationSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return ToDto(session);
    }

    public async Task<ConversationSessionDto?> GetActiveSession(Guid customerId, CancellationToken ct)
    {
        var session = await db.ConversationSessions.AsNoTracking()
            .Where(x => x.CustomerId == customerId && x.State == ConversationState.Active)
            .OrderByDescending(x => x.LastMessageAt)
            .FirstOrDefaultAsync(ct);
        return session is null ? null : ToDto(session);
    }

    public async Task<ConversationSessionDto> UpdateIntent(Guid sessionId, string? intent, CancellationToken ct)
    {
        var session = await db.ConversationSessions.SingleOrDefaultAsync(x => x.Id == sessionId, ct)
            ?? throw new InvalidOperationException("Session not found.");
        session.UpdateIntent(intent);
        await db.SaveChangesAsync(ct);
        return ToDto(session);
    }

    public async Task<ConversationSessionDto> LinkCart(Guid sessionId, Guid cartId, CancellationToken ct)
    {
        var session = await db.ConversationSessions.SingleOrDefaultAsync(x => x.Id == sessionId, ct)
            ?? throw new InvalidOperationException("Session not found.");
        session.LinkCart(cartId);
        await db.SaveChangesAsync(ct);
        return ToDto(session);
    }

    public async Task<ConversationSessionDto> LinkOrder(Guid sessionId, Guid orderId, CancellationToken ct)
    {
        var session = await db.ConversationSessions.SingleOrDefaultAsync(x => x.Id == sessionId, ct)
            ?? throw new InvalidOperationException("Session not found.");
        session.LinkOrder(orderId);
        await db.SaveChangesAsync(ct);
        return ToDto(session);
    }

    public async Task<ConversationSessionDto> HandOff(Guid sessionId, string reason, CancellationToken ct)
    {
        var session = await db.ConversationSessions.SingleOrDefaultAsync(x => x.Id == sessionId, ct)
            ?? throw new InvalidOperationException("Session not found.");
        session.HandOff(reason);
        await db.SaveChangesAsync(ct);
        return ToDto(session);
    }

    public async Task<ConversationMessageDto> AddMessage(Guid sessionId, string direction, string content, string? intent, string? externalId, CancellationToken ct)
    {
        var dir = Enum.Parse<MessageDirection>(direction, ignoreCase: true);
        var message = new ConversationMessage(sessionId, dir, content, intent, externalId);
        db.ConversationMessages.Add(message);

        // Update session last message time
        var session = await db.ConversationSessions.SingleOrDefaultAsync(x => x.Id == sessionId, ct);
        if (session is not null)
        {
            session.UpdateIntent(intent);
        }

        await db.SaveChangesAsync(ct);
        return new ConversationMessageDto(message.Id, message.Direction.ToString(), message.Content, message.Intent, message.Timestamp);
    }

    public async Task<IReadOnlyList<ConversationMessageDto>> GetMessages(Guid sessionId, int limit, CancellationToken ct)
    {
        return await db.ConversationMessages.AsNoTracking()
            .Where(x => x.SessionId == sessionId)
            .OrderByDescending(x => x.Timestamp)
            .Take(limit)
            .Select(x => new ConversationMessageDto(x.Id, x.Direction.ToString(), x.Content, x.Intent, x.Timestamp))
            .ToListAsync(ct);
    }

    private static ConversationSessionDto ToDto(ConversationSession s) => new(
        s.Id, s.CustomerId, s.State.ToString(), s.CurrentIntent, s.ActiveCartId, s.ActiveOrderId, s.StartedAt, s.LastMessageAt
    );
}
