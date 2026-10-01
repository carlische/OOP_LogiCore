using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LogiCore.Domain;

public class RoutePointDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class VehicleDto
{
    public string Type { get; set; }
    public Guid Id { get; set; }
    public string RegistrationNumber { get; set; }
    public decimal MaxLoadKg { get; set; }
    public decimal MaxVolumeM3 { get; set; }
    public decimal AverageSpeedKmH { get; set; }
    public decimal BaseRatePerKm { get; set; }
    public VehicleState State { get; set; }
    public TransportConditions Conditions { get; set; }
    public decimal Extra1 { get; set; }
    public decimal Extra2 { get; set; }
}

public class CargoDto
{
    public string Type { get; set; }
    public Guid Id { get; set; }
    public string Description { get; set; }
    public decimal WeightKg { get; set; }
    public decimal VolumeM3 { get; set; }
    public decimal DeclaredValue { get; set; }
    public decimal Extra1 { get; set; }
    public decimal Extra2 { get; set; }
    public DateTime? ExpirationDate { get; set; }
}

public class CustomerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Contact { get; set; }
}

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? VehicleId { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalCost { get; set; }
    public List<RoutePointDto> RoutePoints { get; set; } = new List<RoutePointDto>();
    public List<Guid> CargoIds { get; set; } = new List<Guid>();
}

public class StateSnapshot
{
    public List<VehicleDto> Vehicles { get; set; } = new List<VehicleDto>();
    public List<CargoDto> Cargos { get; set; } = new List<CargoDto>();
    public List<CustomerDto> Customers { get; set; } = new List<CustomerDto>();
    public List<OrderDto> Orders { get; set; } = new List<OrderDto>();
    public decimal Revenue { get; set; }

    public void Normalize()
    {
        Vehicles ??= new List<VehicleDto>();
        Cargos ??= new List<CargoDto>();
        Customers ??= new List<CustomerDto>();
        Orders ??= new List<OrderDto>();
    }
}

public class StateSerializer
{
    private const string DefaultFileName = "logistate.json";

    public string FilePath { get; }

    public StateSerializer(string filePath = null)
    {
        FilePath = string.IsNullOrWhiteSpace(filePath) ? DefaultFileName : filePath;
    }

    public void Save(StateSnapshot snapshot)
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));

        var options = new JsonSerializerOptions { WriteIndented = true };

        using (var stream = new FileStream(FilePath, FileMode.Create, FileAccess.Write))
        {
            JsonSerializer.Serialize(stream, snapshot, options);
        }
    }

    public StateSnapshot Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new StateSnapshot();

            using (var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
            {
                var snapshot = JsonSerializer.Deserialize<StateSnapshot>(stream);
                snapshot?.Normalize();
                return snapshot ?? new StateSnapshot();
            }
        }
        catch (JsonException)
        {
            return new StateSnapshot();
        }
        catch (IOException)
        {
            return new StateSnapshot();
        }
    }
}

