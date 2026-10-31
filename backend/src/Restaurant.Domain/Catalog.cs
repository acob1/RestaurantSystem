namespace Restaurant.Domain;

public sealed class RestaurantProfile
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = "Accra Kitchen";
    public string Currency { get; private set; } = "GHS";
    public string TimeZone { get; private set; } = "Africa/Accra";
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
}

public sealed class MenuCategory
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = "";
    private MenuCategory() { }
    public MenuCategory(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
            throw new ArgumentException("Category name must contain 1–100 characters.", nameof(name));
        Name = name.Trim();
    }
}

public sealed class MenuItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CategoryId { get; private set; }
    public string Name { get; private set; } = "";
    public string Description { get; private set; } = "";
    public long PriceMinor { get; private set; }
    public bool Available { get; private set; } = true;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    private MenuItem() { }
    public MenuItem(Guid categoryId, string name, string description, long priceMinor)
    {
        if (categoryId == Guid.Empty) throw new ArgumentException("Category is required.");
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 160) throw new ArgumentException("Item name must contain 1–160 characters.");
        if (description.Length > 2000) throw new ArgumentException("Description is too long.");
        if (priceMinor <= 0 || priceMinor > 100_000_000) throw new ArgumentOutOfRangeException(nameof(priceMinor));
        CategoryId = categoryId; Name = name.Trim(); Description = description; PriceMinor = priceMinor;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Customers
// ─────────────────────────────────────────────────────────────────────────────

public sealed class Customer
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string WhatsAppId { get; private set; } = "";
    public string? Name { get; private set; }
    public string? Phone { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastActiveAt { get; private set; }
    private Customer() { }
    public Customer(string whatsAppId)
    {
        if (string.IsNullOrWhiteSpace(whatsAppId) || whatsAppId.Length > 32)
            throw new ArgumentException("WhatsApp ID must contain 1–32 characters.", nameof(whatsAppId));
        WhatsAppId = whatsAppId;
    }
    public void UpdateProfile(string? name, string? phone)
    {
        if (name is not null && name.Trim().Length > 100) throw new ArgumentException("Name is too long.");
        if (phone is not null && phone.Length > 20) throw new ArgumentException("Phone is too long.");
        Name = name?.Trim();
        Phone = phone;
    }
    public void RecordActivity() => LastActiveAt = DateTimeOffset.UtcNow;
}

public sealed class CustomerAddress
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CustomerId { get; private set; }
    public string Label { get; private set; } = "";
    public string Address { get; private set; } = "";
    public string? Landmark { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public bool IsDefault { get; private set; }
    private CustomerAddress() { }
    public CustomerAddress(Guid customerId, string label, string address, string? landmark = null)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("Customer is required.");
        if (string.IsNullOrWhiteSpace(label) || label.Length > 50) throw new ArgumentException("Label must be 1–50 characters.");
        if (string.IsNullOrWhiteSpace(address) || address.Length > 500) throw new ArgumentException("Address must be 1–500 characters.");
        if (landmark?.Length > 200) throw new ArgumentException("Landmark is too long.");
        CustomerId = customerId; Label = label.Trim(); Address = address.Trim(); Landmark = landmark?.Trim();
    }
    public void SetLocation(double lat, double lng) { Latitude = lat; Longitude = lng; }
    public void SetDefault(bool isDefault) => IsDefault = isDefault;
}

// ─────────────────────────────────────────────────────────────────────────────
// Cart (per-customer shopping cart)
// ─────────────────────────────────────────────────────────────────────────────

