using System;
using System.Collections.Generic;
using System.Linq;
using LogiCore.Domain;

namespace LogiCore.App;

public class DemoRunner
{
    private readonly DeliveryService _service;
    private readonly Repository<Vehicle> _vehicles;
    private readonly Repository<Customer> _customers;
    private readonly Repository<Order> _orders;
    private readonly ReportService _reports;
    private readonly StateSerializer _serializer;
    private readonly ITariffStrategy _tariff;

    private readonly VehicleFactory _vehicleFactory = new VehicleFactory();
    private readonly CargoFactory _cargoFactory = new CargoFactory();

    private RoutePoint _pointMsk;
    private RoutePoint _pointSpb;
    private RoutePoint _pointNearby;

    private Route _longRoute;
    private Route _shortRoute;

    private Truck _truck1;
    private DroneCourier _drone;

    private Customer _cust1;
    private Customer _cust2;
    private Customer _cust3;

    private StandardCargo _std1;
    private StandardCargo _std2;
    private PerishableCargo _perish1;
    private PerishableCargo _perish2;
    private FragileCargo _fragile1;
    private FragileCargo _fragile2;
    private DangerousCargo _danger1;
    private DangerousCargo _danger2;
    private OversizedCargo _over1;
    private OversizedCargo _over2;
    private DangerousCargo _danger3;
    private PerishableCargo _perish3;

    private Order _orderStandard;
    private Order _orderPerishable;
    private Order _orderFragile;
    private Order _orderDangerous;
    private Order _orderOversized;

    private readonly List<Order> _validOrders = new List<Order>();

    public DemoRunner(
        DeliveryService service,
        Repository<Vehicle> vehicles,
        Repository<Customer> customers,
        Repository<Order> orders,
        ReportService reports,
        StateSerializer serializer,
        ITariffStrategy tariff)
    {
        _service = service;
        _vehicles = vehicles;
        _customers = customers;
        _orders = orders;
        _reports = reports;
        _serializer = serializer;
        _tariff = tariff;
    }

    public void RunInitialScenario()
    {
        Console.WriteLine("Демо LogiCore");

        SeedRoutes();
        SeedPark();
        SeedCargos();
        CreateCustomersAndOrders();

        DemonstrateLanguageFeatures();
        DemonstrateExceptionFilters();
        ShowOverloadAttempt();
        ShowCostSteps();
        RunFullLifecycleWithTemporarySubscriber();

        PrintReports();
        SaveLoadDemo();

        Console.WriteLine("Демо завершено");
        Console.WriteLine();
    }

    private void SeedRoutes()
    {
        _pointMsk = new RoutePoint(55.75, 37.62);
        _pointSpb = new RoutePoint(59.94, 30.31);
        _pointNearby = new RoutePoint(55.85, 37.72);

        _longRoute = new Route(new[] { _pointMsk, _pointSpb });
        _shortRoute = new Route(new[] { _pointMsk, _pointNearby });
    }

    private void SeedPark()
    {
        _truck1 = (Truck)_vehicleFactory.Create(
            "truck",
            Guid.NewGuid(),
            "A111AA77",
            20000m,
            100m,
            80m,
            50m,
            1.2m);

        var fridge = (RefrigeratorTruck)_vehicleFactory.Create(
            "refrigerator",
            Guid.NewGuid(),
            "B222BB77",
            15000m,
            80m,
            75m,
            70m,
            -5m,
            5m);

        var plane = (CargoPlane)_vehicleFactory.Create(
            "plane",
            Guid.NewGuid(),
            "C333CC77",
            5000m,
            30m,
            800m,
            300m,
            2m);

        var ship = (CargoShip)_vehicleFactory.Create(
            "ship",
            Guid.NewGuid(),
            "D444DD77",
            100000m,
            500m,
            30m,
            10m,
            1000m);

        _drone = (DroneCourier)_vehicleFactory.Create(
            "drone",
            Guid.NewGuid(),
            "E555EE77",
            5m,
            0.1m,
            60m,
            100m,
            50m);

        var truck2 = (Truck)_vehicleFactory.Create(
            "truck",
            Guid.NewGuid(),
            "F666FF77",
            10000m,
            50m,
            80m,
            45m,
            1.1m);

        _vehicles.Add(_truck1);
        _vehicles.Add(fridge);
        _vehicles.Add(plane);
        _vehicles.Add(ship);
        _vehicles.Add(_drone);
        _vehicles.Add(truck2);
    }

