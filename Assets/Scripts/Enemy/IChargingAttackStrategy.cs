/// <summary>
/// Attack strategies that telegraph before firing. Movement strategies pause while charging.
/// </summary>
public interface IChargingAttackStrategy : IAttackStrategy
{
    bool IsCharging { get; }
}
