using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LogiCore.Domain;
using Xunit;

namespace LogiCore.Tests;

public class Tests
{
    private static Route ShortRoute()
    {
        return new Route(new[]
        {
            new RoutePoint(0, 0),
            new RoutePoint(1, 0)
        });
    }

    private static Route ZeroRoute()
    {
        return new Route(new[]
        {
            new RoutePoint(0, 0),
            new RoutePoint(0, 0)
        });
    }

    private static Truck MakeTruck(decimal maxLoadKg = 1000m)
    {
        return new Truck(
            Guid.NewGuid(),
            "A111AA11",
            maxLoadKg,
            100m,
            80m,
            10m,
            1m);
    }

    private static RefrigeratorTruck MakeFridge(decimal min = -5m, decimal max = 5m)
    {
        return new RefrigeratorTruck(
            Guid.NewGuid(),
            "B111BB11",
            1000m,
            100m,
            70m,
            20m,
            min,
            max);
    }

    private static CargoPlane MakePlane()
    {
        return new CargoPlane(
            Guid.NewGuid(),
            "C111CC11",
            1000m,
            100m,
            800m,
            100m,
            1m);
    }

    private static CargoShip MakeShip(decimal oversizeSurcharge = 1000m)
    {
        return new CargoShip(
            Guid.NewGuid(),
            "D111DD11",
            100000m,
            1000m,
            30m,
            10m,
            oversizeSurcharge);
    }

    private static DroneCourier MakeDrone(decimal maxLoadKg = 5m)
    {
        return new DroneCourier(
            Guid.NewGuid(),
            "E111EE11",
            maxLoadKg,
            1m,
            60m,
            100m,
            50m);
    }

    private static StandardCargo MakeCargo(decimal weightKg = 100m)
    {
        return new StandardCargo(
            Guid.NewGuid(),
            "Box",
            weightKg,
            1m,
            1000m);
    }

    private static PerishableCargo MakePerishable(
        decimal temperature = 2m,
        DateTime? expiration = null)
    {
        return new PerishableCargo(
            Guid.NewGuid(),
            "Food",
            100m,
            1m,
            1000m,
            expiration ?? DateTime.UtcNow.AddDays(1),
            temperature);
    }

    private static DangerousCargo MakeDangerous(int dangerClass = 5)
    {
        return new DangerousCargo(
            Guid.NewGuid(),
            "Chemicals",
            100m,
            1m,
            1000m,
            dangerClass);
    }

    private static OversizedCargo MakeOversized()
    {
        return new OversizedCargo(
            Guid.NewGuid(),
            "Machine",
            100m,
            10m,
            10000m,
            1.2m);
    }

    private static FragileCargo MakeFragile()
    {
        return new FragileCargo(
            Guid.NewGuid(),
            "Glass",
            100m,
            1m,
            1000m,
            1.5m);
    }

    private static Customer MakeCustomer()
    {
        return new Customer(Guid.NewGuid(), "Test Customer", "test@test.ru");
    }

    private static Order MakeOrder()
    {
        var customer = MakeCustomer();
        var cargo = MakeCargo();
        var route = ShortRoute();
        return new Order(customer, new[] { cargo }, route);
    }

    [Fact]
    public void Truck_CalculateDeliveryCost_ZeroDistance_UsesWeightSurcharge()
    {
        var truck = MakeTruck();
        var cargo = MakeCargo(10m);
        var route = ZeroRoute();

        decimal cost = truck.CalculateDeliveryCost(route, new[] { cargo });

        Assert.Equal(1m, cost);
    }

    [Fact]
    public void Truck_CanCarry_BoundaryWeight_ReturnsTrue()
    {
        var truck = MakeTruck(100m);
        var cargo = MakeCargo(100m);

        Assert.True(truck.CanCarry(cargo));
    }

    [Fact]
    public void Truck_CanCarry_OverWeight_ReturnsFalse()
    {
        var truck = MakeTruck(100m);
        var cargo = MakeCargo(101m);

        Assert.False(truck.CanCarry(cargo));
    }