    private void SeedCargos()
    {
        _std1 = (StandardCargo)_cargoFactory.Create(
            "standard",
            Guid.NewGuid(),
            "Коробки",
            1000m,
            5m,
            10000m);

        _std2 = (StandardCargo)_cargoFactory.Create(
            "standard",
            Guid.NewGuid(),
            "Мешки",
            1500m,
            6m,
            12000m);

        _perish1 = (PerishableCargo)_cargoFactory.Create(
            "perishable",
            Guid.NewGuid(),
            "Фрукты",
            500m,
            3m,
            8000m,
            extra1: 2m,
            expirationDate: DateTime.UtcNow.AddDays(3));

        _perish2 = (PerishableCargo)_cargoFactory.Create(
            "perishable",
            Guid.NewGuid(),
            "Овощи",
            600m,
            3m,
            7000m,
            extra1: 4m,
            expirationDate: DateTime.UtcNow.AddDays(2));

        _fragile1 = (FragileCargo)_cargoFactory.Create(
            "fragile",
            Guid.NewGuid(),
            "Стекло",
            300m,
            2m,
            20000m,
            extra1: 1.5m);

        _fragile2 = (FragileCargo)_cargoFactory.Create(
            "fragile",
            Guid.NewGuid(),
            "Керамика",
            250m,
            2m,
            18000m,
            extra1: 1.4m);

        _danger1 = (DangerousCargo)_cargoFactory.Create(
            "dangerous",
            Guid.NewGuid(),
            "Растворитель",
            400m,
            2m,
            15000m,
            extra1: 5m);

        _danger2 = (DangerousCargo)_cargoFactory.Create(
            "dangerous",
            Guid.NewGuid(),
            "Аккумуляторы",
            350m,
            2m,
            14000m,
            extra1: 4m);

        _over1 = (OversizedCargo)_cargoFactory.Create(
            "oversized",
            Guid.NewGuid(),
            "Станок",
            8000m,
            40m,
            100000m,
            extra1: 1.3m);

        _over2 = (OversizedCargo)_cargoFactory.Create(
            "oversized",
            Guid.NewGuid(),
            "Балка",
            7000m,
            35m,
            90000m,
            extra1: 1.2m);

        _danger3 = (DangerousCargo)_cargoFactory.Create(
            "dangerous",
            Guid.NewGuid(),
            "Газ баллон",
            200m,
            1m,
            5000m,
            extra1: 2m);

        _perish3 = (PerishableCargo)_cargoFactory.Create(
            "perishable",
            Guid.NewGuid(),
            "Молоко",
            200m,
            1m,
            3000m,
            extra1: 3m,
            expirationDate: DateTime.UtcNow.AddDays(1));
    }

    private void CreateCustomersAndOrders()
    {
        _cust1 = new Customer(Guid.NewGuid(), "ООО Ромашка", "romashka@mail.ru");
        _cust2 = new Customer(Guid.NewGuid(), "ИП Петров", "petrov@mail.ru");
        _cust3 = new Customer(Guid.NewGuid(), "ЗАО Вектор", "vector@mail.ru");

        _customers.Add(_cust1);
        _customers.Add(_cust2);
        _customers.Add(_cust3);

        _orderStandard = _service.CreateOrder(_cust1, new[] { _std1, _std2 }, _longRoute);
        _orderPerishable = _service.CreateOrder(_cust1, new[] { _perish1, _perish2 }, _longRoute);
        _orderFragile = _service.CreateOrder(_cust2, new[] { _fragile1, _fragile2 }, _shortRoute);
        _orderDangerous = _service.CreateOrder(_cust2, new[] { _danger1, _danger2 }, _longRoute);
        _orderOversized = _service.CreateOrder(_cust3, new[] { _over1, _over2 }, _longRoute);

        _validOrders.Add(_orderStandard);
        _validOrders.Add(_orderPerishable);
        _validOrders.Add(_orderFragile);
        _validOrders.Add(_orderDangerous);
        _validOrders.Add(_orderOversized);

        Console.WriteLine();
        Console.WriteLine("Пытаемся создать невалидный заказ: опасный и скоропортящийся.");

        try
        {
            _service.CreateOrder(_cust3, new Cargo[] { _danger3, _perish3 }, _longRoute);
        }
        catch (IncompatibleCargoException ex)
        {
            Console.WriteLine("Нашли IncompatibleCargoException: " + ex.Message);
        }
    }

    private void DemonstrateLanguageFeatures()
    {
        Console.WriteLine();
        Console.WriteLine("Демонстрация возможностей C#");

        // Ковариантность IReadOnlyRepository<out T>
        IReadOnlyRepository<IEntity> entityRepo = _vehicles;
        var firstVehicle = entityRepo.GetAll().FirstOrDefault();

        if (firstVehicle != null)
        {
            var found = entityRepo.GetById(firstVehicle.Id);
            Console.WriteLine($"Ковариантность: найден объект {found?.GetType().Name}");
        }

        // Контравариантность IValidator<in T>
        IValidator<PerishableCargo> perishableValidator = new CargoValidator();
        var validationResult = perishableValidator.Validate(_perish1);
        Console.WriteLine($"Контравариантность: валиден={validationResult.IsValid}");

        // Явная реализация интерфейса
        IInsurable insurable = _perish1;
        decimal insurance = insurable.GetInsuranceAmount();
        Console.WriteLine($"Явная реализация IInsurable: {insurance:c}");

        // Flags enum
        TransportConditions conditions = TransportConditions.Refrigerated | TransportConditions.Sealed;
        bool hasRefrigerated = conditions.HasFlag(TransportConditions.Refrigerated);
        Console.WriteLine($"Flags: {conditions}, HasFlag(Refrigerated)={hasRefrigerated}");

        // Структура и операторы
        double km = _pointMsk - _pointSpb;
        string pointAsString = (string)_pointMsk;
        Console.WriteLine($"RoutePoint: расстояние ~{km:F1} км, точка={pointAsString}");
    }

