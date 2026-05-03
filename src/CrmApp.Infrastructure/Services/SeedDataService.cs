using CrmApp.Core.Abstractions;
using CrmApp.Core.Common;
using CrmApp.Core.Enums;
using CrmApp.Core.Models;
using Microsoft.Extensions.Logging;

namespace CrmApp.Infrastructure.Services;

// Создаёт демо-данные при первом запуске, чтобы приложение не открывалось пустым.
// Запускается из Program.cs до показа LoginForm.
public sealed class SeedDataService
{
    private readonly IUserRepository _users;
    private readonly ICustomerRepository _customers;
    private readonly IDealRepository _deals;
    private readonly IActivityRepository _activities;
    private readonly IProductRepository _products;
    private readonly IAuthService _auth;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<SeedDataService> _logger;

    public SeedDataService(
        IUserRepository users,
        ICustomerRepository customers,
        IDealRepository deals,
        IActivityRepository activities,
        IProductRepository products,
        IAuthService auth,
        IDateTimeProvider clock,
        ILogger<SeedDataService> logger)
    {
        _users = users;
        _customers = customers;
        _deals = deals;
        _activities = activities;
        _products = products;
        _auth = auth;
        _clock = clock;
        _logger = logger;
    }

    public async Task EnsureSeededAsync(CancellationToken ct = default)
    {
        // Если уже есть пользователи - считаем, что данные засеяны.
        var existingUsers = await _users.GetAllAsync(ct).ConfigureAwait(false);
        if (existingUsers.Count > 0)
        {
            return;
        }

        _logger.LogInformation("Засеваю демо-данные при первом запуске");

        await SeedUsersAsync(ct).ConfigureAwait(false);
        var customerIds = await SeedCustomersAsync(ct).ConfigureAwait(false);
        var dealIds = await SeedDealsAsync(customerIds, ct).ConfigureAwait(false);
        await SeedActivitiesAsync(customerIds, dealIds, ct).ConfigureAwait(false);
        await SeedProductsAsync(ct).ConfigureAwait(false);
    }

    private async Task SeedProductsAsync(CancellationToken ct)
    {
        await _products.AddAsync(new Product
        {
            Name = "Лицензия CRM (1 пользователь, год)",
            Sku = "LIC-CRM-USR-Y",
            Category = "Лицензии",
            Description = "Годовая подписка на одно рабочее место.",
            Price = new Money(15_000m),
            IsActive = true,
        }, ct).ConfigureAwait(false);

        await _products.AddAsync(new Product
        {
            Name = "Внедрение и обучение",
            Sku = "SVC-IMPL",
            Category = "Услуги",
            Description = "Установка, настройка, обучение сотрудников (до 8 часов).",
            Price = new Money(45_000m),
            IsActive = true,
        }, ct).ConfigureAwait(false);

        await _products.AddAsync(new Product
        {
            Name = "Техническая поддержка (месяц)",
            Sku = "SVC-SUPP-M",
            Category = "Услуги",
            Description = "Реакция на инциденты в рабочее время, до 5 обращений.",
            Price = new Money(8_000m),
            IsActive = true,
        }, ct).ConfigureAwait(false);

        await _products.AddAsync(new Product
        {
            Name = "Старая редакция CRM (архив)",
            Sku = "LIC-CRM-LEGACY",
            Category = "Лицензии",
            Price = new Money(0m),
            IsActive = false,
        }, ct).ConfigureAwait(false);
    }

    private async Task SeedUsersAsync(CancellationToken ct)
    {
        var admin = new User
        {
            Login = "admin",
            FullName = "Администратор",
            Email = "admin@example.com",
            Role = UserRole.Admin,
            PasswordHash = _auth.HashPassword("admin"),
        };
        await _users.AddAsync(admin, ct).ConfigureAwait(false);

        var manager = new User
        {
            Login = "manager",
            FullName = "Иван Петров",
            Email = "manager@example.com",
            Role = UserRole.Manager,
            PasswordHash = _auth.HashPassword("manager"),
        };
        await _users.AddAsync(manager, ct).ConfigureAwait(false);
    }

