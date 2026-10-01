using System;
using System.IO;
using LogiCore.Domain;

namespace LogiCore.App;

public class ConsoleNotifier
{
    public void Attach(DeliveryService service)
    {
        service.OrderCreated += OnOrderCreated;
        service.OrderStatusChanged += OnOrderStatusChanged;
        service.VehicleOverloadAttempt += OnVehicleOverloadAttempt;
        service.DeliveryCompleted += OnDeliveryCompleted;
    }

    public void Detach(DeliveryService service)
    {
        service.OrderCreated -= OnOrderCreated;
        service.OrderStatusChanged -= OnOrderStatusChanged;
        service.VehicleOverloadAttempt -= OnVehicleOverloadAttempt;
        service.DeliveryCompleted -= OnDeliveryCompleted;
    }

    private void OnOrderCreated(object sender, OrderCreatedEventArgs e)
    {
        Console.WriteLine($"[CONSOLE] Заказ создан: {e.Order.Id}");
    }

    private void OnOrderStatusChanged(object sender, OrderStatusChangedEventArgs e)
    {
        Console.WriteLine($"[CONSOLE] Статус заказа {e.Order.Id}: {e.OldStatus} -> {e.NewStatus}");
    }

    private void OnVehicleOverloadAttempt(object sender, VehicleOverloadAttemptEventArgs e)
    {
        Console.WriteLine($"[CONSOLE] Попытка перегруза {e.Vehicle.RegistrationNumber}: {e.Reason}");
    }

    private void OnDeliveryCompleted(object sender, DeliveryCompletedEventArgs e)
    {
        Console.WriteLine($"[CONSOLE] Доставка завершена: {e.Order.Id}, выручка +{e.RevenueDelta:c}");
    }
}

public class FileLogger
{
    private readonly string _path;

    public FileLogger(string path)
    {
        _path = path;
    }

    public void Attach(DeliveryService service)
    {
        service.OrderCreated += OnOrderCreated;
        service.OrderStatusChanged += OnOrderStatusChanged;
        service.VehicleOverloadAttempt += OnVehicleOverloadAttempt;
        service.DeliveryCompleted += OnDeliveryCompleted;
    }

    public void Detach(DeliveryService service)
    {
        service.OrderCreated -= OnOrderCreated;
        service.OrderStatusChanged -= OnOrderStatusChanged;
        service.VehicleOverloadAttempt -= OnVehicleOverloadAttempt;
        service.DeliveryCompleted -= OnDeliveryCompleted;
    }

    private void OnOrderCreated(object sender, OrderCreatedEventArgs e)
    {
        Write($"ORDER_CREATED {e.Order.Id}");
    }

    private void OnOrderStatusChanged(object sender, OrderStatusChangedEventArgs e)
    {
        Write($"STATUS_CHANGED {e.Order.Id} {e.OldStatus}->{e.NewStatus}");
    }

    private void OnVehicleOverloadAttempt(object sender, VehicleOverloadAttemptEventArgs e)
    {
        Write($"OVERLOAD_ATTEMPT {e.Vehicle.RegistrationNumber} {e.Reason}");
    }

    private void OnDeliveryCompleted(object sender, DeliveryCompletedEventArgs e)
    {
        Write($"DELIVERY_COMPLETED {e.Order.Id} cost={e.Order.TotalCost}");
    }

    private void Write(string message)
    {
        using (var writer = new StreamWriter(_path, true))
        {
            writer.WriteLine($"{DateTime.Now:O} {message}");
        }
    }
}