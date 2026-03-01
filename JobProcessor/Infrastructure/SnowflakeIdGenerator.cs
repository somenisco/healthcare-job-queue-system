using SnowflakeID;

namespace JobProcessor.Infrastructure;

public sealed class SnowflakeIdGenerator : IIdGenerator
{
    private const int MaxMachineId = 1023;
    private readonly ISnowflakeIDGenerator _generator;

    public SnowflakeIdGenerator(IConfiguration configuration)
    {
        var machineId = configuration.GetValue<int?>("Snowflake:MachineId") ?? 1;
        if (machineId < 0 || machineId > MaxMachineId)
        {
            throw new InvalidOperationException($"Snowflake:MachineId must be between 0 and {MaxMachineId}.");
        }

        _generator = new SnowflakeIDGenerator(machineId);
    }

    public long NextId()
    {
        return checked((long)_generator.GetCode());
    }
}
