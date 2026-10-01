using System;
using System.Collections.Generic;
using System.Linq;

namespace LogiCore.Domain;

// Singleton конфигурации тарифов через Lazy<T>
public sealed class TariffConfig
{
    private static readonly Lazy<TariffConfig> _instance =
        new Lazy<TariffConfig>(() => new TariffConfig());

    public static TariffConfig Instance => _instance.Value;

    private TariffConfig()
    {
        StandardMarkup = 1.0m;
        ExpressMultiplier = 1.35m;
        ExpressPerKm = 0.05m;
        HeavyPerKg = 0.08m;
        InsuranceRate = 0.03m;
        UrgencyMultiplier = 1.2m;
        PackagingFixed = 250m;
    }

    public decimal StandardMarkup { get; }
    public decimal ExpressMultiplier { get; }
    public decimal ExpressPerKm { get; }
    public decimal HeavyPerKg { get; }
    public decimal InsuranceRate { get; }
    public decimal UrgencyMultiplier { get; }
    public decimal PackagingFixed { get; }
}

public interface ITariffStrategy
{
    string Name { get; }
    decimal Calculate(decimal baseCost, Route route, IReadOnlyCollection<Cargo> cargo);
}

public class StandardTariff : ITariffStrategy
{
    public string Name => "Стандарт";

    public decimal Calculate(decimal baseCost, Route route, IReadOnlyCollection<Cargo> cargo)
    {
        return baseCost * TariffConfig.Instance.StandardMarkup;
    }
}

public class ExpressTariff : ITariffStrategy
{
    public string Name => "Экспресс";

    public decimal Calculate(decimal baseCost, Route route, IReadOnlyCollection<Cargo> cargo)
    {
        if (route == null)
            throw new ArgumentNullException(nameof(route));

        return baseCost * TariffConfig.Instance.ExpressMultiplier +
               route.DistanceKm * TariffConfig.Instance.ExpressPerKm;
    }
}

public class HeavyCargoTariff : ITariffStrategy
{
    public string Name => "Тяжеловес";

    public decimal Calculate(decimal baseCost, Route route, IReadOnlyCollection<Cargo> cargo)
    {
        decimal weight = cargo == null ? 0m : cargo.Sum(c => c.WeightKg);
        return baseCost + weight * TariffConfig.Instance.HeavyPerKg;
    }
}

public interface IDeliveryCost
{
    decimal Total { get; }
    string Describe();
}

public class BaseDeliveryCost : IDeliveryCost
{
    private readonly string _description;

    public decimal Total { get; }

    public BaseDeliveryCost(decimal total, string description)
    {
        if (total < 0)
            throw new ArgumentOutOfRangeException(nameof(total), "Стоимость не может быть отрицательной.");

        Total = total;
        _description = description;
    }

    public string Describe()
    {
        return _description;
    }
}

public abstract class DeliveryCostDecorator : IDeliveryCost
{
    protected readonly IDeliveryCost _inner;

    protected DeliveryCostDecorator(IDeliveryCost inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public virtual decimal Total => _inner.Total;

    public virtual string Describe() => _inner.Describe();
}

public class InsuranceDecorator : DeliveryCostDecorator
{
    private readonly decimal _added;

    public InsuranceDecorator(IDeliveryCost inner, decimal declaredValue, decimal rate)
        : base(inner)
    {
        if (declaredValue < 0)
            throw new ArgumentOutOfRangeException(nameof(declaredValue));

        if (rate < 0)
            throw new ArgumentOutOfRangeException(nameof(rate));

        _added = declaredValue * rate;
    }

    public override decimal Total => base.Total + _added;

    public override string Describe()
    {
        return base.Describe() + $" + страховка {_added:c}";
    }
}

public class UrgencyDecorator : DeliveryCostDecorator
{
    private readonly decimal _multiplier;

    public UrgencyDecorator(IDeliveryCost inner, decimal multiplier)
        : base(inner)
    {
        if (multiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(multiplier));

        _multiplier = multiplier;
    }

