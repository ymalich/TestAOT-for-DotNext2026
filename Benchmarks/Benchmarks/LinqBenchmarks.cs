using BenchmarkDotNet.Attributes;

namespace Benchmarks;

[MemoryDiagnoser]
////[GcServer(true)]
////[WarmupCount(3)]
//// [IterationCount(8)]
public class LinqBenchmarks
{
    private List<Person> _peopleList = null!;
    private Person[] _peopleArray = null!;
    private List<Employee> _employees = null!;
    private List<Order> _orders = null!;
    private int LoopCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        const int count = 200_000;

        _peopleList = Enumerable.Range(1, count).Select(i => new Person
        {
            Id = i,
            Name = $"Person_{i % 500}",
            Age = 18 + (i % 65),
            City = i % 7 == 0 ? "Moscow" : i % 5 == 0 ? "Saint Petersburg" : "Other",
            IsActive = i % 3 != 0
        }).ToList();

        _peopleArray = _peopleList.ToArray();   // один раз преобразуем в массив

        _employees = Enumerable.Range(1, count).Select(i => new Employee
        {
            Id = i,
            Name = $"Emp_{i}",
            Department = i % 6 == 0 ? "IT" : i % 4 == 0 ? "Sales" : "Support",
            Salary = 50000 + (i % 80000),
            City = i % 5 == 0 ? "Moscow" : "Other"
        }).ToList();

        _orders = Enumerable.Range(1, count / 2).Select(i => new Order
        {
            Id = i,
            CustomerId = i % (count / 10) + 1,
            Amount = 100 + (i % 5000),
            OrderDate = DateTime.Now.AddDays(-i % 365)
        }).ToList();