public sealed class Cart
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CustomerId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    private readonly List<CartItem> _items = [];
    public IReadOnlyList<CartItem> Items => _items;
    private Cart() { }
    public Cart(Guid customerId)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("Customer is required.");
        CustomerId = customerId;
    }
    public CartItem AddItem(Guid menuItemId, string name, long unitPriceMinor, int quantity, string? instructions = null)
    {
        if (quantity < 1 || quantity > 50) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be 1–50.");
        var existing = _items.FirstOrDefault(x => x.MenuItemId == menuItemId && x.Instructions == instructions);
        if (existing is not null)
        {
            existing.SetQuantity(existing.Quantity + quantity);
            UpdatedAt = DateTimeOffset.UtcNow;
            return existing;
        }
        var item = new CartItem(Id, menuItemId, name, unitPriceMinor, quantity, instructions);
        _items.Add(item);
        UpdatedAt = DateTimeOffset.UtcNow;
        return item;
    }
    public void UpdateItemQuantity(Guid itemId, int quantity)
    {
        var item = _items.FirstOrDefault(x => x.Id == itemId) ?? throw new InvalidOperationException("Item not found.");
        if (quantity < 1)
        {
            _items.Remove(item);
        }
        else
        {
            item.SetQuantity(quantity);
        }
        UpdatedAt = DateTimeOffset.UtcNow;
    }
    public void RemoveItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(x => x.Id == itemId);
        if (item is not null)
        {
            _items.Remove(item);
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
    public void Clear()
    {
        _items.Clear();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
    public long SubtotalMinor => _items.Sum(x => x.LineTotalMinor);
}

public sealed class CartItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CartId { get; private set; }
    public Guid MenuItemId { get; private set; }
    public string Name { get; private set; } = "";
    public long UnitPriceMinor { get; private set; }
    public int Quantity { get; private set; }
    public string? Instructions { get; private set; }
    public long LineTotalMinor => UnitPriceMinor * Quantity;
    private CartItem() { }
    public CartItem(Guid cartId, Guid menuItemId, string name, long unitPriceMinor, int quantity, string? instructions)
    {
        if (cartId == Guid.Empty) throw new ArgumentException("Cart is required.");
        if (menuItemId == Guid.Empty) throw new ArgumentException("Menu item is required.");
        if (string.IsNullOrWhiteSpace(name) || name.Length > 160) throw new ArgumentException("Name must be 1–160 characters.");
        if (unitPriceMinor <= 0) throw new ArgumentOutOfRangeException(nameof(unitPriceMinor));
        if (quantity < 1 || quantity > 50) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (instructions?.Length > 500) throw new ArgumentException("Instructions too long.");
        CartId = cartId; MenuItemId = menuItemId; Name = name; UnitPriceMinor = unitPriceMinor; Quantity = quantity;
        Instructions = instructions?.Trim();
    }
    public void SetQuantity(int quantity)
    {
        if (quantity < 1 || quantity > 50) throw new ArgumentOutOfRangeException(nameof(quantity));
        Quantity = quantity;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Orders
// ─────────────────────────────────────────────────────────────────────────────

public enum OrderStatus
{
    Draft,
    AwaitingConfirmation,
    AwaitingPayment,
    PaymentUnderReview,
    Paid,
    Accepted,
    Preparing,
    Ready,
    OutForDelivery,
    Completed,
    Cancelled,
    Expired,
    Refunded
}

public enum FulfillmentType { Pickup, Delivery }

public sealed class Order
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CustomerId { get; private set; }
    public string OrderNumber { get; private set; } = "";
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public FulfillmentType Fulfillment { get; private set; } = FulfillmentType.Pickup;
    public string? DeliveryAddress { get; private set; }
    public string? DeliveryLandmark { get; private set; }
    public long SubtotalMinor { get; private set; }
    public long DeliveryFeeMinor { get; private set; }
    public long DiscountMinor { get; private set; }
    public long TotalMinor { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? Notes { get; private set; }
    private readonly List<OrderItem> _items = [];
    public IReadOnlyList<OrderItem> Items => _items;

    private Order() { }

    public Order(Guid customerId, string orderNumber, FulfillmentType fulfillment)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("Customer is required.");
        if (string.IsNullOrWhiteSpace(orderNumber) || orderNumber.Length > 30) throw new ArgumentException("Order number invalid.");
        CustomerId = customerId;
        OrderNumber = orderNumber;
        Fulfillment = fulfillment;
    }

    public OrderItem AddItem(Guid menuItemId, string name, long unitPriceMinor, int quantity, string? instructions = null)
    {
        if (Status != OrderStatus.Draft) throw new InvalidOperationException("Cannot modify finalized order.");
        var item = new OrderItem(Id, menuItemId, name, unitPriceMinor, quantity, instructions);
        _items.Add(item);
        RecalculateTotals();
        return item;
    }

    public void SetDeliveryDetails(string address, string? landmark, long deliveryFeeMinor)
    {
        if (Fulfillment != FulfillmentType.Delivery) throw new InvalidOperationException("Not a delivery order.");
        if (string.IsNullOrWhiteSpace(address) || address.Length > 500) throw new ArgumentException("Address invalid.");
        DeliveryAddress = address.Trim();
        DeliveryLandmark = landmark?.Trim();
        DeliveryFeeMinor = deliveryFeeMinor;
        RecalculateTotals();
    }

    public void ApplyDiscount(long discountMinor)
    {
        if (discountMinor < 0 || discountMinor > SubtotalMinor) throw new ArgumentOutOfRangeException(nameof(discountMinor));
        DiscountMinor = discountMinor;
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        SubtotalMinor = _items.Sum(x => x.LineTotalMinor);
        TotalMinor = SubtotalMinor + DeliveryFeeMinor - DiscountMinor;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetNotes(string? notes)
    {
        if (notes?.Length > 1000) throw new ArgumentException("Notes too long.");
        Notes = notes?.Trim();
    }

    private static readonly Dictionary<OrderStatus, OrderStatus[]> ValidTransitions = new()
    {
        [OrderStatus.Draft] = [OrderStatus.AwaitingConfirmation, OrderStatus.Cancelled],
        [OrderStatus.AwaitingConfirmation] = [OrderStatus.AwaitingPayment, OrderStatus.Cancelled],
        [OrderStatus.AwaitingPayment] = [OrderStatus.PaymentUnderReview, OrderStatus.Paid, OrderStatus.Cancelled, OrderStatus.Expired],
        [OrderStatus.PaymentUnderReview] = [OrderStatus.Paid, OrderStatus.AwaitingPayment, OrderStatus.Cancelled],
        [OrderStatus.Paid] = [OrderStatus.Accepted, OrderStatus.Cancelled, OrderStatus.Refunded],
        [OrderStatus.Accepted] = [OrderStatus.Preparing, OrderStatus.Cancelled, OrderStatus.Refunded],
        [OrderStatus.Preparing] = [OrderStatus.Ready, OrderStatus.Cancelled, OrderStatus.Refunded],
        [OrderStatus.Ready] = [OrderStatus.OutForDelivery, OrderStatus.Completed, OrderStatus.Cancelled, OrderStatus.Refunded],
        [OrderStatus.OutForDelivery] = [OrderStatus.Completed, OrderStatus.Cancelled, OrderStatus.Refunded],
        [OrderStatus.Completed] = [OrderStatus.Refunded],
        [OrderStatus.Cancelled] = [],
        [OrderStatus.Expired] = [],
        [OrderStatus.Refunded] = []
    };

    public void TransitionTo(OrderStatus newStatus, string? reason = null)
    {
        if (!ValidTransitions[Status].Contains(newStatus))
            throw new InvalidOperationException($"Cannot transition from {Status} to {newStatus}.");
        Status = newStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
        if (newStatus == OrderStatus.Paid) PaidAt = DateTimeOffset.UtcNow;
        if (newStatus is OrderStatus.Completed or OrderStatus.Cancelled or OrderStatus.Refunded) CompletedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class OrderItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public Guid MenuItemId { get; private set; }
    public string Name { get; private set; } = "";
    public long UnitPriceMinor { get; private set; }
    public int Quantity { get; private set; }
    public string? Instructions { get; private set; }
    public long LineTotalMinor => UnitPriceMinor * Quantity;
    private OrderItem() { }
    public OrderItem(Guid orderId, Guid menuItemId, string name, long unitPriceMinor, int quantity, string? instructions)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("Order is required.");
        if (menuItemId == Guid.Empty) throw new ArgumentException("Menu item is required.");
        if (string.IsNullOrWhiteSpace(name) || name.Length > 160) throw new ArgumentException("Name invalid.");
        if (unitPriceMinor <= 0) throw new ArgumentOutOfRangeException(nameof(unitPriceMinor));
        if (quantity < 1 || quantity > 50) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (instructions?.Length > 500) throw new ArgumentException("Instructions too long.");
        OrderId = orderId; MenuItemId = menuItemId; Name = name; UnitPriceMinor = unitPriceMinor; Quantity = quantity;
        Instructions = instructions?.Trim();
    }
}

public sealed class OrderStatusHistory
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public OrderStatus FromStatus { get; private set; }
    public OrderStatus ToStatus { get; private set; }
    public string? Reason { get; private set; }
    public string? ChangedBy { get; private set; }
    public DateTimeOffset ChangedAt { get; private set; } = DateTimeOffset.UtcNow;
    private OrderStatusHistory() { }
    public OrderStatusHistory(Guid orderId, OrderStatus from, OrderStatus to, string? reason = null, string? changedBy = null)
    {
        OrderId = orderId; FromStatus = from; ToStatus = to; Reason = reason; ChangedBy = changedBy;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Payments
// ─────────────────────────────────────────────────────────────────────────────

public enum PaymentMethod { Paystack, MoMo, BankTransfer }
public enum PaymentStatus { Pending, Processing, Verified, Failed, Cancelled, Refunded }

public sealed class PaymentAttempt
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public string Reference { get; private set; } = "";
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public long AmountMinor { get; private set; }
    public string Currency { get; private set; } = "GHS";
    public string? ExternalReference { get; private set; }
    public string? CheckoutUrl { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }
    public string? FailureReason { get; private set; }
    private PaymentAttempt() { }
    public PaymentAttempt(Guid orderId, string reference, PaymentMethod method, long amountMinor, TimeSpan? expiry = null)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("Order is required.");
        if (string.IsNullOrWhiteSpace(reference) || reference.Length > 100) throw new ArgumentException("Reference invalid.");
        if (amountMinor <= 0) throw new ArgumentOutOfRangeException(nameof(amountMinor));
        OrderId = orderId; Reference = reference; Method = method; AmountMinor = amountMinor;
        if (expiry.HasValue) ExpiresAt = DateTimeOffset.UtcNow.Add(expiry.Value);
    }
    public void SetCheckoutUrl(string url, string? externalRef = null)
    {
        CheckoutUrl = url; ExternalReference = externalRef; Status = PaymentStatus.Processing; UpdatedAt = DateTimeOffset.UtcNow;
    }
    public void MarkVerified(string externalRef)
    {
        ExternalReference = externalRef; Status = PaymentStatus.Verified; VerifiedAt = DateTimeOffset.UtcNow; UpdatedAt = DateTimeOffset.UtcNow;
    }
    public void MarkFailed(string reason)
    {
        FailureReason = reason; Status = PaymentStatus.Failed; UpdatedAt = DateTimeOffset.UtcNow;
    }
    public void MarkCancelled() { Status = PaymentStatus.Cancelled; UpdatedAt = DateTimeOffset.UtcNow; }
}

public sealed class PaymentEvidence
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid PaymentAttemptId { get; private set; }
    public string CloudinaryPublicId { get; private set; } = "";
    public string CloudinaryAssetId { get; private set; } = "";
    public string CloudinaryUrl { get; private set; } = "";
    public string FileHash { get; private set; } = "";
    public string? OcrText { get; private set; }
    public string? OcrTransactionRef { get; private set; }
    public long? OcrAmountMinor { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; } = DateTimeOffset.UtcNow;
    private PaymentEvidence() { }
    public PaymentEvidence(Guid paymentAttemptId, string publicId, string assetId, string url, string hash)
    {
        PaymentAttemptId = paymentAttemptId;
        CloudinaryPublicId = publicId;
        CloudinaryAssetId = assetId;
        CloudinaryUrl = url;
        FileHash = hash;
    }
    public void SetOcrResults(string? text, string? transactionRef, long? amount)
    {
        OcrText = text; OcrTransactionRef = transactionRef; OcrAmountMinor = amount;
    }
}

