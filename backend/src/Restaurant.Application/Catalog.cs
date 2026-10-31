namespace Restaurant.Application;

// ─────────────────────────────────────────────────────────────────────────────
// Catalog DTOs
// ─────────────────────────────────────────────────────────────────────────────

public sealed record RestaurantDto(Guid Id, string Name, string Currency, string TimeZone);
public sealed record CategoryDto(Guid Id, string Name);
public sealed record MenuItemDto(Guid Id, Guid CategoryId, string Name, string Description, long PriceMinor, bool Available);
public sealed record Page<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int Total);
public sealed record SystemStatusDto(string Phase, string PaymentMode, string Currency, string[] AvailableFeatures, string[] PlannedFeatures);

public interface ICatalogQueries
{
    Task<RestaurantDto?> Restaurant(CancellationToken ct);
    Task<IReadOnlyList<CategoryDto>> Categories(CancellationToken ct);
    Task<Page<MenuItemDto>> Items(int page, int pageSize, CancellationToken ct);
    Task<MenuItemDto?> ItemById(Guid id, CancellationToken ct);
    Task<IReadOnlyList<MenuItemDto>> ItemsByIds(IEnumerable<Guid> ids, CancellationToken ct);
}

// ─────────────────────────────────────────────────────────────────────────────
// Customer DTOs
// ─────────────────────────────────────────────────────────────────────────────

public sealed record CustomerDto(Guid Id, string WhatsAppId, string? Name, string? Phone, DateTimeOffset CreatedAt, DateTimeOffset? LastActiveAt);
public sealed record CustomerAddressDto(Guid Id, string Label, string Address, string? Landmark, bool IsDefault);
public sealed record CreateCustomerRequest(string WhatsAppId);
public sealed record UpdateCustomerRequest(string? Name, string? Phone);

public interface ICustomerService
{
    Task<CustomerDto?> GetByWhatsAppId(string whatsAppId, CancellationToken ct);
    Task<CustomerDto> GetOrCreate(string whatsAppId, CancellationToken ct);
    Task<CustomerDto?> GetById(Guid id, CancellationToken ct);
    Task<IReadOnlyList<CustomerDto>> List(int page, int pageSize, CancellationToken ct);
    Task<CustomerDto> Update(Guid id, UpdateCustomerRequest request, CancellationToken ct);
}

// ─────────────────────────────────────────────────────────────────────────────
// Cart DTOs
// ─────────────────────────────────────────────────────────────────────────────

public sealed record CartDto(Guid Id, Guid CustomerId, IReadOnlyList<CartItemDto> Items, long SubtotalMinor, DateTimeOffset UpdatedAt);
public sealed record CartItemDto(Guid Id, Guid MenuItemId, string Name, long UnitPriceMinor, int Quantity, string? Instructions, long LineTotalMinor);
public sealed record AddToCartRequest(Guid MenuItemId, int Quantity, string? Instructions);
public sealed record UpdateCartItemRequest(int Quantity);

public interface ICartService
{
    Task<CartDto> GetOrCreateCart(Guid customerId, CancellationToken ct);
    Task<CartDto?> GetCartByCustomerId(Guid customerId, CancellationToken ct);
    Task<CartItemDto> AddItem(Guid customerId, AddToCartRequest request, CancellationToken ct);
    Task<CartDto> UpdateItemQuantity(Guid customerId, Guid itemId, int quantity, CancellationToken ct);
    Task<CartDto> RemoveItem(Guid customerId, Guid itemId, CancellationToken ct);
    Task ClearCart(Guid customerId, CancellationToken ct);
}

// ─────────────────────────────────────────────────────────────────────────────
// Order DTOs
// ─────────────────────────────────────────────────────────────────────────────

public sealed record OrderDto(
    Guid Id,
    Guid CustomerId,
    string OrderNumber,
    string Status,
    string Fulfillment,
    string? DeliveryAddress,
    string? DeliveryLandmark,
    long SubtotalMinor,
    long DeliveryFeeMinor,
    long DiscountMinor,
    long TotalMinor,
    IReadOnlyList<OrderItemDto> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? CompletedAt,
    string? Notes
);