public static class StateMapper
{
    public static StateSnapshot ToSnapshot(DeliveryService service)
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));

        var snapshot = new StateSnapshot
        {
            Revenue = service.Revenue
        };

        foreach (var vehicle in service.Vehicles.GetAll())
        {
            snapshot.Vehicles.Add(MapVehicle(vehicle));
        }

        foreach (var customer in service.Customers.GetAll())
        {
            snapshot.Customers.Add(new CustomerDto
            {
                Id = customer.Id,
                Name = customer.Name,
                Contact = customer.Contact
            });
        }

        var cargos = new HashSet<Cargo>();

        foreach (var order in service.Orders.GetAll())
        {
            foreach (var cargo in order.Cargos)
            {
                cargos.Add(cargo);
            }
        }

        foreach (var cargo in cargos)
        {
            snapshot.Cargos.Add(MapCargo(cargo));
        }

        foreach (var order in service.Orders.GetAll())
        {
            snapshot.Orders.Add(MapOrder(order));
        }

        return snapshot;
    }

    public static void Restore(
        StateSnapshot snapshot,
        IWriteRepository<Vehicle> vehicles,
        IWriteRepository<Customer> customers,
        IWriteRepository<Order> orders)
    {
        if (snapshot == null)
            return;

        snapshot.Normalize();

        var vehicleFactory = new VehicleFactory();
        var cargoFactory = new CargoFactory();

        var cargoById = new Dictionary<Guid, Cargo>();

        foreach (var dto in snapshot.Cargos)
        {
            var cargo = cargoFactory.CreateFromDto(dto);
            cargoById[cargo.Id] = cargo;
        }

        var customerById = new Dictionary<Guid, Customer>();

        foreach (var dto in snapshot.Customers)
        {
            var customer = new Customer(dto.Id, dto.Name, dto.Contact);
            customers.Add(customer);
            customerById[customer.Id] = customer;
        }

        var vehicleById = new Dictionary<Guid, Vehicle>();

        foreach (var dto in snapshot.Vehicles)
        {
            var vehicle = vehicleFactory.CreateFromDto(dto);
            vehicles.Add(vehicle);
            vehicleById[vehicle.Id] = vehicle;
        }

        foreach (var dto in snapshot.Orders)
        {
            if (!customerById.TryGetValue(dto.CustomerId, out var customer))
                continue;

            if (dto.CargoIds == null || dto.CargoIds.Count == 0)
                continue;

            var cargos = new List<Cargo>();
            bool ok = true;

            foreach (var cargoId in dto.CargoIds)
            {
                if (!cargoById.TryGetValue(cargoId, out var cargo))
                {
                    ok = false;
                    break;
                }

                cargos.Add(cargo);
            }

            if (!ok)
                continue;

            var points = new List<RoutePoint>();

            if (dto.RoutePoints != null)
            {
                foreach (var p in dto.RoutePoints)
                {
                    points.Add(new RoutePoint(p.Latitude, p.Longitude));
                }
            }

            Route route;

            try
            {
                route = new Route(points);
            }
            catch (RouteNotFoundException)
            {
                continue;
            }

            Vehicle vehicle = null;

            if (dto.VehicleId.HasValue)
            {
                vehicleById.TryGetValue(dto.VehicleId.Value, out vehicle);
            }

            var order = new Order(customer, cargos, route);
            order.RestoreFromSnapshot(dto.Status, vehicle, dto.TotalCost);

            orders.Add(order);
            customer.AddOrder(order);
        }
    }

    private static VehicleDto MapVehicle(Vehicle v)
    {
        var dto = new VehicleDto
        {
            Id = v.Id,
            RegistrationNumber = v.RegistrationNumber,
            MaxLoadKg = v.MaxLoadKg,
            MaxVolumeM3 = v.MaxVolumeM3,
            AverageSpeedKmH = v.AverageSpeedKmH,
            BaseRatePerKm = v.BaseRatePerKm,
            State = v.State,
            Conditions = v.Conditions
        };

        if (v is Truck truck)
        {
            dto.Type = "truck";
            dto.Extra1 = truck.TollCoefficient;
        }
        else if (v is RefrigeratorTruck fridge)
        {
            dto.Type = "refrigerator";
            dto.Extra1 = fridge.MinTemperatureC;
            dto.Extra2 = fridge.MaxTemperatureC;
        }
        else if (v is CargoPlane plane)
        {
            dto.Type = "plane";
            dto.Extra1 = plane.PricePerKg;
        }
        else if (v is CargoShip ship)
        {
            dto.Type = "ship";
            dto.Extra1 = ship.OversizeSurcharge;
        }
        else if (v is DroneCourier drone)
        {
            dto.Type = "drone";
            dto.Extra1 = drone.MaxRangeKm;
        }
        else
        {
            dto.Type = "unknown";
        }

        return dto;
    }

    private static CargoDto MapCargo(Cargo c)
    {
        var dto = new CargoDto
        {
            Id = c.Id,
            Description = c.Description,
            WeightKg = c.WeightKg,
            VolumeM3 = c.VolumeM3,
            DeclaredValue = c.DeclaredValue
        };

        if (c is StandardCargo)
        {
            dto.Type = "standard";
        }
        else if (c is PerishableCargo p)
        {
            dto.Type = "perishable";
            dto.Extra1 = p.RequiredTemperatureC;
            dto.ExpirationDate = p.ExpirationDate;
        }
        else if (c is FragileCargo f)
        {
            dto.Type = "fragile";
            dto.Extra1 = f.RiskCoefficient;
        }
        else if (c is DangerousCargo d)
        {
            dto.Type = "dangerous";
            dto.Extra1 = d.DangerClass;
        }
        else if (c is OversizedCargo o)
        {
            dto.Type = "oversized";
            dto.Extra1 = o.OversizeCoefficient;
        }
        else
        {
            dto.Type = "unknown";
        }

        return dto;
    }

    private static OrderDto MapOrder(Order o)
    {
        var dto = new OrderDto
        {
            Id = o.Id,
            CustomerId = o.Customer.Id,
            VehicleId = o.AssignedVehicle?.Id,
            Status = o.Status,
            TotalCost = o.TotalCost
        };

        foreach (var point in o.Route.Points)
        {
            dto.RoutePoints.Add(new RoutePointDto
            {
                Latitude = point.Latitude,
                Longitude = point.Longitude
            });
        }

        foreach (var cargo in o.Cargos)
        {
            dto.CargoIds.Add(cargo.Id);
        }

        return dto;
    }
}