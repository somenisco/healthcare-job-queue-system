namespace JobProcessor.Infrastructure;

public static class QueueKeys
{
    public const string TestOrders = "queue:test_orders";
    public const string ScheduledTestOrders = "queue:scheduled_test_orders";
    public const string TestOrderEvents = "stream:test_orders";
}
