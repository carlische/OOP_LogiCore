using System;
using System.Collections.Generic;
using System.Linq;

namespace LogiCore.Domain;

public class Customer : IEntity
{
    private readonly List<Order> _orders = new List<Order>();

    public Guid Id { get; }
    public string Name { get; }
    public string Contact { get; }

    public IReadOnlyCollection<Order> Orders => _orders;

    public Customer(Guid id, string name, string contact)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id клиента не может быть пустым.", nameof(id));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Имя клиента обязательно.", nameof(name));

        if (string.IsNullOrWhiteSpace(contact))
            throw new ArgumentException("Контакт клиента обязателен.", nameof(contact));

        Id = id;
        Name = name;
        Contact = contact;
    }

    public void AddOrder(Order order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        if (order.Customer != this)
            throw new ArgumentException("Заказ принадлежит другому клиенту.", nameof(order));

        _orders.Add(order);
    }

    public override string ToString()
    {
        return $"Customer {Name}, orders: {_orders.Count}";
    }
}

public class Order : IEntity
{
    private readonly List<Cargo> _cargos = new List<Cargo>();
    private OrderStatus _status;

    public Guid Id { get; }
    public Customer Customer { get; }
    public IReadOnlyCollection<Cargo> Cargos => _cargos;
    public Route Route { get; }
    public Vehicle AssignedVehicle { get; private set; }
    public decimal TotalCost { get; private set; }
    public OrderStatus Status => _status;

    public Order(Customer customer, IEnumerable<Cargo> cargos, Route route)
    {
        if (customer == null)
            throw new ArgumentNullException(nameof(customer));

        if (route == null)
            throw new ArgumentNullException(nameof(route));

        if (cargos == null)
            throw new ArgumentNullException(nameof(cargos));

        foreach (var cargo in cargos)
        {
            if (cargo == null)
                throw new CargoValidationException("В списке грузов не может быть null.");

            _cargos.Add(cargo);
        }

        if (_cargos.Count == 0)
            throw new CargoValidationException("Заказ должен содержать хотя бы один груз.");

        Id = Guid.NewGuid();
        Customer = customer;
        Route = route;
        _status = OrderStatus.Created;
    }

    public void Assign(Vehicle vehicle, decimal totalCost)
    {
        if (_status != OrderStatus.Created)
            throw new InvalidOrderStateException($"Нельзя назначить транспорт из статуса {_status}.");

        if (vehicle == null)
            throw new ArgumentNullException(nameof(vehicle));

        if (totalCost < 0)
            throw new ArgumentOutOfRangeException(nameof(totalCost), "Стоимость не может быть отрицательной.");

        decimal weight = _cargos.Sum(c => c.WeightKg);
        decimal volume = _cargos.Sum(c => c.VolumeM3);

        if (weight > vehicle.MaxLoadKg || volume > vehicle.MaxVolumeM3)
            throw new VehicleOverloadException("Перегруз при назначении транспорта.");

        AssignedVehicle = vehicle;
        TotalCost = totalCost;
        _status = OrderStatus.Assigned;
    }

    public void StartDelivery()
    {
        if (_status != OrderStatus.Assigned)
            throw new InvalidOrderStateException($"Нельзя начать доставку из статуса {_status}.");

        _status = OrderStatus.InTransit;
    }

    public void Complete()
    {
        if (_status != OrderStatus.InTransit)
            throw new InvalidOrderStateException($"Нельзя завершить доставку из статуса {_status}.");

        _status = OrderStatus.Delivered;
    }

    public void Cancel()
    {
        if (_status != OrderStatus.Created && _status != OrderStatus.Assigned)
            throw new InvalidOrderStateException($"Нельзя отменить заказ из статуса {_status}.");

        _status = OrderStatus.Cancelled;
    }

    internal void RestoreFromSnapshot(OrderStatus status, Vehicle vehicle, decimal totalCost)
    {
        _status = status;
        AssignedVehicle = vehicle;
        TotalCost = totalCost;
    }

    public override string ToString()
    {
        return $"Order {Id}, status={Status}, cost={TotalCost:c}, vehicle={AssignedVehicle?.RegistrationNumber ?? "-"}";
    }
}