public sealed record OrderItemDto(Guid Id, Guid MenuItemId, string Name, long UnitPriceMinor, int Quantity, string? Instructions, long LineTotalMinor);
public sealed record OrderSummaryDto(Guid Id, string OrderNumber, string Status, long TotalMinor, DateTimeOffset CreatedAt, int ItemCount);

public sealed record CheckoutRequest(string Fulfillment, string? DeliveryAddress, string? DeliveryLandmark, string? Notes);
public sealed record UpdateOrderStatusRequest(string Status, string? Reason);

public interface IOrderService
{
    Task<OrderDto> Checkout(Guid customerId, CheckoutRequest request, CancellationToken ct);
    Task<OrderDto?> GetById(Guid id, CancellationToken ct);
    Task<OrderDto?> GetByOrderNumber(string orderNumber, CancellationToken ct);
    Task<IReadOnlyList<OrderSummaryDto>> GetByCustomer(Guid customerId, CancellationToken ct);
    Task<Page<OrderSummaryDto>> List(int page, int pageSize, string? status, CancellationToken ct);
    Task<OrderDto> UpdateStatus(Guid id, UpdateOrderStatusRequest request, string? changedBy, CancellationToken ct);
    Task<OrderDto> Cancel(Guid id, string? reason, string? changedBy, CancellationToken ct);
    string GenerateOrderNumber();
}

// ─────────────────────────────────────────────────────────────────────────────
// Payment DTOs
// ─────────────────────────────────────────────────────────────────────────────

public sealed record PaymentAttemptDto(
    Guid Id,
    Guid OrderId,
    string Reference,
    string Method,
    string Status,
    long AmountMinor,
    string Currency,
    string? CheckoutUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? VerifiedAt
);

public sealed record InitializePaymentRequest(Guid OrderId, string Method);
public sealed record PaymentInitializedDto(Guid PaymentAttemptId, string Reference, string? CheckoutUrl);

public interface IPaymentService
{
    Task<PaymentInitializedDto> Initialize(InitializePaymentRequest request, CancellationToken ct);
    Task<PaymentAttemptDto?> GetByReference(string reference, CancellationToken ct);
    Task<PaymentAttemptDto?> GetById(Guid id, CancellationToken ct);
    Task<IReadOnlyList<PaymentAttemptDto>> GetByOrder(Guid orderId, CancellationToken ct);
    Task MarkVerified(string reference, string externalReference, CancellationToken ct);
    Task MarkFailed(string reference, string reason, CancellationToken ct);
}

// ─────────────────────────────────────────────────────────────────────────────
// Conversation DTOs
// ─────────────────────────────────────────────────────────────────────────────

public sealed record ConversationSessionDto(
    Guid Id,
    Guid CustomerId,
    string State,
    string? CurrentIntent,
    Guid? ActiveCartId,
    Guid? ActiveOrderId,
    DateTimeOffset StartedAt,
    DateTimeOffset LastMessageAt
);

public sealed record ConversationMessageDto(Guid Id, string Direction, string Content, string? Intent, DateTimeOffset Timestamp);

public interface IConversationService
{
    Task<ConversationSessionDto> GetOrCreateSession(Guid customerId, CancellationToken ct);
    Task<ConversationSessionDto?> GetActiveSession(Guid customerId, CancellationToken ct);
    Task<ConversationSessionDto> UpdateIntent(Guid sessionId, string? intent, CancellationToken ct);
    Task<ConversationSessionDto> LinkCart(Guid sessionId, Guid cartId, CancellationToken ct);
    Task<ConversationSessionDto> LinkOrder(Guid sessionId, Guid orderId, CancellationToken ct);
    Task<ConversationSessionDto> HandOff(Guid sessionId, string reason, CancellationToken ct);
    Task<ConversationMessageDto> AddMessage(Guid sessionId, string direction, string content, string? intent, string? externalId, CancellationToken ct);
    Task<IReadOnlyList<ConversationMessageDto>> GetMessages(Guid sessionId, int limit, CancellationToken ct);
}
