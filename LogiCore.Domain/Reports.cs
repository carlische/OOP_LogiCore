using System;
using System.Collections.Generic;
using System.Linq;

namespace LogiCore.Domain;

public class ReportService
{
    private readonly IReadOnlyRepository<Vehicle> _vehicles;
    private readonly IReadOnlyRepository<Customer> _customers;
    private readonly IReadOnlyRepository<Order> _orders;

    public ReportService(
        IReadOnlyRepository<Vehicle> vehicles,
        IReadOnlyRepository<Customer> customers,
        IReadOnlyRepository<Order> orders)
    {
        _vehicles = vehicles ?? throw new ArgumentNullException(nameof(vehicles));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _orders = orders ?? throw new ArgumentNullException(nameof(orders));
    }

    public string Top3VehiclesByRevenue()
    {
        var query =
            from o in _orders.GetAll()
            where o.Status == OrderStatus.Delivered && o.AssignedVehicle != null
            join v in _vehicles.GetAll() on o.AssignedVehicle.Id equals v.Id
            group o by v.RegistrationNumber into g
            select new
            {
                Vehicle = g.Key,
                Revenue = g.Sum(x => x.TotalCost)
            };

        return query
            .OrderByDescending(x => x.Revenue)
            .Take(3)
            .ToReportTable("Топ-3 ТС по выручке");
    }

    public string OrdersByStatus()
    {
        var lookup = _orders.GetAll().ToLookup(o => o.Status);

        var result = lookup.Select(g => new
        {
            Status = g.Key,
            Count = g.Count(),
            Sum = g.Sum(o => o.TotalCost)
        });

        return result.ToReportTable("Заказы по статусам");
    }

    public string AverageLoadByVehicleType()
    {
        var result = _vehicles.GetAll()
            .GroupBy(v => v.GetType().Name)
            .Select(g => new
            {
                Type = g.Key,
                AvgLoadPercent = g.Average(v =>
                {
                    var delivered = _orders.GetAll()
                        .Where(o => o.AssignedVehicle != null &&
                                    o.AssignedVehicle.Id == v.Id &&
                                    o.Status == OrderStatus.Delivered)
                        .ToList();

                    if (delivered.Count == 0)
                        return 0m;

                    return delivered.Average(o =>
                        o.Cargos.Sum(c => c.WeightKg) / v.MaxLoadKg * 100m);
                })
            });

        return result.ToReportTable("Средняя загрузка ТС по типам");
    }

    public string CustomersAboveThreshold(decimal threshold)
    {
        var result = _customers.GetAll()
            .Where(c => c.Orders.Sum(o => o.TotalCost) > threshold)
            .Select(c => new
            {
                Name = c.Name,
                Total = c.Orders.Sum(o => o.TotalCost)
            });

        return result.ToReportTable($"Клиенты с суммой заказов выше {threshold:c}");
    }

    public string CargoOrderClientJoin()
    {
        var allCargos = _orders.GetAll()
            .SelectMany(o => o.Cargos.Select(c => new { Cargo = c, Order = o }));

        var result = allCargos.Join(
            _customers.GetAll(),
            x => x.Order.Customer.Id,
            y => y.Id,
            (x, y) => new
            {
                Cargo = x.Cargo.Description,
                Order = x.Order.Id,
                Client = y.Name
            });

        return result.ToReportTable("Груз -> заказ -> клиент");
    }

    public string DangerousClassDictionary()
    {
        var dictionary = _orders.GetAll()
            .SelectMany(o => o.Cargos)
            .OfType<DangerousCargo>()
            .GroupBy(d => d.DangerClass)
            .ToDictionary(g => g.Key, g => g.Count());

        var result = dictionary.Select(kv => new
        {
            DangerClass = kv.Key,
            Count = kv.Value
        });

        return result.ToReportTable("Класс опасности -> количество");
    }

    public string PrintAll()
    {
        return
            Top3VehiclesByRevenue() + Environment.NewLine +
            OrdersByStatus() + Environment.NewLine +
            AverageLoadByVehicleType() + Environment.NewLine +
            CustomersAboveThreshold(10000m) + Environment.NewLine +
            CargoOrderClientJoin() + Environment.NewLine +
            DangerousClassDictionary();
    }
}