    private async Task<List<Guid>> SeedCustomersAsync(CancellationToken ct)
    {
        var ids = new List<Guid>();

        var c1 = new Customer
        {
            Type = CustomerType.Company,
            Status = CustomerStatus.Active,
            Name = "Сидоров А.В.",
            CompanyName = "ООО \"Ромашка\"",
            Inn = "7707083893",
            Phone = "+7 (495) 123-45-67",
            Email = "info@romashka.ru",
            Address = "г. Москва, ул. Ленина, 1",
            Position = "Директор по закупкам",
            Notes = "Постоянный клиент с 2020 года",
        };
        await _customers.AddAsync(c1, ct).ConfigureAwait(false);
        ids.Add(c1.Id);

        var c2 = new Customer
        {
            Type = CustomerType.Company,
            Status = CustomerStatus.Vip,
            Name = "Кузнецова М.С.",
            CompanyName = "АО \"ТехноПром\"",
            Inn = "7728168971",
            Phone = "+7 (812) 987-65-43",
            Email = "kuznetsova@technoprom.ru",
            Address = "г. Санкт-Петербург, Невский пр., 100",
            Position = "Генеральный директор",
        };
        await _customers.AddAsync(c2, ct).ConfigureAwait(false);
        ids.Add(c2.Id);

        var c3 = new Customer
        {
            Type = CustomerType.Person,
            Status = CustomerStatus.Lead,
            Name = "Морозов Дмитрий",
            Phone = "+7 (916) 555-12-34",
            Email = "morozov.d@gmail.com",
            BirthDate = new DateOnly(1985, 3, 14),
            Notes = "Заинтересовался каталогом на выставке",
        };
        await _customers.AddAsync(c3, ct).ConfigureAwait(false);
        ids.Add(c3.Id);

        var c4 = new Customer
        {
            Type = CustomerType.Company,
            Status = CustomerStatus.Inactive,
            Name = "Белов И.Г.",
            CompanyName = "ИП Белов",
            Phone = "+7 (903) 222-33-44",
            Notes = "Не отвечает уже три месяца",
        };
        await _customers.AddAsync(c4, ct).ConfigureAwait(false);
        ids.Add(c4.Id);

        return ids;
    }

    private async Task<List<Guid>> SeedDealsAsync(List<Guid> customerIds, CancellationToken ct)
    {
        var ids = new List<Guid>();
        var today = _clock.Today;

        var d1 = new Deal
        {
            Title = "Поставка оборудования Q2",
            Description = "Партия станков ЧПУ, 5 шт",
            Stage = DealStage.Negotiation,
            Amount = new Money(1_250_000m),
            Probability = 70,
            CustomerId = customerIds[0],
            ExpectedCloseDate = today.AddDays(15),
        };
        await _deals.AddAsync(d1, ct).ConfigureAwait(false);
        ids.Add(d1.Id);

        var d2 = new Deal
        {
            Title = "Годовая лицензия CRM",
            Stage = DealStage.Proposal,
            Amount = new Money(180_000m),
            Probability = 50,
            CustomerId = customerIds[1],
            ExpectedCloseDate = today.AddDays(30),
        };
        await _deals.AddAsync(d2, ct).ConfigureAwait(false);
        ids.Add(d2.Id);

        var d3 = new Deal
        {
            Title = "Консультационные услуги",
            Stage = DealStage.Won,
            Amount = new Money(95_000m),
            Probability = 100,
            CustomerId = customerIds[1],
            ExpectedCloseDate = today.AddDays(-10),
            ActualCloseDate = today.AddDays(-8),
        };
        await _deals.AddAsync(d3, ct).ConfigureAwait(false);
        ids.Add(d3.Id);

        var d4 = new Deal
        {
            Title = "Расширение рабочих мест",
            Stage = DealStage.Qualified,
            Amount = new Money(420_000m),
            Probability = 35,
            CustomerId = customerIds[0],
            ExpectedCloseDate = today.AddDays(45),
        };
        await _deals.AddAsync(d4, ct).ConfigureAwait(false);
        ids.Add(d4.Id);

        var d5 = new Deal
        {
            Title = "Демо-проект",
            Stage = DealStage.New,
            Amount = new Money(50_000m),
            Probability = 20,
            CustomerId = customerIds[2],
            ExpectedCloseDate = today.AddDays(60),
        };
        await _deals.AddAsync(d5, ct).ConfigureAwait(false);
        ids.Add(d5.Id);

        return ids;
    }

    private async Task SeedActivitiesAsync(List<Guid> customerIds, List<Guid> dealIds, CancellationToken ct)
    {
        var now = _clock.Now;

        await _activities.AddAsync(new Activity
        {
            Type = ActivityType.Call,
            Status = ActivityStatus.Planned,
            Priority = Priority.High,
            Title = "Согласовать условия поставки",
            CustomerId = customerIds[0],
            DealId = dealIds[0],
            DueDate = now.AddDays(2),
        }, ct).ConfigureAwait(false);

        await _activities.AddAsync(new Activity
        {
            Type = ActivityType.Meeting,
            Status = ActivityStatus.Planned,
            Priority = Priority.Normal,
            Title = "Презентация решения",
            Description = "Демо в офисе клиента",
            CustomerId = customerIds[1],
            DealId = dealIds[1],
            DueDate = now.AddDays(5),
        }, ct).ConfigureAwait(false);

        await _activities.AddAsync(new Activity
        {
            Type = ActivityType.Email,
            Status = ActivityStatus.Planned,
            Priority = Priority.Low,
            Title = "Отправить КП",
            CustomerId = customerIds[2],
            DueDate = now.AddDays(-3), // Просроченная - попадёт в виджет дашборда
        }, ct).ConfigureAwait(false);

        await _activities.AddAsync(new Activity
        {
            Type = ActivityType.Task,
            Status = ActivityStatus.Completed,
            Priority = Priority.Normal,
            Title = "Подписать договор",
            CustomerId = customerIds[1],
            DealId = dealIds[2],
            DueDate = now.AddDays(-9),
            CompletedAt = now.AddDays(-8),
        }, ct).ConfigureAwait(false);
    }
}
