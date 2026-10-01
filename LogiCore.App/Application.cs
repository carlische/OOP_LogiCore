using System;
using LogiCore.Domain;

namespace LogiCore.App;

public class Application
{
    private readonly Repository<Vehicle> _vehicles = new Repository<Vehicle>();
    private readonly Repository<Customer> _customers = new Repository<Customer>();
    private readonly Repository<Order> _orders = new Repository<Order>();

    private readonly DeliveryService _service;
    private readonly ReportService _reports;
    private readonly StateSerializer _serializer;
    private readonly ITariffStrategy _tariff;

    private readonly ConsoleNotifier _consoleNotifier;
    private readonly FileLogger _fileLogger;

    public Application()
    {
        var validator = new CargoCompatibilityValidator();
        _tariff = new ExpressTariff();

        _service = new DeliveryService(
            _vehicles,
            _customers,
            _orders,
            validator,
            _tariff);

        _reports = new ReportService(_vehicles, _customers, _orders);
        _serializer = new StateSerializer("logistate.json");

        _consoleNotifier = new ConsoleNotifier();
        _consoleNotifier.Attach(_service);

        _fileLogger = new FileLogger("deliveries.log");
        _fileLogger.Attach(_service);
    }

    public void Run()
    {
        var demo = new DemoRunner(
            _service,
            _vehicles,
            _customers,
            _orders,
            _reports,
            _serializer,
            _tariff);

        demo.RunInitialScenario();

        var menu = new MainMenu(
            _service,
            _vehicles,
            _customers,
            _orders,
            _reports,
            _serializer);

        menu.Run();
    }
}