    private void DemonstrateExceptionFilters()
    {
        Console.WriteLine();
        Console.WriteLine("=== Демонстрация исключений ===");

        try
        {
            try
            {
                throw new VehicleOverloadException("Демонстрация фильтра when и throw;");
            }
            catch (LogisticsException ex) when (ex is VehicleOverloadException)
            {
                Console.WriteLine("Фильтр when поймал VehicleOverloadException.");
                throw;
            }
        }
        catch (VehicleOverloadException)
        {
            Console.WriteLine("Повторный проброс пойман снаружи.");
        }
        finally
        {
            Console.WriteLine("finally выполнен.");
        }
    }

    private void ShowOverloadAttempt()
    {
        Console.WriteLine();
        Console.WriteLine("Попытка перегруза дрона");

        _service.AttemptOverload(_drone, new[] { _over1 });
    }

    private void ShowCostSteps()
    {
        Console.WriteLine();
        Console.WriteLine("Расчёт стоимости по шагам");

        decimal baseCost = _truck1.CalculateDeliveryCost(_longRoute, _orderStandard.Cargos);
        decimal tariffCost = _tariff.Calculate(baseCost, _longRoute, _orderStandard.Cargos);

        Console.WriteLine($"Базовая стоимость ТС: {baseCost:c}");
        Console.WriteLine($"После тарифа {_tariff.Name}: {tariffCost:c}");

        decimal declaredValue = _orderStandard.Cargos.Sum(c => c.DeclaredValue);

        IDeliveryCost cost = new BaseDeliveryCost(tariffCost, $"Тариф {_tariff.Name}");
        Console.WriteLine(cost.Describe());

        cost = new InsuranceDecorator(cost, declaredValue, TariffConfig.Instance.InsuranceRate);
        Console.WriteLine(cost.Describe());

        cost = new UrgencyDecorator(cost, TariffConfig.Instance.UrgencyMultiplier);
        Console.WriteLine(cost.Describe());

        cost = new PackagingDecorator(cost, TariffConfig.Instance.PackagingFixed);
        Console.WriteLine(cost.Describe());

        Console.WriteLine($"Итоговая стоимость с услугами: {cost.Total:c}");
    }

    private void RunFullLifecycleWithTemporarySubscriber()
    {
        Console.WriteLine();
        Console.WriteLine("Полный жизненный цикл заказов");

        var temporaryLogger = new FileLogger("temporary-demo.log");
        temporaryLogger.Attach(_service);

        foreach (var order in _validOrders)
        {
            if (_service.TryDispatch(order))
            {
                _service.StartDelivery(order);
                _service.CompleteDelivery(order);
            }
            else
            {
                Console.WriteLine($"Не удалось подобрать ТС для заказа {order.Id}");
            }
        }

        temporaryLogger.Detach(_service);
        Console.WriteLine("Временный подписчик FileLogger отписан.");
    }

    private void PrintReports()
    {
        Console.WriteLine();
        Console.WriteLine("Отчёты LINQ");
        Console.WriteLine(_reports.PrintAll());
    }

    private void SaveLoadDemo()
    {
        Console.WriteLine();
        Console.WriteLine("Сохранение и загрузка состояния");

        decimal revenueBefore = _service.Revenue;
        int vehiclesBefore = _vehicles.Count;
        int ordersBefore = _orders.Count;

        var snapshot = StateMapper.ToSnapshot(_service);
        _serializer.Save(snapshot);

        ClearState();

        var loaded = _serializer.Load();
        StateMapper.Restore(loaded, _vehicles, _customers, _orders);
        _service.RestoreRevenue(loaded.Revenue);

        Console.WriteLine($"До сохранения: ТС={vehiclesBefore}, заказы={ordersBefore}, выручка={revenueBefore:c}");
        Console.WriteLine($"После загрузки: ТС={_vehicles.Count}, заказы={_orders.Count}, выручка={_service.Revenue:c}");
        Console.WriteLine("Повторный отчёт после загрузки:");
        Console.WriteLine(_reports.Top3VehiclesByRevenue());
    }

    private void ClearState()
    {
        foreach (var id in _orders.GetAll().Select(o => o.Id).ToList())
            _orders.Remove(id);

        foreach (var id in _customers.GetAll().Select(c => c.Id).ToList())
            _customers.Remove(id);

        foreach (var id in _vehicles.GetAll().Select(v => v.Id).ToList())
            _vehicles.Remove(id);

        _service.RestoreRevenue(0m);
    }
}