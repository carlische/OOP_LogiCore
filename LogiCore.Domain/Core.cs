using System;
using System.Collections.Generic;

namespace LogiCore.Domain;

// Базовый интерфейс для всех сущностей с идентификаторами
public interface IEntity
{
    Guid Id { get; }
}

public enum VehicleState
{
    Free,
    InTransit,
    UnderMaintenance
}

public enum OrderStatus
{
    Created,
    Assigned,
    InTransit,
    Delivered,
    Cancelled
}

[Flags]
public enum TransportConditions
{
    None = 0,
    Refrigerated = 1,
    Sealed = 2,
    Pressurized = 4,
    LongRange = 8
}

public class LogisticsException : Exception
{
    public LogisticsException(string message) : base(message) { }
    public LogisticsException(string message, Exception inner) : base(message, inner) { }
}

public class CargoValidationException : LogisticsException
{
    public CargoValidationException(string message) : base(message) { }
}

public class IncompatibleCargoException : LogisticsException
{
    public IncompatibleCargoException(string message) : base(message) { }
}

public class VehicleOverloadException : LogisticsException
{
    public VehicleOverloadException(string message) : base(message) { }
}

public class RouteNotFoundException : LogisticsException
{
    public RouteNotFoundException(string message) : base(message) { }
}

public class InvalidOrderStateException : LogisticsException
{
    public InvalidOrderStateException(string message) : base(message) { }
}

public sealed class ValidationResult
{
    public bool IsValid { get; }
    public string Error { get; }

    private ValidationResult(bool isValid, string error)
    {
        IsValid = isValid;
        Error = error;
    }

    public static ValidationResult Ok()
    {
        return new ValidationResult(true, string.Empty);
    }

    public static ValidationResult Fail(string error)
    {
        return new ValidationResult(false, error);
    }
}

// Ковариантный интерфейс только для чтения репозитория
public interface IReadOnlyRepository<out T> where T : class, IEntity
{
    T GetById(Guid id);
    IEnumerable<T> GetAll();
}

// Репозиторий с возможностью записи
public interface IWriteRepository<T> : IReadOnlyRepository<T> where T : class, IEntity
{
    void Add(T item);
    bool Remove(Guid id);
}

// Контравариантный валидатор

public interface IValidator<in T>
{
    ValidationResult Validate(T item);
}

public interface ITemperatureSensitive
{
    decimal RequiredTemperatureC { get; }
    DateTime ExpirationDate { get; }
}

public interface IInsurable
{
    decimal GetInsuranceAmount();
}

public interface IStackable
{
    bool CanStackWith(Cargo other);
}

public interface ITemperatureCapable
{
    decimal MinTemperatureC { get; }
    decimal MaxTemperatureC { get; }
}

public interface IRangeCapable
{
    decimal MaxRangeKm { get; }
}