    [Fact]
    public void RefrigeratorTruck_CanCarry_PerishableWithinRange_ReturnsTrue()
    {
        var fridge = MakeFridge(-5m, 5m);
        var cargo = MakePerishable(2m);

        Assert.True(fridge.CanCarry(cargo));
    }

    [Fact]
    public void RefrigeratorTruck_CanCarry_PerishableOutsideRange_ReturnsFalse()
    {
        var fridge = MakeFridge(-5m, 5m);
        var cargo = MakePerishable(10m);

        Assert.False(fridge.CanCarry(cargo));
    }

    [Fact]
    public void CargoPlane_CanCarry_ForbiddenDangerClass_ReturnsFalse()
    {
        var plane = MakePlane();
        var cargo = MakeDangerous(2);

        Assert.False(plane.CanCarry(cargo));
    }

    [Fact]
    public void CargoPlane_CanCarry_AllowedDangerClass_ReturnsTrue()
    {
        var plane = MakePlane();
        var cargo = MakeDangerous(5);

        Assert.True(plane.CanCarry(cargo));
    }

    [Fact]
    public void CargoShip_CalculateDeliveryCost_AddsOversizeSurcharge()
    {
        var ship = MakeShip(1000m);
        var cargo = MakeOversized();
        var route = ShortRoute();

        decimal cost = ship.CalculateDeliveryCost(route, new[] { cargo });

        Assert.Equal(1777m, cost);
    }

    [Fact]
    public void DroneCourier_CanCarry_TooHeavy_ReturnsFalse()
    {
        var drone = MakeDrone(5m);
        var cargo = MakeCargo(10m);

        Assert.False(drone.CanCarry(cargo));
    }

    [Fact]
    public void DroneCourier_CanCarry_Dangerous_ReturnsFalse()
    {
        var drone = MakeDrone();
        var cargo = MakeDangerous(5);

        Assert.False(drone.CanCarry(cargo));
    }

