using Restaurant.Application;

namespace Restaurant.API;

public static class BusinessEndpoints
{
    public static void MapBusinessEndpoints(this RouteGroupBuilder api)
    {
        // Customer endpoints
        api.MapGet("/customers", async (ICustomerService service, int? page, int? pageSize, CancellationToken ct) =>
        {
            var customers = await service.List(page ?? 1, pageSize ?? 20, ct);
            return Results.Ok(customers);
        });

        api.MapGet("/customers/{id:guid}", async (Guid id, ICustomerService service, CancellationToken ct) =>
            await service.GetById(id, ct) is { } customer ? Results.Ok(customer) : Results.NotFound());

        api.MapGet("/customers/whatsapp/{whatsAppId}", async (string whatsAppId, ICustomerService service, CancellationToken ct) =>
            await service.GetByWhatsAppId(whatsAppId, ct) is { } customer ? Results.Ok(customer) : Results.NotFound());

        api.MapPost("/customers", async (CreateCustomerRequest request, ICustomerService service, CancellationToken ct) =>
        {
            var customer = await service.GetOrCreate(request.WhatsAppId, ct);
            return Results.Created($"/api/v1/customers/{customer.Id}", customer);
        });

        api.MapPatch("/customers/{id:guid}", async (Guid id, UpdateCustomerRequest request, ICustomerService service, CancellationToken ct) =>
        {
            try
            {
                var customer = await service.Update(id, request, ct);
                return Results.Ok(customer);
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        });

        // Cart endpoints
        api.MapGet("/customers/{customerId:guid}/cart", async (Guid customerId, ICartService service, CancellationToken ct) =>
            await service.GetCartByCustomerId(customerId, ct) is { } cart ? Results.Ok(cart) : Results.NotFound());

        api.MapPost("/customers/{customerId:guid}/cart/items", async (Guid customerId, AddToCartRequest request, ICartService service, CancellationToken ct) =>
        {
            try
            {
                var item = await service.AddItem(customerId, request, ct);
                return Results.Created($"/api/v1/customers/{customerId}/cart", item);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        api.MapPatch("/customers/{customerId:guid}/cart/items/{itemId:guid}", async (Guid customerId, Guid itemId, UpdateCartItemRequest request, ICartService service, CancellationToken ct) =>
        {
            try
            {
                var cart = await service.UpdateItemQuantity(customerId, itemId, request.Quantity, ct);
                return Results.Ok(cart);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        api.MapDelete("/customers/{customerId:guid}/cart/items/{itemId:guid}", async (Guid customerId, Guid itemId, ICartService service, CancellationToken ct) =>
        {
            try
            {
                var cart = await service.RemoveItem(customerId, itemId, ct);
                return Results.Ok(cart);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        api.MapDelete("/customers/{customerId:guid}/cart", async (Guid customerId, ICartService service, CancellationToken ct) =>
        {
            await service.ClearCart(customerId, ct);
            return Results.NoContent();
        });

        // Order endpoints
        api.MapPost("/customers/{customerId:guid}/checkout", async (Guid customerId, CheckoutRequest request, IOrderService service, CancellationToken ct) =>
        {
            try
            {
                var order = await service.Checkout(customerId, request, ct);
                return Results.Created($"/api/v1/orders/{order.Id}", order);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        api.MapGet("/orders", async (IOrderService service, int? page, int? pageSize, string? status, CancellationToken ct) =>
        {
            var orders = await service.List(page ?? 1, pageSize ?? 20, status, ct);
            return Results.Ok(orders);
        });

        api.MapGet("/orders/{id:guid}", async (Guid id, IOrderService service, CancellationToken ct) =>
            await service.GetById(id, ct) is { } order ? Results.Ok(order) : Results.NotFound());

        api.MapGet("/orders/number/{orderNumber}", async (string orderNumber, IOrderService service, CancellationToken ct) =>
            await service.GetByOrderNumber(orderNumber, ct) is { } order ? Results.Ok(order) : Results.NotFound());

        api.MapGet("/customers/{customerId:guid}/orders", async (Guid customerId, IOrderService service, CancellationToken ct) =>
        {
            var orders = await service.GetByCustomer(customerId, ct);
            return Results.Ok(orders);
        });

        api.MapPatch("/orders/{id:guid}/status", async (Guid id, UpdateOrderStatusRequest request, IOrderService service, CancellationToken ct) =>
        {
            try
            {
                var order = await service.UpdateStatus(id, request, null, ct);
                return Results.Ok(order);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        api.MapPost("/orders/{id:guid}/cancel", async (Guid id, CancelOrderRequest? request, IOrderService service, CancellationToken ct) =>
        {
            try
            {
                var order = await service.Cancel(id, request?.Reason, null, ct);
                return Results.Ok(order);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // Payment endpoints
        api.MapPost("/payments/initialize", async (InitializePaymentRequest request, IPaymentService service, CancellationToken ct) =>
        {
            try
            {
                var result = await service.Initialize(request, ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        api.MapGet("/payments/{id:guid}", async (Guid id, IPaymentService service, CancellationToken ct) =>
            await service.GetById(id, ct) is { } payment ? Results.Ok(payment) : Results.NotFound());

        api.MapGet("/payments/reference/{reference}", async (string reference, IPaymentService service, CancellationToken ct) =>
            await service.GetByReference(reference, ct) is { } payment ? Results.Ok(payment) : Results.NotFound());

        api.MapGet("/orders/{orderId:guid}/payments", async (Guid orderId, IPaymentService service, CancellationToken ct) =>
        {
            var payments = await service.GetByOrder(orderId, ct);
            return Results.Ok(payments);
        });

        // Payment details (bank account / MoMo) - for agent to share with customer
        api.MapGet("/payments/details", (IPaymentService service) =>
        {
            var details = service.GetPaymentDetails();
            return Results.Ok(details);
        });

        // Upload payment evidence (screenshot)
        api.MapPost("/payments/evidence", async (UploadEvidenceRequest request, IPaymentService service, CancellationToken ct) =>
        {
            try
            {
                var evidence = await service.UploadEvidence(request, ct);
                return Results.Created($"/api/v1/payments/evidence/{evidence.Id}", evidence);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // Admin: Get pending payment reviews
        api.MapGet("/payments/reviews", async (IPaymentService service, CancellationToken ct) =>
        {
            var reviews = await service.GetPendingReviews(ct);
            return Results.Ok(reviews);
        });

        // Admin: Get specific review
        api.MapGet("/payments/reviews/{reviewId:guid}", async (Guid reviewId, IPaymentService service, CancellationToken ct) =>
            await service.GetReviewById(reviewId, ct) is { } review ? Results.Ok(review) : Results.NotFound());

        // Admin: Approve or reject payment
        api.MapPost("/payments/reviews/{reviewId:guid}/decision", async (Guid reviewId, ReviewPaymentRequest request, IPaymentService service, CancellationToken ct) =>
        {
            try
            {
                // TODO: Get actual admin user from auth context when auth is implemented
                var reviewedBy = "Admin";
                var review = await service.ReviewPayment(reviewId, request, reviewedBy, ct);
                return Results.Ok(review);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // Conversation endpoints (for n8n integration)
        api.MapPost("/conversations/session", async (GetOrCreateSessionRequest request, IConversationService service, ICustomerService customerService, CancellationToken ct) =>
        {
            var customer = await customerService.GetOrCreate(request.WhatsAppId, ct);
            var session = await service.GetOrCreateSession(customer.Id, ct);
            return Results.Ok(new { session, customer });
        });

        api.MapGet("/conversations/session/{customerId:guid}", async (Guid customerId, IConversationService service, CancellationToken ct) =>
            await service.GetActiveSession(customerId, ct) is { } session ? Results.Ok(session) : Results.NotFound());

        api.MapPost("/conversations/session/{sessionId:guid}/messages", async (Guid sessionId, AddMessageRequest request, IConversationService service, CancellationToken ct) =>
        {
            try
            {
                var message = await service.AddMessage(sessionId, request.Direction, request.Content, request.Intent, request.ExternalMessageId, ct);
                return Results.Created($"/api/v1/conversations/session/{sessionId}/messages/{message.Id}", message);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        api.MapGet("/conversations/session/{sessionId:guid}/messages", async (Guid sessionId, int? limit, IConversationService service, CancellationToken ct) =>
        {
            var messages = await service.GetMessages(sessionId, limit ?? 50, ct);
            return Results.Ok(messages);
        });

        api.MapPost("/conversations/session/{sessionId:guid}/intent", async (Guid sessionId, UpdateIntentRequest request, IConversationService service, CancellationToken ct) =>
        {
            try
            {
                var session = await service.UpdateIntent(sessionId, request.Intent, ct);
                return Results.Ok(session);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        api.MapPost("/conversations/session/{sessionId:guid}/handoff", async (Guid sessionId, HandoffRequest request, IConversationService service, CancellationToken ct) =>
        {
            try
            {
                var session = await service.HandOff(sessionId, request.Reason, ct);
                return Results.Ok(session);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });
    }
}

// Additional request DTOs
public sealed record CancelOrderRequest(string? Reason);
public sealed record GetOrCreateSessionRequest(string WhatsAppId);
public sealed record AddMessageRequest(string Direction, string Content, string? Intent, string? ExternalMessageId);
public sealed record UpdateIntentRequest(string? Intent);
public sealed record HandoffRequest(string Reason);