    public override decimal Total => base.Total * _multiplier;

    public override string Describe()
    {
        return base.Describe() + $" × срочность {_multiplier}";
    }
}

public class PackagingDecorator : DeliveryCostDecorator
{
    private readonly decimal _fixed;

    public PackagingDecorator(IDeliveryCost inner, decimal fixedAmount)
        : base(inner)
    {
        if (fixedAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(fixedAmount));

        _fixed = fixedAmount;
    }

    public override decimal Total => base.Total + _fixed;

    public override string Describe()
    {
        return base.Describe() + $" + упаковка {_fixed:c}";
    }
}

public class VehicleFactory
{
    public Vehicle Create(
        string type,
        Guid id,
        string registrationNumber,
        decimal maxLoadKg,
        decimal maxVolumeM3,
        decimal averageSpeedKmH,
        decimal baseRatePerKm,
        decimal extra1 = 0m,
        decimal extra2 = 0m)
    {
        switch ((type ?? string.Empty).ToLowerInvariant())
        {
            case "truck":
                return new Truck(id, registrationNumber, maxLoadKg, maxVolumeM3, averageSpeedKmH, baseRatePerKm, extra1);

            case "refrigerator":
                return new RefrigeratorTruck(id, registrationNumber, maxLoadKg, maxVolumeM3, averageSpeedKmH, baseRatePerKm, extra1, extra2);

            case "plane":
                return new CargoPlane(id, registrationNumber, maxLoadKg, maxVolumeM3, averageSpeedKmH, baseRatePerKm, extra1);

            case "ship":
                return new CargoShip(id, registrationNumber, maxLoadKg, maxVolumeM3, averageSpeedKmH, baseRatePerKm, extra1);

            case "drone":
                return new DroneCourier(id, registrationNumber, maxLoadKg, maxVolumeM3, averageSpeedKmH, baseRatePerKm, extra1);

            default:
                throw new ArgumentException("Неизвестный тип транспорта.", nameof(type));
        }
    }

    public Vehicle CreateFromDto(VehicleDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        Vehicle vehicle = Create(
            dto.Type,
            dto.Id,
            dto.RegistrationNumber,
            dto.MaxLoadKg,
            dto.MaxVolumeM3,
            dto.AverageSpeedKmH,
            dto.BaseRatePerKm,
            dto.Extra1,
            dto.Extra2);

        vehicle.RestoreState(dto.State);
        return vehicle;
    }
}

public class CargoFactory
{
    public Cargo Create(
        string type,
        Guid id,
        string description,
        decimal weightKg,
        decimal volumeM3,
        decimal declaredValue,
        decimal extra1 = 0m,
        decimal extra2 = 0m,
        DateTime? expirationDate = null)
    {
        switch ((type ?? string.Empty).ToLowerInvariant())
        {
            case "standard":
                return new StandardCargo(id, description, weightKg, volumeM3, declaredValue);

            case "perishable":
                if (!expirationDate.HasValue)
                    throw new CargoValidationException("Для скоропортящегося груза нужна дата годности.");

                return new PerishableCargo(id, description, weightKg, volumeM3, declaredValue, expirationDate.Value, extra1);

            case "fragile":
                return new FragileCargo(id, description, weightKg, volumeM3, declaredValue, extra1);

            case "dangerous":
                return new DangerousCargo(id, description, weightKg, volumeM3, declaredValue, (int)extra1);

            case "oversized":
                return new OversizedCargo(id, description, weightKg, volumeM3, declaredValue, extra1);

            default:
                throw new ArgumentException("Неизвестный тип груза.", nameof(type));
        }
    }

    public Cargo CreateFromDto(CargoDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return Create(
            dto.Type,
            dto.Id,
            dto.Description,
            dto.WeightKg,
            dto.VolumeM3,
            dto.DeclaredValue,
            dto.Extra1,
            dto.Extra2,
            dto.ExpirationDate);
    }
}