    [Fact]
    public void CompatibilityValidator_DangerousAndPerishable_Fails()
    {
        var validator = new CargoCompatibilityValidator();
        var truck = MakeTruck();
        var request = new CargoCompatibilityRequest(
            truck,
            new Cargo[] { MakeDangerous(5), MakePerishable() });

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CompatibilityValidator_PerishableWithoutTemperatureCapable_Fails()
    {
        var validator = new CargoCompatibilityValidator();
        var truck = MakeTruck();
        var request = new CargoCompatibilityRequest(
            truck,
            new Cargo[] { MakePerishable() });

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CompatibilityValidator_PerishableWrongTemperature_Fails()
    {
        var validator = new CargoCompatibilityValidator();
        var fridge = MakeFridge(-5m, 5m);
        var request = new CargoCompatibilityRequest(
            fridge,
            new Cargo[] { MakePerishable(10m) });

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CompatibilityValidator_TotalWeightOverLimit_Fails()
    {
        var validator = new CargoCompatibilityValidator();
        var truck = MakeTruck(100m);
        var request = new CargoCompatibilityRequest(
            truck,
            new Cargo[] { MakeCargo(200m) });

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CompatibilityValidator_ExpiredPerishable_Fails()
    {
        var validator = new CargoCompatibilityValidator();
        var fridge = MakeFridge();
        var request = new CargoCompatibilityRequest(
            fridge,
            new Cargo[] { MakePerishable(2m, DateTime.UtcNow.AddDays(-1)) });

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Order_ValidLifecycle_ChangesStatuses()
    {
        var order = MakeOrder();
        var truck = MakeTruck();

        order.Assign(truck, 100m);
        Assert.Equal(OrderStatus.Assigned, order.Status);

        order.StartDelivery();
        Assert.Equal(OrderStatus.InTransit, order.Status);

        order.Complete();
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public void Order_CancelFromCreated_Works()
    {
        var order = MakeOrder();
        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Order_CancelFromAssigned_Works()
    {
        var order = MakeOrder();
        var truck = MakeTruck();

        order.Assign(truck, 100m);
        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Order_CompleteFromCreated_ThrowsInvalidState()
    {
        var order = MakeOrder();

        Assert.Throws<InvalidOrderStateException>(() => order.Complete());
    }

    [Fact]
    public void Order_AssignTwice_ThrowsInvalidState()
    {
        var order = MakeOrder();
        var truck = MakeTruck();

        order.Assign(truck, 100m);

        Assert.Throws<InvalidOrderStateException>(() => order.Assign(truck, 100m));
    }

    [Fact]
    public void Repository_AddRemoveFindAllIndexerEnumeration_Works()
    {
        var repo = new Repository<StandardCargo>();

        var c1 = MakeCargo(100m);
        var c2 = MakeCargo(10m);

        repo.Add(c1);
        repo.Add(c2);

        Assert.Equal(2, repo.Count);
        Assert.Same(c1, repo[c1.Id]);
        Assert.NotNull(repo.GetById(c2.Id));

        var found = repo.FindAll(x => x.WeightKg > 50m).ToList();
        Assert.Single(found);

        Assert.True(repo.Remove(c1.Id));
        Assert.Null(repo.GetById(c1.Id));

        int count = 0;
        foreach (var item in repo)
        {
            count++;
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public void Decorator_OrderMatters_ChangesTotal()
    {
        IDeliveryCost baseCost = new BaseDeliveryCost(100m, "base");

        var insuranceThenUrgency =
            new UrgencyDecorator(
                new InsuranceDecorator(baseCost, 100m, 0.1m),
                2m);

        var urgencyThenInsurance =
            new InsuranceDecorator(
                new UrgencyDecorator(baseCost, 2m),
                100m,
                0.1m);

        Assert.Equal(220m, insuranceThenUrgency.Total);
        Assert.Equal(210m, urgencyThenInsurance.Total);
    }

    [Fact]
    public void Decorator_PackagingAddsFixedAmount()
    {
        IDeliveryCost baseCost = new BaseDeliveryCost(100m, "base");
        var decorated = new PackagingDecorator(baseCost, 50m);

        Assert.Equal(150m, decorated.Total);
    }

    [Fact]
    public void Serialization_RoundTrip_PreservesCountsAndRevenue()
    {
        var vehicles = new Repository<Vehicle>();
        var customers = new Repository<Customer>();
        var orders = new Repository<Order>();

        var service = new DeliveryService(
            vehicles,
            customers,
            orders,
            new CargoCompatibilityValidator(),
            new StandardTariff());

        var customer = MakeCustomer();
        var truck = MakeTruck();
        var cargo = MakeCargo();
        var route = ShortRoute();

        customers.Add(customer);
        vehicles.Add(truck);

        var order = service.CreateOrder(customer, new[] { cargo }, route);

        Assert.True(service.TryDispatch(order));
        service.StartDelivery(order);
        service.CompleteDelivery(order);

        decimal expectedRevenue = service.Revenue;
        int expectedOrders = orders.Count;

        string path = Path.GetTempFileName();

        try
        {
            var serializer = new StateSerializer(path);
            serializer.Save(StateMapper.ToSnapshot(service));

            foreach (var id in orders.GetAll().Select(o => o.Id).ToList())
                orders.Remove(id);

            foreach (var id in customers.GetAll().Select(c => c.Id).ToList())
                customers.Remove(id);

            foreach (var id in vehicles.GetAll().Select(v => v.Id).ToList())
                vehicles.Remove(id);

            service.RestoreRevenue(0m);

            var loaded = serializer.Load();
            StateMapper.Restore(loaded, vehicles, customers, orders);
            service.RestoreRevenue(loaded.Revenue);

            Assert.Equal(1, vehicles.Count);
            Assert.Equal(1, customers.Count);
            Assert.Equal(expectedOrders, orders.Count);
            Assert.Equal(expectedRevenue, service.Revenue);
        }
        finally
        {
            File.Delete(path);
        }
    }
}