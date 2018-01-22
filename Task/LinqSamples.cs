// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//
//Copyright (C) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using SampleSupport;
using Task.Data;

// Version Mad01

namespace SampleQueries
{
    [Title("LINQ Module")]
    [Prefix("Linq")]
    public class LinqSamples : SampleHarness
    {

        private DataSource dataSource = new DataSource();

        [Category("Restriction Operators")]
        [Title("Task 1")]
        [Description("Выдайте список всех клиентов, чей суммарный оборот (сумма всех заказов) превосходит некоторую величину X. Продемонстрируйте выполнение запроса с различными X (подумайте, можно ли обойтись без копирования запроса несколько раз)")]
        public void Linq1()
        {
            var x = 100000;

            var customers =
                from p in dataSource.Customers
                where p.Orders.Select(i => i.Total).Sum() > x
                select p;

            Console.WriteLine($"Annual Turnover > :{x}");
            foreach (var customer in customers)
            {
                Console.WriteLine(customer.CustomerID);
            }
        }

        [Category("Restriction Operators")]
        [Title("Task 2")]
        [Description("Для каждого клиента составьте список поставщиков, находящихся в той же стране и том же городе. Сделайте задания с использованием группировки и без.")]
        public void Linq2()
        {
            var customerWithoutGrouping =
                (from c in dataSource.Customers
                 select new
                 {
                     Customer = c,
                     Suppliers = from sup in dataSource.Suppliers
                                 where c.City == sup.City && c.Country == c.Country
                                 select sup
                 }).Where(i => i.Suppliers.Count() > 0);

            ObjectDumper.Write("Customers and suppliers without groupping");
            foreach (var customer in customerWithoutGrouping)
            {
                ObjectDumper.Write($"Customer ID: {customer.Customer.CustomerID}");
                foreach (var supplier in customer.Suppliers)
                {
                    ObjectDumper.Write($"Supplier: {supplier.SupplierName}");
                    ObjectDumper.Write(new string('-', 50));
                }

            }

            var customerWithGrouping =
                from c in dataSource.Customers
                from s in dataSource.Suppliers
                where c.City == s.City && c.Country == s.Country
                group s by c into g
                select new
                {
                    CcustomerId = g.Key.CustomerID,
                    Suppliers = g.Select(i => i.SupplierName).ToList(),
                };

            ObjectDumper.Write("Customers and suppliers with groupping");
            foreach (var customer in customerWithGrouping)
            {
                ObjectDumper.Write($"Customer ID: {customer.CcustomerId}");
                foreach (var supplier in customer.Suppliers)
                {
                    ObjectDumper.Write($"Supplier: {supplier}");
                    ObjectDumper.Write(new string('-', 50));
                }

            }
        }
        [Category("Restriction Operators")]
        [Title("Task 3")]
        [Description("Найдите всех клиентов, у которых были заказы, превосходящие по сумме величину X")]
        public void Linq3()
        {
            var x = 10000;

            var customers =
                from c in dataSource.Customers
                where c.Orders.Any(i => i.Total > x)
                select c;

            Console.WriteLine($"Customer with Order > :{x}");
            foreach (var customer in customers)
            {
                Console.WriteLine(customer.CustomerID);
            }
        }
        [Category("Order Operators")]
        [Title("Task 4")]
        [Description("Выдайте список клиентов с указанием, начиная с какого месяца какого года они стали клиентами (принять за таковые месяц и год самого первого заказа)")]
        public void Linq4()
        {
            var customers =
               (from c in dataSource.Customers
                select new
                {
                    Id = c.CustomerID,
                    FirstOrder = c.Orders.OrderBy(i => i.OrderDate).Select(i => i.OrderDate).FirstOrDefault()
                }).Where(i => !i.FirstOrder.Equals(default(DateTime)));

            foreach (var customer in customers)
            {
                Console.WriteLine($"Customer Id: {customer.Id} First order date: {customer.FirstOrder.ToShortDateString()}");
            }
        }
        [Category("Order Operators")]
        [Title("Task 5")]
        [Description("Сделайте предыдущее задание, но выдайте список отсортированным по году, месяцу, оборотам клиента (от максимального к минимальному) и имени клиента")]
        public void Linq5()
        {
            var customers =
                (from c in dataSource.Customers
                 select new
                 {
                     Customer = c,
                     FirstOrder = c.Orders.OrderBy(i => i.OrderDate).Select(i => i.OrderDate).FirstOrDefault()
                 })
                .Where(i => !i.FirstOrder.Equals(default(DateTime)))
                .OrderByDescending(i => i.FirstOrder.Year)
                .ThenByDescending(i => i.FirstOrder.Month)
                .ThenByDescending(i => i.Customer.Orders
                                               .Select(j => j.Total)
                                               .Sum())
                .ThenByDescending(i => i.Customer.CompanyName);

            foreach (var customer in customers)
            {
                Console.WriteLine($"Customer Id: {customer.Customer.CompanyName} First order date: {customer.FirstOrder}");
            }
        }
        [Category("Restriction Operators")]
        [Title("Task 6")]
        [Description("Укажите всех клиентов, у которых указан нецифровой почтовый код или не заполнен регион или в телефоне не указан код оператора (для простоты считаем, что это равнозначно «нет круглых скобочек в начале»).")]
        public void Linq6()
        {
            var customers = dataSource.Customers.Select(customer =>
            {
                int value;
                bool success = int.TryParse(customer.PostalCode, out value);
                return new { customer, success };
            })
                  .Where(c => !c.success
                              || String.IsNullOrEmpty(c.customer.Region)
                              || c.customer.Phone.StartsWith("(")
                              || c.customer.Phone.StartsWith(")"))
                  .Select(i => i.customer);
            foreach (var customer in customers)
            {
                Console.WriteLine($"Customer Id: {customer.CustomerID} ");
                Console.WriteLine($"Customer PostalCode: {customer.PostalCode} ");
                Console.WriteLine($"Customer Region: {customer.Region} ");
                Console.WriteLine($"Customer Phone: {customer.Phone} ");
            }
        }
        [Category("Grouping Operators")]
        [Title("Task 7")]
        [Description("Сгруппируйте все продукты по категориям, внутри – по наличию на складе, внутри последней группы отсортируйте по стоимости")]
        public void Linq7()
        {
            var products = from p in dataSource.Products
                           group p by p.Category into categoryGroup
                           select new
                           {
                               Category = categoryGroup.Key,
                               UnitsInStock = from itemInStock in categoryGroup
                                              orderby itemInStock.UnitPrice
                                              group itemInStock by itemInStock.UnitsInStock into itemsInStockGroup
                                              select new { itemsInStockGroup.Key, itemsInStockGroup }
                           };
            foreach (var product in products)
            {
                Console.WriteLine($"Category: {product.Category}");

                foreach (var unitInStock in product.UnitsInStock)
                {
                    Console.WriteLine($"Items in stosk: {unitInStock.Key}");
                    foreach (var unit in unitInStock.itemsInStockGroup)
                    {
                        Console.WriteLine($"Unit Price: {unit.UnitPrice}");
                        Console.WriteLine($"Product Id: {unit.ProductID}");
                        Console.WriteLine($"Product Name: {unit.ProductName}");
                    }
                    Console.WriteLine(new string('-', 50));
                }
                Console.WriteLine(new string('!', 50));
            }
        }
        [Category("Grouping Operators")]
        [Title("Task 8")]
        [Description("Сгруппируйте товары по группам «дешевые», «средняя цена», «дорогие». Границы каждой группы задайте сами")]
        public void Linq8()
        {
            const decimal EXPENSIVE = 20;
            const decimal MIDDLE = 8;
            const decimal CHEAP = 2;

            var ranges = new[] { EXPENSIVE, MIDDLE, CHEAP };
            var goods = from g in dataSource.Products
                        orderby g.UnitPrice
                        group g by ranges.FirstOrDefault(r => r < g.UnitPrice) into goodGroup
                        select new
                        {
                            Price = goodGroup.Key,
                            goodGroup
                        };
            foreach (var good in goods)
            {
                Console.WriteLine($"Group products where Unit price more than: {good.Price}");
                Console.WriteLine(new string('-', 50));
                foreach (var product in good.goodGroup)
                {
                    Console.WriteLine(product.UnitPrice);
                }

            }
        }
        [Category("Grouping Operators")]
        [Title("Task 9")]
        [Description("Рассчитайте среднюю прибыльность каждого города (среднюю сумму заказа по всем клиентам из данного города) и среднюю интенсивность (среднее количество заказов, приходящееся на клиента из каждого города)")]
        public void Linq9()
        {
            var cities = from c in dataSource.Customers
                         group c by c.City into cityGroup
                         select new
                         {
                             City = cityGroup.Key,
                             SummOfOrders = cityGroup.ToList().Select(o => o.Orders.Select(t => t.Total).Sum()).Average(),
                             CountOfOrders = cityGroup.ToList().Select(o => o.Orders.Count()).Average()
                         };
            foreach (var city in cities)
            {
                Console.WriteLine($"City: {city.City}");
                Console.WriteLine($"Average summ of orders in city: {city.SummOfOrders}");
                Console.WriteLine($"Average count of orders in city: {city.CountOfOrders}");
            }
        }
        [Category("Grouping Operators")]
        [Title("Task 10")]
        [Description("Сделайте среднегодовую статистику активности клиентов по месяцам (без учета года), статистику по годам, по годам и месяцам (т.е. когда один месяц в разные годы имеет своё значение).")]
        public void Linq10()
        {
            var averageByMonth = from c in dataSource.Customers
                                 select new
                                 {
                                     CustomerName = c.CompanyName,
                                     MonthOrders = from o in c.Orders
                                                   group o by o.OrderDate.Month into monthGroup
                                                   orderby monthGroup.Key
                                                   select new { Month = monthGroup.Key, Count = monthGroup.Count() }
                                 };

            var averageByYear = from c in dataSource.Customers
                                select new
                                {
                                    CustomerName = c.CompanyName,
                                    YearsOrder = from o in c.Orders
                                                 group o by o.OrderDate.Year into yearGrosup
                                                 orderby yearGrosup.Key
                                                 select new { Year = yearGrosup.Key, Count = yearGrosup.Count() }
                                };

            var averageByYearAndMonth = from c in dataSource.Customers
                                        select new
                                        {
                                            CustomerName = c.CompanyName,
                                            YearsOrder = from o in c.Orders
                                                         group o by o.OrderDate.Year into yearGrosup
                                                         orderby yearGrosup.Key
                                                         select new
                                                         {
                                                             Year = yearGrosup.Key,
                                                             MonthCroup =
                                                                from order in yearGrosup
                                                                group order by order.OrderDate.Month into monthGroup
                                                                orderby yearGrosup.Key
                                                                select new { Month = monthGroup.Key, Count = monthGroup.Count() }
                                                         }
                                        };

            foreach (var customer in averageByYearAndMonth)
            {
                Console.WriteLine($"CustomerName: {customer.CustomerName}");
                foreach (var yearsOrder in customer.YearsOrder)
                {
                    Console.WriteLine($"Year: { yearsOrder.Year}");
                    foreach (var monthOrder in yearsOrder.MonthCroup)
                    {
                        Console.WriteLine($"Month: { monthOrder.Month}");
                        Console.WriteLine($"Count of Activities: { monthOrder.Count}");
                    }
                }

            }
        }
    }
}