        LoopCount = 5;
    }

    // 1. Простая фильтрация + проекция + материализация
    [Benchmark]
    public List<string> WhereSelectToList()
    {
        return _peopleList
            .Where(p => p.Age >= 30 && p.IsActive)
            .Select(p => p.Name.ToUpper())
            .ToList();
    }

    // 2. Count с предикатом
    [Benchmark]
    public int WhereCount()
    {
        return _peopleList
            .Count(p => p.Age >= 25 && p.City == "Moscow");
    }

    // 3. GroupBy + агрегация + ToDictionary
    [Benchmark]
    public Dictionary<string, decimal> GroupByAverageSalary()
    {
        return _employees.GroupBy(e => e.Department)
                  .ToDictionary(g => g.Key, g => g.Average(e => e.Salary));
    }

    // 4. OrderBy + ThenBy + Take (пагинация)
    //[Benchmark]
    public List<Person> OrderByTake()
    {
        return _peopleList.OrderBy(p => p.City)
               .ThenBy(p => p.Age)
               .ThenByDescending(p => p.Name)
               .Take(1000)
               .ToList();
    }

    // 5. Join двух коллекций
    [Benchmark(Description = "List: Inner Join")]
    public List<OrderInfo> List_InnerJoinCount1()
    {
        return (from o in _orders
                join e in _employees on o.CustomerId equals e.Id
                where o.Amount > 1000
                select new OrderInfo
                {
                    OrderId = o.Id,
                    EmployeeName = e.Name,
                    Amount = o.Amount,
                    Department = e.Department
                }).ToList();
    }

    ////[Benchmark(Description = "List: Inner Join in Tuple List (LINQ query syntax)")]
    ////public object List_InnerJoinCount2()
    ////{
    ////    var x = (from p in _peopleList
    ////            join e in _employees on p.Id equals e.Id
    ////            where p.Age >= 30 && p.IsActive 
    ////            select (p.Name, e.Department, p.Age, p.IsActive ))
    ////           .ToList();
    ////    return x;
    ////}

    ////[Benchmark(Description = "List: Inner Join in Tuple  List (method syntax)")]
    ////public object List_InnerJoinCount()
    ////{
    ////    var x = _peopleList
    ////        .Join(
    ////            _employees,
    ////            p => p.Id,           // outer key selector
    ////            e => e.Id,           // inner key selector
    ////            (p, e) => (        // result selector
    ////                p.Name,
    ////                e.Department,
    ////                p.Age,
    ////                p.IsActive
    ////            ))
    ////        .Where(x => x.Age >= 30 && x.IsActive)
    ////        .ToList();

    ////    return x;
    ////}

    //[Benchmark(Description = "List: Inner Join List (LINQ query syntax)")]
    public object List_InnerJoinQuerySyntax()
    {
        var x = (from p in _peopleList
                 join e in _employees on p.Id equals e.Id
                 where p.Age >= 30 && p.IsActive
                 select new { p.Name, e.Department, p.Age, p.IsActive })
               .ToList();
        return x;
    }

    [Benchmark(Description = "List: Inner Join List (method syntax)")]
    public object List_InnerJoinMethodSyntax()
    {
        var x = _peopleList
            .Join(
                _employees,
                p => p.Id,           // outer key selector
                e => e.Id,           // inner key selector
                (p, e) => new
                {        // result selector
                    p.Name,
                    e.Department,
                    p.Age,
                    p.IsActive
                })
            .Where(x => x.Age >= 30 && x.IsActive)
            .ToList();

        return x;
    }

    // 6. Any + FirstOrDefault (быстрые проверки)
    // [Benchmark]
    public bool AnyAndFirstOrDefault()
    {
        return _peopleList.Any(p => p.Age > 60) &&
            _peopleList.FirstOrDefault(p => p.City == "Moscow") != null;
    }

    // 7. Complex chained query
    [Benchmark]
    public Dictionary<string, int> ComplexQuery()
    {
        return _peopleList.Where(p => p.IsActive && p.Age >= 25)
               .GroupBy(p => p.City)
               .Select(g => new
               {
                   City = g.Key,
                   Count = g.Count(),
                   AvgAge = (int)g.Average(p => p.Age)
               })
               .OrderByDescending(x => x.Count)
               .Take(10)
               .ToDictionary(x => x.City, x => x.Count);
    }

    // 8. LINQ на массиве vs List (показывает разницу в оптимизациях)
    [Benchmark(Description = "Array: Where + Select + Sum")]
    public long Array_WhereSelectSum()
    {
        return _peopleArray
            .Where(p => p.Age > 40)
            .Select(p => (long)p.Id)
            .Sum();
    }

    [Benchmark(Description = "List: Where + Select + Sum")]
    public long List_WhereSelectSum()
    {
        return _peopleList
            .Where(p => p.Age > 40)
            .Select(p => (long)p.Id)
            .Sum();
    }

    //[Benchmark(Description = "Array: Where + Sum(selector) ")]
    public long Array_WhereSum()
    {
        return _peopleArray
            .Where(p => p.Age > 40)
            .Sum(p => (long)p.Id);
    }

    //[Benchmark(Description = "List: Where + Sum(selector)")]
    public long List_WhereSum()
    {
        return _peopleList
            .Where(p => p.Age > 40)
            .Sum(p => (long)p.Id);
    }

    //[Benchmark(Description = "Array: foreach")]
    public long Array_Foreach_Sum()
    {
        long sum = 0;
        foreach (var p in _peopleArray)
        {
            if (p.Age > 40)
            {
                sum += p.Id;
            }
        }

        return sum;
    }

    //[Benchmark(Description = "List: foreach")]
    public long List_Foreach_Sum()
    {
        long sum = 0;
        foreach (var p in _peopleList)
        {
            if (p.Age > 40)
            {
                sum += p.Id;
            }
        }

        return sum;
    }

    // ==================== DEFERRED EXECUTION / MULTIPLE MATERIALIZATION ====================

    //[Benchmark(Description = "Deferred: Same query materialized 5 times (ToList x5)")]
    public int Deferred_MaterializeFiveTimes()
    {
        // Создаём отложенный запрос один раз
        var query = _peopleList
            .Where(p => p.Age >= 30 && p.IsActive)
            .Select(p => p.Name.Length);

        // Материализуем его 5 раз — имитируем типичную ошибку в коде
        int total = 0;
        for (int i = 0; i < LoopCount; i++)
        {
            total += query.ToList().Count;
        }
        return total;
    }

    [Benchmark(Description = "Deferred: Same query enumerated 5 times with foreach")]
    public int Deferred_EnumerateFiveTimes()
    {
        var query = _peopleList
            .Where(p => p.Age >= 30 && p.IsActive)
            .Select(p => p.Name.Length);

        int total = 0;
        for (int i = 0; i < LoopCount; i++)
        {
            foreach (var length in query)
            {
                total += length;
            }
        }
        return total;
    }

    int _x;
    [Benchmark(Description = "Deferred: Same query enumerated 3 times")]
    public int Deferred_Enumerate3Times()
    {
        var query = _peopleList
            .Where(p => p.Age >= 30 && p.IsActive)
            .Select(p => p.Name.Length);

        var count1 = query.Count(x => x < 3);
        var count2 = query.Count(x => x >= 3);
        var total = query.Sum(x => x);

        return total + count1 + count2;
    }

    // Вспомогательные классы
    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string City { get; set; } = string.Empty;

        public int Height { get; set; }

        public bool IsActive { get; set; }
    }

    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public string City { get; set; } = string.Empty;
    }

    public class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public decimal Amount { get; set; }
        public DateTime OrderDate { get; set; }
    }

    public class OrderInfo
    {
        public int OrderId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Department { get; set; } = string.Empty;
    }
}