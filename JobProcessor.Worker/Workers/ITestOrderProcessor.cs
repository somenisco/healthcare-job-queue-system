namespace JobProcessor.Workers;

public interface ITestOrderProcessor
{
    Task ProcessAsync(long testOrderId, CancellationToken cancellationToken);
}
