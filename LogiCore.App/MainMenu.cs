using System;
using System.Linq;
using LogiCore.Domain;

namespace LogiCore.App;

public class MainMenu
{
    private readonly DeliveryService _service;
    private readonly Repository<Vehicle> _vehicles;
    private readonly Repository<Customer> _customers;
    private readonly Repository<Order> _orders;
    private readonly ReportService _reports;
    private readonly StateSerializer _serializer;

    public MainMenu(
        DeliveryService service,
        Repository<Vehicle> vehicles,
        Repository<Customer> customers,
        Repository<Order> orders,
        ReportService reports,
        StateSerializer serializer)
    {
        _service = service;
        _vehicles = vehicles;
        _customers = customers;
        _orders = orders;
        _reports = reports;
        _serializer = serializer;
    }

    public void Run()
    {
        while (true)
        {
            Console.WriteLine("Меню:");
            Console.WriteLine("1 - Создать заказ");
            Console.WriteLine("2 - Показать отчёты");
            Console.WriteLine("3 - Сохранить состояние");
            Console.WriteLine("4 - Загрузить состояние");
            Console.WriteLine("0 - Выход");
            Console.Write("Выбор: ");

            var line = Console.ReadLine();

            if (line == "0")
                break;

            if (line == "1")
                CreateOrder();
            else if (line == "2")
                ShowReports();
            else if (line == "3")
                SaveState();
            else if (line == "4")
                LoadState();
            else
                Console.WriteLine("Неизвестный пункт.");
        }
    }

    private void CreateOrder()
    {
        var customer = _customers.GetAll().FirstOrDefault();

        if (customer == null)
        {
            customer = new Customer(Guid.NewGuid(), "Новый клиент", "new@mail.ru");
            _customers.Add(customer);
        }

        var cargoFactory = new CargoFactory();
        var cargo = cargoFactory.Create(
            "standard",
            Guid.NewGuid(),
            "Новый груз",
            100m,
            1m,
            1000m);

        var route = new Route(new[]
        {
            new RoutePoint(55.75, 37.62),
            new RoutePoint(55.85, 37.72)
        });

        try
        {
            var order = _service.CreateOrder(customer, new[] { cargo }, route);

            if (_service.TryDispatch(order))
            {
                _service.StartDelivery(order);
                _service.CompleteDelivery(order);
                Console.WriteLine("Заказ создан и выполнен.");
            }
            else
            {
                Console.WriteLine("Нет подходящего свободного транспорта.");
            }
        }
        catch (LogisticsException ex)
        {
            Console.WriteLine("Ошибка: " + ex.Message);
        }
    }

    private void ShowReports()
    {
        Console.WriteLine(_reports.PrintAll());
    }

    private void SaveState()
    {
        var snapshot = StateMapper.ToSnapshot(_service);
        _serializer.Save(snapshot);
        Console.WriteLine("Состояние сохранено.");
    }

    private void LoadState()
    {
        ClearState();

        var loaded = _serializer.Load();
        StateMapper.Restore(loaded, _vehicles, _customers, _orders);
        _service.RestoreRevenue(loaded.Revenue);

        Console.WriteLine("Состояние загружено.");
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