public sealed class PaymentReview
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid PaymentAttemptId { get; private set; }
    public Guid? EvidenceId { get; private set; }
    public string? ReviewedBy { get; private set; }
    public bool? Approved { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; private set; }
    private PaymentReview() { }
    public PaymentReview(Guid paymentAttemptId, Guid? evidenceId = null)
    {
        PaymentAttemptId = paymentAttemptId; EvidenceId = evidenceId;
    }
    public void Approve(string reviewedBy, string? notes = null)
    {
        ReviewedBy = reviewedBy; Approved = true; Notes = notes; ReviewedAt = DateTimeOffset.UtcNow;
    }
    public void Reject(string reviewedBy, string? notes = null)
    {
        ReviewedBy = reviewedBy; Approved = false; Notes = notes; ReviewedAt = DateTimeOffset.UtcNow;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Conversations (AI session tracking)
// ─────────────────────────────────────────────────────────────────────────────

public enum ConversationState { Active, Idle, HandedOff, Closed }

public sealed class ConversationSession
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CustomerId { get; private set; }
    public ConversationState State { get; private set; } = ConversationState.Active;
    public string? CurrentIntent { get; private set; }
    public Guid? ActiveCartId { get; private set; }
    public Guid? ActiveOrderId { get; private set; }
    public DateTimeOffset StartedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastMessageAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAt { get; private set; }
    public string? HandoffReason { get; private set; }
    private ConversationSession() { }
    public ConversationSession(Guid customerId)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("Customer is required.");
        CustomerId = customerId;
    }
    public void UpdateIntent(string? intent) { CurrentIntent = intent; LastMessageAt = DateTimeOffset.UtcNow; }
    public void LinkCart(Guid cartId) { ActiveCartId = cartId; }
    public void LinkOrder(Guid orderId) { ActiveOrderId = orderId; ActiveCartId = null; }
    public void HandOff(string reason)
    {
        State = ConversationState.HandedOff; HandoffReason = reason; LastMessageAt = DateTimeOffset.UtcNow;
    }
    public void Resume() { State = ConversationState.Active; LastMessageAt = DateTimeOffset.UtcNow; }
    public void Close() { State = ConversationState.Closed; EndedAt = DateTimeOffset.UtcNow; }
    public void MarkIdle() { State = ConversationState.Idle; }
}

public enum MessageDirection { Inbound, Outbound }

public sealed class ConversationMessage
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid SessionId { get; private set; }
    public MessageDirection Direction { get; private set; }
    public string Content { get; private set; } = "";
    public string? Intent { get; private set; }
    public string? ExternalMessageId { get; private set; }
    public DateTimeOffset Timestamp { get; private set; } = DateTimeOffset.UtcNow;
    private ConversationMessage() { }
    public ConversationMessage(Guid sessionId, MessageDirection direction, string content, string? intent = null, string? externalId = null)
    {
        if (sessionId == Guid.Empty) throw new ArgumentException("Session is required.");
        if (string.IsNullOrWhiteSpace(content) || content.Length > 4000) throw new ArgumentException("Content invalid.");
        SessionId = sessionId; Direction = direction; Content = content; Intent = intent; ExternalMessageId = externalId;
    }
}
