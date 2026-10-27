namespace Restaurant.Infrastructure;

// A test-mode inbox record. No order or payment state is stored here.
public sealed class MetaWebhookEvent
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string MessageId { get; private set; } = "";
    public string PhoneNumberId { get; private set; } = "";
    public string SenderWaId { get; private set; } = "";
    public string Text { get; private set; } = "";
    public DateTimeOffset ReceivedAt { get; private set; } = DateTimeOffset.UtcNow;
    public string State { get; private set; } = "Pending";

    private MetaWebhookEvent() { }

    public MetaWebhookEvent(string messageId, string phoneNumberId, string senderWaId, string text)
    {
        MessageId = messageId;
        PhoneNumberId = phoneNumberId;
        SenderWaId = senderWaId;
        Text = text;
    }
}
