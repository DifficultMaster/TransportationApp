using AppServer.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using AppServer.Network;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Text.Json;
using System.Diagnostics;
using AppServer.Data;
using System.Reflection.Metadata.Ecma335;

namespace AppServer.Data
{
    public static class DataService
    {       
        private static Dictionary<string, string> userCredentials = new Dictionary<string, string>();

        public static Dictionary<User, DataService.TableName> users = new Dictionary<User, DataService.TableName>();

        public static AppDbContext context { get; private set; } = new AppDbContext();

        public static string ipAddress { get; private set; } = string.Empty;

        public static int port { get; private set; } = 0;

        public enum TableName
        {
            ADDRESSES,
            DEPOTS,
            DISPATCHERS,
            DRIVERS,
            ENGINES,
            EVENTS,
            PERSONS,
            ROUTES,
            SCHEDULE,
            STOPS,
            VALIDATIONS,
            VEHICLES,
            VEHICLETYPES,
            NONE
        }

        public enum ColumnType
        {
            KEY,
            NUMERIC,
            TEXT,
            DATE,
            TIME,
            DATETIME,
            NONE
        }

        public enum AccessLevel
        {
            VIEWER,
            DRIVER,
            DISPATCHER,
            ADMIN
        }

        private static void LogEdit(User user, TableName tableName, string actionType, string query)
        {
            var person = context.Persons
                    .FirstOrDefault(p => p.Login == user.login);

            if (person == null) throw new Exception("Invalid person");

            Event log = new Event
            {
                PersonId = person.PersonId,
                TableName = tableName.ToString(),
                ActionType = actionType,
                Query = query,
                DateTime = DateTime.Now
            };

            context.Events.Add(log);
            context.SaveChanges();
        }

        private static void DownloadCredentialsFromFile()
        {
            string[] lines = File.ReadAllLines(@$"{Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetParent(AppContext.BaseDirectory).FullName).FullName).FullName).FullName}\Data\Accounts\credentials.txt");
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] parts = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 4)
                {
                    continue;
                }

                string login = parts[1].TrimEnd(',');
                string password = parts[3].TrimEnd(',');

                userCredentials[login] = PasswordHandler.GetHashedPassword(password);
                LogToConsole($"!!!!!!!!!!!!!SERVER!!!!!!!!!!!!!! --- User credentials uploaded for login {login}");
            }
        }

        private static void AddValidation(User user, string directive)
        {
            if (user.accessLevel != AccessLevel.VIEWER)
            {
                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        var driver = context.Persons                                           
                                            .Where(p => p.Login == user.login)
                                            .Join(context.Drivers,
                                                p => p.PersonId,
                                                d => d.PersonId,
                                                (p, d) => new { p.PersonId })
                                            .SingleOrDefault();

                        if (driver == null) throw new Exception();

                        DateTime now = DateTime.Now;
                        var closestSchedule = context.Schedules                         
                            .Where(s => s.DriverId == driver.PersonId)
                            .Select(s => new
                            {
                                s.ScheduleId,
                                AbsStartDiff = Math.Abs(EF.Functions.DateDiffSecond(s.StartDate, now)),
                                AbsEndDiff = Math.Abs(EF.Functions.DateDiffSecond(s.EndDate, now))
                            })
                            .Select(s => new
                            {
                                s.ScheduleId,
                                MinTimeDiff = s.AbsStartDiff < s.AbsEndDiff ? s.AbsStartDiff : s.AbsEndDiff
                            })
                            .OrderBy(s => s.MinTimeDiff)
                            .FirstOrDefault();

                        if (closestSchedule == null) throw new Exception();

                        Validation validation = new Validation
                        {
                            DateTime = now,
                            ScheduleId = closestSchedule.ScheduleId
                        };

                        context.Validations.Add(validation);
                        context.SaveChanges();

                        LogEdit(user, TableName.VALIDATIONS, "INSERT", directive);

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw ex;
                    }  
                }
            }
            else throw new Exception("Access denied");
        }

        private static void EditContact(User user, string directive, string keyOrValue)
        {
            if (user.accessLevel != AccessLevel.VIEWER)
            {        
                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        var person = context.Persons
                        .Where(p => p.Login == user.login)
                        .SingleOrDefault();

                        if (person == null) throw new Exception("Invalid person");

                        person.ContactNumber = keyOrValue;
                        context.SaveChanges();

                        LogEdit(user, TableName.PERSONS, "UPDATE", directive);
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw ex;
                    }
                }
            }
            else throw new Exception("Access denied");
        }

        public static void DeleteLogs(User user, string directive)
        {
            if (user.accessLevel != AccessLevel.ADMIN)
                throw new Exception("Access denied");
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    var cutoff = System.DateTime.Now.AddHours(-48);
                    var eventsToDelete = context.Events.Where(e => e.DateTime < cutoff).ToList();

                    if (eventsToDelete.Any())
                    {
                        context.Events.RemoveRange(eventsToDelete);
                        context.SaveChanges();
                    }

                    LogEdit(user, TableName.EVENTS, "DELETE", directive);
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw ex;
                }
            }
        }

        public static void DeleteValidations(User user, string directive)
        {
            if (user.accessLevel != AccessLevel.ADMIN)
                throw new Exception("Access denied");
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    var cutoff = System.DateTime.Now.AddDays(-7);
                    var validationsToDelete = context.Validations.Where(v => v.DateTime < cutoff).ToList();

                    if (validationsToDelete.Any())
                    {
                        context.Validations.RemoveRange(validationsToDelete);
                        context.SaveChanges();
                    }

                    LogEdit(user, TableName.VALIDATIONS, "DELETE", directive);
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw ex;
                }
            }
        }

        public static class SqlFunctions
        {
            [DbFunction("DATEDIFF", Schema = "")]
            public static int DateDiff(string datepart, DateTime start, DateTime end)
            {
                throw new NotImplementedException("This method should only be used in EF Core queries.");
            }

            [DbFunction("ABS", Schema = "")]
            public static long Abs(long value) => Math.Abs(value);
        }

        public static string Select(User user, TableName tableName, string message)
        {
            string[] messageParts = message.Split('~');
            string result = string.Empty;

            switch (messageParts[2])
            {
                case "ALLADDRESSES":
                    {
                        if (user.accessLevel != AccessLevel.ADMIN)
                            throw new Exception("Access denied");
                        
                        List<Address> addresses = context.Addresses
                            .AsNoTracking()
                            .ToList();

                        result = System.Text.Json.JsonSerializer.Serialize(addresses, new JsonSerializerOptions
                        {                            
                            WriteIndented = true
                        });
                        break;       
                    }

                case "ALLDEPOTS":
                    {
                        if (user.accessLevel != AccessLevel.ADMIN)
                            throw new Exception("Access denied");

                        List<Depot> depots = context.Depots
                            .AsNoTracking()
                            .ToList();

                        result = System.Text.Json.JsonSerializer.Serialize(depots, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });
                        break;
                    }

                case "ALLDISPATCHERS":
                    {
                        if (user.accessLevel != AccessLevel.ADMIN)
                            throw new Exception("Access denied");

                        List<Dispatcher> dispatchers = context.Dispatchers
                            .AsNoTracking()
                            .ToList();

                        result = System.Text.Json.JsonSerializer.Serialize(dispatchers, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });
                        break;
                    }

                case "ALLDRIVERS":
                    {
                        if (user.accessLevel != AccessLevel.ADMIN)
                            throw new Exception("Access denied");

                        List<Driver> drivers = context.Drivers
                            .AsNoTracking()
                            .ToList();

                        result = System.Text.Json.JsonSerializer.Serialize(drivers, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });
                        break;
                    }

                case "ALLENGINES":
                    {
                        if (user.accessLevel != AccessLevel.ADMIN)
                            throw new Exception("Access denied");

                        List<Engine> engines = context.Engines
                            .AsNoTracking()
                            .ToList();

                        result = System.Text.Json.JsonSerializer.Serialize(engines, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });
                        break;
                    }

                case "ALLPERSONS":
                    {
                        if (user.accessLevel != AccessLevel.ADMIN)
                            throw new Exception("Access denied");

                        List<Person> persons = context.Persons
                            .AsNoTracking()
                            .ToList();

                        result = System.Text.Json.JsonSerializer.Serialize(persons, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });
                        break;
                    }

                case "ALLROUTES":
                    {
                        if (user.accessLevel != AccessLevel.ADMIN)
                            throw new Exception("Access denied");

                        List<Route> routes = context.Routes
                            .AsNoTracking()
                            .ToList();

                        result = System.Text.Json.JsonSerializer.Serialize(routes, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });
                        break;
                    }

                case "ALLSCHEDULES":
                    {
                        if (user.accessLevel != AccessLevel.ADMIN && user.accessLevel != AccessLevel.DISPATCHER)
                            throw new Exception("Access denied");

                        List<Schedule> schedules = context.Schedules
                            .AsNoTracking()
                            .ToList();

                        result = System.Text.Json.JsonSerializer.Serialize(schedules, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });
                        break;
                    }

                case "ALLSTOPS":
                    {
                        if (user.accessLevel != AccessLevel.ADMIN)
                            throw new Exception("Access denied");

                        List<Stop> stops = context.Stops
                            .AsNoTracking()
                            .ToList();

                        result = System.Text.Json.JsonSerializer.Serialize(stops, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });
                        break;
                    }

                case "ALLVEHICLES":
                    {
                        if (user.accessLevel != AccessLevel.ADMIN)
                            throw new Exception("Access denied");

                        List<Vehicle> vehicles = context.Vehicles
                            .AsNoTracking()
                            .ToList();

                        result = System.Text.Json.JsonSerializer.Serialize(vehicles, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });
                        break;
                    }

                case "ALLVEHICLETYPES":
                    {
                        if (user.accessLevel != AccessLevel.ADMIN)
                            throw new Exception("Access denied");

                        List<VehicleType> vehicleTypes = context.VehicleTypes
                            .AsNoTracking()
                            .ToList();

                        result = System.Text.Json.JsonSerializer.Serialize(vehicleTypes, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });
                        break;
                    }

                case "MYROUTE":
                    {
                        if (user.accessLevel == AccessLevel.VIEWER)
                            throw new Exception("Access denied");

                        var driver = context.Persons
                            .AsNoTracking()
                            .Where(p => p.Login == user.login)
                            .Join(context.Drivers,
                                p => p.PersonId,
                                d => d.PersonId,
                                (p, d) => new { p.PersonId })
                            .SingleOrDefault();

                        if (driver == null) throw new Exception("Invalid driver");

                        DateTime now = DateTime.Now;               
                        var closestSchedule = context.Schedules
                            .AsNoTracking()
                            .Where(s => s.DriverId == driver.PersonId)
                            .Select(s => new
                            {
                                s.ScheduleId,
                                s.RouteId,
                                AbsStartDiff = Math.Abs(EF.Functions.DateDiffSecond(s.StartDate, now)),
                                AbsEndDiff = Math.Abs(EF.Functions.DateDiffSecond(s.EndDate, now))
                            })
                            .Select(s => new
                            {
                                s.ScheduleId,
                                s.RouteId,
                                MinTimeDiff = s.AbsStartDiff < s.AbsEndDiff ? s.AbsStartDiff : s.AbsEndDiff
                            })
                            .OrderBy(s => s.MinTimeDiff)
                            .FirstOrDefault();

                        if (closestSchedule == null) throw new Exception("Invalid schedule");

                        Route? route = context.Routes
                            .AsNoTracking()
                            .Where(r => r.RouteId == closestSchedule.RouteId)
                            .SingleOrDefault();

                        result = System.Text.Json.JsonSerializer.Serialize(route, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            WriteIndented = true
                        });
                        break;
                    }

                case "MYROUTE+":
                    {
                        if (user.accessLevel == AccessLevel.VIEWER)
                            throw new Exception("Access denied");

                        var driver = context.Persons
                            .AsNoTracking()
                            .Where(p => p.Login == user.login)
                            .Join(context.Drivers,
                                p => p.PersonId,
                                d => d.PersonId,
                                (p, d) => new { p.PersonId })
                            .SingleOrDefault();

                        if (driver == null) throw new Exception("Invalid driver");

                        DateTime now = DateTime.Now;
                        var routeData = context.Schedules
                            .AsNoTracking()
                            .Where(s => s.DriverId == driver.PersonId)
                            .Select(s => new
                            {
                                s.ScheduleId,
                                s.RouteId,
                                s.VehicleId,
                                s.StartDate,
                                s.EndDate,
                                AbsStartDiff = Math.Abs(EF.Functions.DateDiffSecond(s.StartDate, now)),
                                AbsEndDiff = Math.Abs(EF.Functions.DateDiffSecond(s.EndDate, now))
                            })
                            .Select(s => new
                            {
                                s.ScheduleId,
                                s.RouteId,
                                s.VehicleId,
                                s.StartDate,
                                s.EndDate,
                                MinTimeDiff = s.AbsStartDiff < s.AbsEndDiff ? s.AbsStartDiff : s.AbsEndDiff
                            })
                            .OrderBy(s => s.MinTimeDiff)
                            .Join(context.Routes,
                                s => s.RouteId,
                                r => r.RouteId,
                                (s, r) => new { s, Route = r })
                            .Join(context.Vehicles,
                                x => x.s.VehicleId,
                                v => v.VehicleId,
                                (x, v) => new { x.s, x.Route, Vehicle = v })
                            .Join(context.VehicleTypes,
                                x => x.Vehicle.VehicleTypeId,
                                vt => vt.VehicleTypeId,
                                (x, vt) => new { x.s, x.Route, x.Vehicle, VehicleType = vt })
                            .Join(context.Depots,
                                x => x.Vehicle.DepotId,
                                d => d.DepotId,
                                (x, d) => new
                                {
                                    Schedule = new
                                    {
                                        x.s.ScheduleId,
                                        x.s.RouteId,
                                        x.s.VehicleId,
                                        x.s.StartDate,
                                        x.s.EndDate
                                    },
                                    Route = x.Route,
                                    Vehicle = x.Vehicle,
                                    VehicleType = x.VehicleType,
                                    Depot = d,
                                    MinTimeDiff = x.s.MinTimeDiff
                                })
                            .OrderBy(x => x.MinTimeDiff)
                            .Select(x => new
                            {
                                x.Schedule,
                                x.Route,
                                x.Vehicle,
                                x.VehicleType,
                                x.Depot,
                                FirstStop = context.Stops
                                    .Where(rs => rs.StopId == x.Route.FirstStopId)
                                    .Join(context.Stops,
                                        rs => rs.StopId,
                                        st => st.StopId,
                                        (rs, st) => st)
                                    .FirstOrDefault(),
                                LastStop = context.Stops
                                    .Where(rs => rs.StopId == x.Route.LastStopId)
                                    .Join(context.Stops,
                                        rs => rs.StopId,
                                        st => st.StopId,
                                        (rs, st) => st)
                                    .FirstOrDefault()
                            })
                            .FirstOrDefault();

                        if (routeData == null) throw new Exception("No active route found");

                        result = System.Text.Json.JsonSerializer.Serialize(routeData, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            WriteIndented = true
                        });
                        break;
                    }

                case "MYDEPOT":
                    {
                        if (user.accessLevel == AccessLevel.VIEWER)
                            throw new Exception("Access denied");

                        var person = context.Persons
                            .AsNoTracking()
                            .Where(p => p.Login == user.login)
                            .SingleOrDefault();

                        if (person == null)
                            throw new Exception("Invalid person");

                        string? depotId = null;

                        if (user.accessLevel == AccessLevel.DRIVER)
                        {
                            var driver = context.Drivers
                                .AsNoTracking()
                                .Where(d => d.PersonId == person.PersonId)
                                .Select(d => d.DepotId)
                                .SingleOrDefault();

                            if (driver != null)
                            {
                                depotId = driver;
                            }
                        }
                        else if (user.accessLevel == AccessLevel.DISPATCHER)
                        {
                            var dispatcher = context.Dispatchers
                                .AsNoTracking()
                                .Where(d => d.PersonId == person.PersonId)
                                .Select(d => d.DepotId)
                                .SingleOrDefault();

                            if (dispatcher != null)
                            {
                                depotId = dispatcher;
                            }
                        }                        

                        if (string.IsNullOrEmpty(depotId))
                            throw new Exception("Person is not assigned a depot");

                        Depot depot = context.Depots
                            .AsNoTracking()
                            .Where(d => d.DepotId == depotId)
                            .Single();

                        if (depot == null)
                            throw new Exception("Depot not found");

                        result = System.Text.Json.JsonSerializer.Serialize(depot, new JsonSerializerOptions { WriteIndented = true });
                        break;
                    }

                case "MYDEPOTDRIVERS":
                    {
                        var userDispatcher = context.Persons
                            .AsNoTracking()
                            .Where(p => p.Login == user.login)
                            .Join(context.Dispatchers,
                                p => p.PersonId,
                                d => d.PersonId,
                                (p, d) => new { d.DepotId })
                            .SingleOrDefault();

                        if (userDispatcher == null) throw new System.Exception("User is not a dispatcher");

                        var drivers = context.Drivers
                            .AsNoTracking()
                            .Where(d => d.DepotId == userDispatcher.DepotId)
                            .Join(context.Persons,
                                d => d.PersonId,
                                p => p.PersonId,
                                (d, p) => new
                                {
                                    PersonId = d.PersonId,
                                    DepotId = d.DepotId,
                                    FirstName = p.FirstName,
                                    SurName = p.SurName,
                                    LastName = p.LastName,
                                    Position = d.Position,
                                    EmploymentDate = d.EmploymentDate,
                                    ContactNumber = p.ContactNumber,
                                    Notes = d.Notes,
                                    Login = p.Login,
                                    HashedPassword = p.HashedPassword
                                })
                            .ToList();

                        if (!drivers.Any()) throw new System.Exception("No drivers in depot");

                        result = System.Text.Json.JsonSerializer.Serialize(drivers, new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                            WriteIndented = true                        
                        });
                        break;
                    }

                case "MYDEPOTVEHICLES":
                    {
                        var userDispatcher = context.Persons
                            .AsNoTracking()
                            .Where(p => p.Login == user.login)
                            .Join(context.Dispatchers,
                                p => p.PersonId,
                                d => d.PersonId,
                                (p, d) => new { d.DepotId })
                            .SingleOrDefault();

                        if (userDispatcher == null) throw new System.Exception("User is not a dispatcher");

                        var vehicles = context.Vehicles
                            .AsNoTracking()
                            .Where(v => v.DepotId == userDispatcher.DepotId)
                            .Join(context.VehicleTypes,
                                v => v.VehicleTypeId,
                                vt => vt.VehicleTypeId,
                                (v, vt) => new
                                {
                                    VehicleId = v.VehicleId,
                                    VehicleTypeId = v.VehicleTypeId,
                                    DepotId = v.DepotId,
                                    IsActive = v.IsActive,
                                    BeginOperationYear = v.BeginOperationYear,
                                    GalleryUrl = v.GalleryUrl,
                                    Notes = v.Notes,
                                    Type = vt.Type,
                                    ManufactureCountry = vt.ManufactureCountry,
                                    ModelYear = vt.ModelYear,                                    
                                    Make = vt.Make,
                                    Model = vt.Model,
                                    EngineId = vt.EngineId,
                                    Capacity = vt.Capacity
                                })
                            .ToList();

                        if (!vehicles.Any()) throw new System.Exception("No vehicles in depot");

                        result = System.Text.Json.JsonSerializer.Serialize(vehicles, new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                            WriteIndented = true
                        });
                        break;
                    }

                case "MYDEPOTROUTES":
                    {
                        var userDispatcher = context.Persons
                            .AsNoTracking()
                            .Where(p => p.Login == user.login)
                            .Join(context.Dispatchers,
                                p => p.PersonId,
                                d => d.PersonId,
                                (p, d) => new { d.DepotId })
                            .Join(context.Depots,
                                d => d.DepotId,
                                dep => dep.DepotId,
                                (d, dep) => new { DepotType = dep.Type })
                            .SingleOrDefault();

                        if (userDispatcher == null) throw new System.Exception("User is not a dispatcher");

                        var routes = context.Routes
                            .AsNoTracking()
                            .Where(r => r.Type.Contains(userDispatcher.DepotType))
                            .Join(context.Stops,
                                r => r.FirstStopId,
                                fs => fs.StopId,
                                (r, fs) => new { Route = r, FirstStopName = fs.Name })
                            .Join(context.Stops,
                                x => x.Route.LastStopId,
                                ls => ls.StopId,
                                (x, ls) => new
                                {
                                    RouteId = x.Route.RouteId,
                                    FirstStopId = x.Route.FirstStopId,
                                    LastStopId = x.Route.LastStopId,
                                    FirstStopName = x.FirstStopName,
                                    LastStopName = ls.Name,
                                    NumberOfStops = x.Route.NumberOfStops,
                                    Length = x.Route.Length,
                                    Type = x.Route.Type,
                                    Price = x.Route.Price,
                                    Capacity = x.Route.Capacity,
                                    MapUrl = x.Route.MapUrl,
                                    Notes = x.Route.Notes,
                                })
                            .ToList();

                        if (!routes.Any()) throw new System.Exception("No routes for depot");

                        result = System.Text.Json.JsonSerializer.Serialize(routes, new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                            WriteIndented = true
                        });
                        break;
                    }

                case "FINANCIALRANGE":
                    {
                        if (user.accessLevel != AccessLevel.ADMIN)
                            throw new Exception("Access denied");

                        DateTime startDate = DateTime.Parse(messageParts[3]);
                        DateTime endDate = DateTime.Parse(messageParts[4]);

                        var financialReports = context.Routes
                            .AsNoTracking()
                            .Join(context.Schedules,
                                r => r.RouteId,
                                s => s.RouteId,
                                (r, s) => new { Route = r, Schedule = s })
                            .Join(context.Vehicles,
                                x => x.Schedule.VehicleId,
                                v => v.VehicleId,
                                (x, v) => new { x.Route, x.Schedule, Vehicle = v })
                            .Join(context.VehicleTypes,
                                x => x.Vehicle.VehicleTypeId,
                                vt => vt.VehicleTypeId,
                                (x, vt) => new { x.Route, x.Schedule, x.Vehicle, VehicleType = vt })
                            .Join(context.Engines,
                                x => x.VehicleType.EngineId,
                                e => e.EngineId,
                                (x, e) => new { x.Route, x.Schedule, x.Vehicle, x.VehicleType, Engine = e })
                            .GroupJoin(context.Validations,
                                x => x.Schedule.ScheduleId,
                                val => val.ScheduleId,
                                (x, vals) => new { x.Route, x.Schedule, x.Engine, Validations = vals })
                            .Where(x => x.Schedule.StartDate <= endDate && x.Schedule.EndDate >= startDate)
                            .GroupBy(x => new { x.Route.RouteId, x.Route.Type, x.Route.Price, x.Engine.CostOfOperationPer100km })
                            .Select(g => new
                            {
                                RouteId = g.Key.RouteId,
                                RouteType = g.Key.Type,
                                ValidationCount = g.Count(x => x.Validations.Any()),
                                TotalHours = g.Sum(x => EF.Functions.DateDiffHour(x.Schedule.StartDate, x.Schedule.EndDate)),
                                Revenue = g.Key.Price * g.Count(x => x.Validations.Any()),
                                OperatingCost = g.Key.CostOfOperationPer100km * g.Sum(x => EF.Functions.DateDiffHour(x.Schedule.StartDate, x.Schedule.EndDate)),
                                Profit = (g.Key.Price * g.Count(x => x.Validations.Any())) -
                                         (g.Key.CostOfOperationPer100km * g.Sum(x => EF.Functions.DateDiffHour(x.Schedule.StartDate, x.Schedule.EndDate)))
                            })
                            .OrderByDescending(x => x.Profit)
                            .ToList();
                        
                        result = System.Text.Json.JsonSerializer.Serialize(financialReports, new System.Text.Json.JsonSerializerOptions
                        {
                            WriteIndented = true,
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        });
                        break;
                    }

                case "LOGSRANGE":
                    {
                        if (user.accessLevel != AccessLevel.ADMIN)
                            throw new Exception("Access denied");                      

                        DateTime startDate = DateTime.Parse(messageParts[3]);
                        DateTime endDate = DateTime.Parse(messageParts[4]);

                        List<Event> events = context.Events
                            .AsNoTracking()
                            .Where(e => e.DateTime >= startDate && e.DateTime <= endDate)
                            .OrderByDescending(e => e.DateTime)
                            .ToList();

                        result = System.Text.Json.JsonSerializer.Serialize(events, new JsonSerializerOptions { WriteIndented = true });                      
                        break;
                    }

                case "MYINFO":
                    {
                        if (user.accessLevel == AccessLevel.VIEWER)
                            throw new Exception("Access denied");

                        var person = context.Persons
                            .AsNoTracking()
                            .Where(p => p.Login == user.login)
                            .SingleOrDefault();

                        if (person == null) 
                            throw new Exception("Invalid person");

                        result = System.Text.Json.JsonSerializer.Serialize(person, new JsonSerializerOptions { WriteIndented = true });
                        break;
                    }

                default:
                    throw new Exception("Invalid directive");               
            }

            return result;
        }        

        public static TableName Edit(User user, string directive)
        {            
            switch (directive)
            {
                case "VALIDATE":
                    {
                        AddValidation(user, directive);                        
                        return TableName.VALIDATIONS;
                    }

                case "LOGSPURGE":
                    {
                        DeleteLogs(user, directive);
                        return TableName.EVENTS;
                    }

                case "FINANCIALPURGE":
                    {
                        DeleteValidations(user, directive);
                        return TableName.VALIDATIONS;
                    }

                default:
                    throw new Exception("Invalid directive");
            }
        }

        public static TableName Edit(User user, string directive, string keyOrValue)
        {
            switch (directive)
            {
                case "ADDADDRESS":
                    {
                        Address newAddress = System.Text.Json.JsonSerializer.Deserialize<Address>(keyOrValue);
                        
                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                context.Addresses.Add(newAddress);
                                context.SaveChanges();

                                LogEdit(user, TableName.ADDRESSES, "INSERT", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.ADDRESSES;
                    }

                case "ADDDEPOT":
                    {
                        Depot newDepot = System.Text.Json.JsonSerializer.Deserialize<Depot>(keyOrValue);

                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                context.Depots.Add(newDepot);
                                context.SaveChanges();

                                LogEdit(user, TableName.DEPOTS, "INSERT", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.DEPOTS;
                    }

                case "ADDDISPATCHER":
                    {
                        Dispatcher newDispatcher = System.Text.Json.JsonSerializer.Deserialize<Dispatcher>(keyOrValue);

                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                context.Dispatchers.Add(newDispatcher);
                                context.SaveChanges();

                                LogEdit(user, TableName.DISPATCHERS, "INSERT", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.DISPATCHERS;
                    }

                case "ADDDRIVER":
                    {
                        Driver newDriver = System.Text.Json.JsonSerializer.Deserialize<Driver>(keyOrValue);

                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                context.Drivers.Add(newDriver);
                                context.SaveChanges();

                                LogEdit(user, TableName.DRIVERS, "INSERT", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.DRIVERS;
                    }

                case "ADDENGINE":
                    {
                        Engine newEngine = System.Text.Json.JsonSerializer.Deserialize<Engine>(keyOrValue);

                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                context.Engines.Add(newEngine);
                                context.SaveChanges();

                                LogEdit(user, TableName.ENGINES, "INSERT", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.ENGINES;
                    }

                case "ADDPERSON":
                    {
                        Person newPerson = System.Text.Json.JsonSerializer.Deserialize<Person>(keyOrValue);

                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                context.Persons.Add(newPerson);
                                context.SaveChanges();

                                LogEdit(user, TableName.PERSONS, "INSERT", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.ENGINES;
                    }

                case "ADDROUTE":
                    {
                        Route newRoute = System.Text.Json.JsonSerializer.Deserialize<Route>(keyOrValue);

                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                context.Routes.Add(newRoute);
                                context.SaveChanges();

                                LogEdit(user, TableName.ROUTES, "INSERT", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.ROUTES;
                    }

                case "ADDSCHEDULE":
                    {
                        Schedule newSchedule = System.Text.Json.JsonSerializer.Deserialize<Schedule>(keyOrValue);

                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                context.Schedules.Add(newSchedule);
                                context.SaveChanges();

                                LogEdit(user, TableName.SCHEDULE, "INSERT", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.SCHEDULE;
                    }

                case "ADDSTOP":
                    {
                        Stop newStop = System.Text.Json.JsonSerializer.Deserialize<Stop>(keyOrValue);

                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                context.Stops.Add(newStop);
                                context.SaveChanges();

                                LogEdit(user, TableName.STOPS, "INSERT", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.STOPS;
                    }

                case "ADDVEHICLE":
                    {
                        Vehicle newVehicle = System.Text.Json.JsonSerializer.Deserialize<Vehicle>(keyOrValue);

                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                context.Vehicles.Add(newVehicle);
                                context.SaveChanges();

                                LogEdit(user, TableName.VEHICLES, "INSERT", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.VEHICLES;
                    }

                case "ADDVEHICLETYPE":
                    {
                        VehicleType newVehicleType = System.Text.Json.JsonSerializer.Deserialize<VehicleType>(keyOrValue);

                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                context.VehicleTypes.Add(newVehicleType);
                                context.SaveChanges();

                                LogEdit(user, TableName.VEHICLETYPES, "INSERT", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.VEHICLETYPES;
                    }

                case "MYCONTACT":
                    {
                        EditContact(user, directive, keyOrValue);
                        return TableName.PERSONS;
                    }

                case "REMOVEADDRESS":
                    {
                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                var address = context.Addresses
                                    .FirstOrDefault(a => a.AddressId.ToString() == keyOrValue);

                                if (address == null)
                                    throw new System.Exception("Address not found");

                                context.Addresses.Remove(address);
                                context.SaveChanges();

                                LogEdit(user, TableName.ADDRESSES, "DELETE", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.ADDRESSES;
                    }

                case "REMOVEDEPOT":
                    {
                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                var depot = context.Depots
                                    .FirstOrDefault(d => d.DepotId.ToString() == keyOrValue);

                                if (depot == null)
                                    throw new System.Exception("Depot not found");

                                context.Depots.Remove(depot);
                                context.SaveChanges();

                                LogEdit(user, TableName.DEPOTS, "DELETE", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.DEPOTS;
                    }

                case "REMOVEDISPATCHER":
                    {
                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                var dispatcher = context.Dispatchers
                                    .FirstOrDefault(d => d.PersonId.ToString() == keyOrValue);
                                if (dispatcher == null)
                                    throw new System.Exception("Dispatcher not found");
                                context.Dispatchers.Remove(dispatcher);
                                context.SaveChanges();
                                LogEdit(user, TableName.DISPATCHERS, "DELETE", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.DISPATCHERS;
                    }

                case "REMOVEDRIVER":
                    {
                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                var driver = context.Drivers
                                    .FirstOrDefault(d => d.PersonId.ToString() == keyOrValue);
                                if (driver == null)
                                    throw new System.Exception("Driver not found");
                                context.Drivers.Remove(driver);
                                context.SaveChanges();
                                LogEdit(user, TableName.DRIVERS, "DELETE", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.DRIVERS;
                    }

                case "REMOVEENGINE":
                    {
                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                var engine = context.Engines
                                    .FirstOrDefault(e => e.EngineId.ToString() == keyOrValue);
                                if (engine == null)
                                    throw new System.Exception("Engine not found");
                                context.Engines.Remove(engine);
                                context.SaveChanges();
                                LogEdit(user, TableName.ENGINES, "DELETE", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.ENGINES;
                    }

                case "REMOVEPERSON":
                    {
                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                var person = context.Persons
                                    .FirstOrDefault(p => p.PersonId.ToString() == keyOrValue);
                                if (person == null)
                                    throw new System.Exception("Person not found");
                                context.Persons.Remove(person);
                                context.SaveChanges();
                                LogEdit(user, TableName.PERSONS, "DELETE", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.PERSONS;
                    }

                case "REMOVEROUTE":
                    {
                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                var route = context.Routes
                                    .FirstOrDefault(r => r.RouteId.ToString() == keyOrValue);
                                if (route == null)
                                    throw new System.Exception("Route not found");
                                context.Routes.Remove(route);
                                context.SaveChanges();
                                LogEdit(user, TableName.ROUTES, "DELETE", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.ROUTES;
                    }

                case "REMOVESCHEDULE":
                    {
                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                var schedule = context.Schedules
                                    .FirstOrDefault(s => s.ScheduleId.ToString() == keyOrValue);
                                if (schedule == null)
                                    throw new System.Exception("Schedule not found");
                                context.Schedules.Remove(schedule);
                                context.SaveChanges();
                                LogEdit(user, TableName.SCHEDULE, "DELETE", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.SCHEDULE;
                    }

                case "REMOVESTOP":
                    {
                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                var stop = context.Stops
                                    .FirstOrDefault(s => s.StopId.ToString() == keyOrValue);
                                if (stop == null)
                                    throw new System.Exception("Stop not found");
                                context.Stops.Remove(stop);
                                context.SaveChanges();
                                LogEdit(user, TableName.STOPS, "DELETE", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.STOPS;
                    }

                case "REMOVEVEHICLE":
                    {
                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                var vehicle = context.Vehicles
                                    .FirstOrDefault(v => v.VehicleId.ToString() == keyOrValue);
                                if (vehicle == null)
                                    throw new System.Exception("Vehicle not found");
                                context.Vehicles.Remove(vehicle);
                                context.SaveChanges();
                                LogEdit(user, TableName.VEHICLES, "DELETE", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.VEHICLES;
                    }

                case "REMOVEVEHICLETYPE":
                    {
                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                var vehicleType = context.VehicleTypes
                                    .FirstOrDefault(vt => vt.VehicleTypeId.ToString() == keyOrValue);
                                if (vehicleType == null)
                                    throw new System.Exception("Vehicle type not found");
                                context.VehicleTypes.Remove(vehicleType);
                                context.SaveChanges();
                                LogEdit(user, TableName.VEHICLETYPES, "DELETE", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.VEHICLETYPES;
                    }

                default:
                    throw new Exception("Invalid directive");
            }
        }

        public static TableName Edit(User user, string directive, string key, string newValue)
        {
            switch (directive)
            {
                case "UPDATEADDRESS":
                    {
                        Address newAddress = System.Text.Json.JsonSerializer.Deserialize<Address>(newValue);

                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                var existingAddress = context.Addresses                           
                                        .FirstOrDefault(a => a.AddressId.ToString() == key);

                                if (existingAddress == null)
                                    throw new System.Exception("Address not found");

                                if (existingAddress.City != newAddress.City)
                                    existingAddress.City = newAddress.City;

                                if (existingAddress.District != newAddress.District)
                                    existingAddress.District = newAddress.District;

                                if (existingAddress.Street != newAddress.Street)
                                    existingAddress.Street = newAddress.Street;

                                if (existingAddress.BuildingNo != newAddress.BuildingNo)
                                    existingAddress.BuildingNo = newAddress.BuildingNo;

                                context.Addresses.Update(existingAddress);
                                context.SaveChanges();

                                LogEdit(user, TableName.ADDRESSES, "UPDATE", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.ADDRESSES;
                    }

                case "UPDATEDEPOT":
                    {
                        Depot newDepot = System.Text.Json.JsonSerializer.Deserialize<Depot>(newValue);

                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                var existingDepot = context.Depots                                                        
                                                             .FirstOrDefault(d => d.DepotId.ToString() == key);

                                if (existingDepot == null)
                                    throw new System.Exception("Depot not found");

                                if (existingDepot.Name != newDepot.Name)
                                    existingDepot.Name = newDepot.Name;

                                if (existingDepot.Type != newDepot.Type)
                                    existingDepot.Type = newDepot.Type;

                                if (existingDepot.Operator != newDepot.Operator)
                                    existingDepot.Operator = newDepot.Operator;

                                if (existingDepot.ContactNumber != newDepot.ContactNumber)
                                    existingDepot.ContactNumber = newDepot.ContactNumber;

                                context.Depots.Update(existingDepot);
                                context.SaveChanges();

                                LogEdit(user, TableName.DEPOTS, "UPDATE", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.DEPOTS;
                    }

                case "UPDATEDISPATCHER":
                    {
                        Dispatcher newDispatcher = System.Text.Json.JsonSerializer.Deserialize<Dispatcher>(newValue);

                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                var existingDispatcher = context.Dispatchers                                                           
                                                             .FirstOrDefault(d => d.PersonId.ToString() == key);

                                if (existingDispatcher == null)
                                    throw new System.Exception("Dispatcher not found");

                                if (existingDispatcher.Position != newDispatcher.Position)
                                    existingDispatcher.Position = newDispatcher.Position;

                                if (existingDispatcher.EmploymentDate != newDispatcher.EmploymentDate)
                                    existingDispatcher.EmploymentDate = newDispatcher.EmploymentDate;

                                if (existingDispatcher.Notes != newDispatcher.Notes)
                                    existingDispatcher.Notes = newDispatcher.Notes;

                                context.Dispatchers.Update(existingDispatcher);
                                context.SaveChanges();

                                LogEdit(user, TableName.DISPATCHERS, "UPDATE", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.DISPATCHERS;
                    }

                case "UPDATEDRIVER":
                    {
                        Driver newDriver = System.Text.Json.JsonSerializer.Deserialize<Driver>(newValue);

                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                var existingDriver = context.Drivers                            
                                        .FirstOrDefault(d => d.PersonId.ToString() == key);

                                if (existingDriver == null)
                                    throw new System.Exception("Driver not found");

                                if (existingDriver.Position != newDriver.Position)
                                    existingDriver.Position = newDriver.Position;

                                if (existingDriver.EmploymentDate != newDriver.EmploymentDate)
                                    existingDriver.EmploymentDate = newDriver.EmploymentDate;

                                if (existingDriver.Notes != newDriver.Notes)
                                    existingDriver.Notes = newDriver.Notes;

                                context.Drivers.Update(existingDriver);
                                context.SaveChanges();

                                LogEdit(user, TableName.DRIVERS, "UPDATE", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.DRIVERS;
                    }

                case "UPDATEENGINE":
                    {
                        Engine newEngine = System.Text.Json.JsonSerializer.Deserialize<Engine>(newValue);

                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                var existingEngine = context.Engines                            
                                        .FirstOrDefault(e => e.EngineId.ToString() == key);

                                if (existingEngine == null)
                                    throw new System.Exception("Engine not found");

                                if (existingEngine.Make != newEngine.Make)
                                    existingEngine.Make = newEngine.Make;

                                if (existingEngine.Model != newEngine.Model)
                                    existingEngine.Model = newEngine.Model;

                                if (existingEngine.Propulsion != newEngine.Propulsion)
                                    existingEngine.Propulsion = newEngine.Propulsion;

                                if (existingEngine.CostOfOperationPer100km != newEngine.CostOfOperationPer100km)
                                    existingEngine.CostOfOperationPer100km = newEngine.CostOfOperationPer100km;

                                context.Engines.Update(existingEngine);
                                context.SaveChanges();

                                LogEdit(user, TableName.ENGINES, "UPDATE", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.ENGINES;
                    }

                case "UPDATEPERSON":
                    {
                        Person newPerson = System.Text.Json.JsonSerializer.Deserialize<Person>(newValue);

                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                var existingPerson = context.Persons                            
                                            .FirstOrDefault(p => p.PersonId.ToString() == key);

                                if (existingPerson == null)
                                    throw new System.Exception("Person not found");

                                if (existingPerson.FirstName != newPerson.FirstName)
                                    existingPerson.FirstName = newPerson.FirstName;

                                if (existingPerson.SurName != newPerson.SurName)
                                    existingPerson.SurName = newPerson.SurName;

                                if (existingPerson.LastName != newPerson.LastName)
                                    existingPerson.LastName = newPerson.LastName;

                                if (existingPerson.ContactNumber != newPerson.ContactNumber)
                                    existingPerson.ContactNumber = newPerson.ContactNumber;

                                if (existingPerson.Login != newPerson.Login)
                                    existingPerson.Login = newPerson.Login;

                                if (existingPerson.HashedPassword != newPerson.HashedPassword)
                                    existingPerson.HashedPassword = newPerson.HashedPassword;

                                context.Persons.Update(existingPerson);
                                context.SaveChanges();

                                LogEdit(user, TableName.PERSONS, "UPDATE", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }                      
                        return TableName.PERSONS;
                    }

                case "UPDATEROUTE":
                    {
                        Route newRoute = System.Text.Json.JsonSerializer.Deserialize<Route>(newValue);

                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                var existingRoute = context.Routes                                                             
                                                             .FirstOrDefault(r => r.RouteId.ToString() == key);

                                if (existingRoute == null)
                                    throw new System.Exception("Route not found");

                                if (existingRoute.NumberOfStops != newRoute.NumberOfStops)
                                    existingRoute.NumberOfStops = newRoute.NumberOfStops;

                                if (existingRoute.Length != newRoute.Length)
                                    existingRoute.Length = newRoute.Length;

                                if (existingRoute.Type != newRoute.Type)
                                    existingRoute.Type = newRoute.Type;

                                if (existingRoute.Price != newRoute.Price)
                                    existingRoute.Price = newRoute.Price;

                                if (existingRoute.Capacity != newRoute.Capacity)
                                    existingRoute.Capacity = newRoute.Capacity;

                                if (existingRoute.MapUrl != newRoute.MapUrl)
                                    existingRoute.MapUrl = newRoute.MapUrl;

                                if (existingRoute.Notes != newRoute.Notes)
                                    existingRoute.Notes = newRoute.Notes;

                                context.Routes.Update(existingRoute);
                                context.SaveChanges();

                                LogEdit(user, TableName.ROUTES, "UPDATE", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.ROUTES;
                    }

                case "UPDATESCHEDULE":
                    {
                        Schedule newSchedule = System.Text.Json.JsonSerializer.Deserialize<Schedule>(newValue);

                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                var existingSchedule = context.Schedules                             
                                    .FirstOrDefault(s => s.RouteId.ToString() == key);

                                if (existingSchedule == null)
                                    throw new System.Exception("Schedule not found");

                                if (existingSchedule.StartDate != newSchedule.StartDate)
                                    existingSchedule.StartDate = newSchedule.StartDate;

                                if (existingSchedule.EndDate != newSchedule.EndDate)
                                    existingSchedule.EndDate = newSchedule.EndDate;

                                context.Schedules.Update(existingSchedule);
                                context.SaveChanges();

                                LogEdit(user, TableName.SCHEDULE, "UPDATE", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.SCHEDULE;
                    }

                case "UPDATESTOP":
                    {
                        Stop newStop = System.Text.Json.JsonSerializer.Deserialize<Stop>(newValue);

                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                var existingStop = context.Stops                             
                                     .FirstOrDefault(s => s.StopId.ToString() == key);

                                if (existingStop == null)
                                    throw new System.Exception("Stop not found");

                                if (existingStop.District != newStop.District)
                                    existingStop.District = newStop.District;

                                if (existingStop.Name != newStop.Name)
                                    existingStop.Name = newStop.Name;

                                if (existingStop.Type != newStop.Type)
                                    existingStop.Type = newStop.Type;

                                if (existingStop.Operator != newStop.Operator)
                                    existingStop.Operator = newStop.Operator;

                                context.Stops.Update(existingStop);
                                context.SaveChanges();

                                LogEdit(user, TableName.STOPS, "UPDATE", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.STOPS;
                    }

                case "UPDATEVEHICLE":
                    {
                        Vehicle newVehicle = System.Text.Json.JsonSerializer.Deserialize<Vehicle>(newValue);

                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                var existingVehicle = context.Vehicles                             
                                        .FirstOrDefault(v => v.VehicleId.ToString() == key);

                                if (existingVehicle == null)
                                    throw new System.Exception("Vehicle not found");

                                if (existingVehicle.IsActive != newVehicle.IsActive)
                                    existingVehicle.IsActive = newVehicle.IsActive;

                                if (existingVehicle.BeginOperationYear != newVehicle.BeginOperationYear)
                                    existingVehicle.BeginOperationYear = newVehicle.BeginOperationYear;

                                if (existingVehicle.GalleryUrl != newVehicle.GalleryUrl)
                                    existingVehicle.GalleryUrl = newVehicle.GalleryUrl;

                                if (existingVehicle.Notes != newVehicle.Notes)
                                    existingVehicle.Notes = newVehicle.Notes;

                                context.Vehicles.Update(existingVehicle);
                                context.SaveChanges();

                                LogEdit(user, TableName.VEHICLES, "UPDATE", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.VEHICLES;
                    }

                case "UPDATEVEHICLETYPE":
                    {
                        VehicleType newVehicleType = System.Text.Json.JsonSerializer.Deserialize<VehicleType>(newValue);
                        
                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                var existingVehicleType = context.VehicleTypes                                   
                                     .FirstOrDefault(vt => vt.VehicleTypeId.ToString() == key);

                                if (existingVehicleType == null)
                                    throw new System.Exception("Vehicle type not found");

                                if (existingVehicleType.Type != newVehicleType.Type)
                                    existingVehicleType.Type = newVehicleType.Type;

                                if (existingVehicleType.ManufactureCountry != newVehicleType.ManufactureCountry)
                                    existingVehicleType.ManufactureCountry = newVehicleType.ManufactureCountry;

                                if (existingVehicleType.ModelYear != newVehicleType.ModelYear)
                                    existingVehicleType.ModelYear = newVehicleType.ModelYear;

                                if (existingVehicleType.Make != newVehicleType.Make)
                                    existingVehicleType.Make = newVehicleType.Make;

                                if (existingVehicleType.Model != newVehicleType.Model)
                                    existingVehicleType.Model = newVehicleType.Model;

                                if (existingVehicleType.Capacity != newVehicleType.Capacity)
                                    existingVehicleType.Capacity = newVehicleType.Capacity;

                                context.VehicleTypes.Update(existingVehicleType);
                                context.SaveChanges();

                                LogEdit(user, TableName.VEHICLETYPES, "UPDATE", directive);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                        return TableName.VEHICLETYPES;
                    }

                default:
                    throw new Exception("Invalid directive");
            }
        }

        public static string GetDatabaseWriteSpeed(User user, int writeCount)
        {
            if (user.accessLevel != AccessLevel.ADMIN) 
                throw new Exception("Access denied");

            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (writeCount <= 0)
                throw new ArgumentException("Write count must be positive", nameof(writeCount));

            var stopwatch = new Stopwatch();         

            Person person = context.Persons.FirstOrDefault(p => p.Login == user.login);
            if (person == null) 
                throw new Exception("Invalid person");

            var actionTypes = new[] { "INSERT", "UPDATE", "DELETE" };
            Random random = new Random();

            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    stopwatch.Start();
                    for (int i = 0; i < writeCount; i++)
                    {
                        LogEdit(user, TableName.NONE, actionTypes[random.Next(actionTypes.Length)], $"ТЕСТОВИЙ ЗАПИС {i + 1}");
                        context.SaveChanges();
                    }
                    stopwatch.Stop();
                    transaction.Rollback();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }

                return $"{stopwatch.ElapsedMilliseconds / 1000.0:F2}";
            }
        }

        public static string GetDatabase(User user)
        {
            if (user.accessLevel != AccessLevel.ADMIN) 
                throw new Exception("Access denied");

            var backupData = new Dictionary<string, object>
                {
                    { "Engines", context.Engines.AsNoTracking().ToList() },
                    { "Addresses", context.Addresses.AsNoTracking().ToList() },
                    { "Depots", context.Depots.AsNoTracking().ToList() },
                    { "VehicleTypes", context.VehicleTypes.AsNoTracking().ToList() },
                    { "Vehicles", context.Vehicles.AsNoTracking().ToList() },
                    { "Stops", context.Stops.AsNoTracking().ToList() },
                    { "Routes", context.Routes.AsNoTracking().ToList() },
                    { "Persons", context.Persons.AsNoTracking().ToList() },
                    { "Drivers", context.Drivers.AsNoTracking().ToList() },
                    { "Dispatchers", context.Dispatchers.AsNoTracking().ToList() },
                    { "Schedule", context.Schedules.AsNoTracking().ToList() },
                    { "Validations", context.Validations.AsNoTracking().ToList() },
                    { "Events", context.Events.AsNoTracking().ToList() }
                };

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            string json = System.Text.Json.JsonSerializer.Serialize(backupData, jsonOptions);

            return json;
        }

        public static void SetDatabase(User user, string json)
        {
            if (user.accessLevel != AccessLevel.ADMIN)
                throw new Exception("Access denied");

            var backupData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<object>>>(json);

            if (backupData == null) throw new InvalidOperationException("Invalid backup file format.");

            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    context.Validations.RemoveRange(context.Validations);
                    context.Schedules.RemoveRange(context.Schedules);
                    context.Events.RemoveRange(context.Events);
                    context.Drivers.RemoveRange(context.Drivers);
                    context.Dispatchers.RemoveRange(context.Dispatchers);
                    context.Routes.RemoveRange(context.Routes);
                    context.Vehicles.RemoveRange(context.Vehicles);
                    context.VehicleTypes.RemoveRange(context.VehicleTypes);
                    context.Depots.RemoveRange(context.Depots);
                    context.Stops.RemoveRange(context.Stops);
                    context.Persons.RemoveRange(context.Persons);
                    context.Engines.RemoveRange(context.Engines);
                    context.Addresses.RemoveRange(context.Addresses);
                    context.SaveChanges();
             
                    if (backupData.TryGetValue("Engines", out var engines))
                    {
                        List<Engine> list = System.Text.Json.JsonSerializer.Deserialize<List<Engine>>(System.Text.Json.JsonSerializer.Serialize(engines));
                        if (list != null) context.Engines.AddRange(list);
                    }
                    if (backupData.TryGetValue("Addresses", out var addresses))
                    {
                        List<Address> list = System.Text.Json.JsonSerializer.Deserialize<List<Address>>(System.Text.Json.JsonSerializer.Serialize(addresses));
                        if (list != null) context.Addresses.AddRange(list);
                    }
                    if (backupData.TryGetValue("Stops", out var stops))
                    {
                        List<Stop> list = System.Text.Json.JsonSerializer.Deserialize<List<Stop>>(System.Text.Json.JsonSerializer.Serialize(stops));
                        if (list != null) context.Stops.AddRange(list);
                    }
                    if (backupData.TryGetValue("Persons", out var persons))
                    {
                        List<Person> list = System.Text.Json.JsonSerializer.Deserialize<List<Person>>(System.Text.Json.JsonSerializer.Serialize(persons));
                        if (list != null) context.Persons.AddRange(list);
                    }
                    if (backupData.TryGetValue("Depots", out var depots))
                    {
                        List<Depot> list = System.Text.Json.JsonSerializer.Deserialize<List<Depot>>(System.Text.Json.JsonSerializer.Serialize(depots));
                        if (list != null) context.Depots.AddRange(list);
                    }
                    if (backupData.TryGetValue("VehicleTypes", out var vehicleTypes))
                    {
                        List<VehicleType> list = System.Text.Json.JsonSerializer.Deserialize<List<VehicleType>>(System.Text.Json.JsonSerializer.Serialize(vehicleTypes));
                        if (list != null) context.VehicleTypes.AddRange(list);
                    }
                    if (backupData.TryGetValue("Vehicles", out var vehicles))
                    {
                        List<Vehicle> list = System.Text.Json.JsonSerializer.Deserialize<List<Vehicle>>(System.Text.Json.JsonSerializer.Serialize(vehicles));
                        if (list != null) context.Vehicles.AddRange(list);
                    }
                    if (backupData.TryGetValue("Routes", out var routes))
                    {
                        List<Route> list = System.Text.Json.JsonSerializer.Deserialize<List<Route>>(System.Text.Json.JsonSerializer.Serialize(routes));
                        if (list != null) context.Routes.AddRange(list);
                    }
                    if (backupData.TryGetValue("Drivers", out var drivers))
                    {
                        List<Driver> list = System.Text.Json.JsonSerializer.Deserialize<List<Driver>>(System.Text.Json.JsonSerializer.Serialize(drivers));
                        if (list != null) context.Drivers.AddRange(list);
                    }
                    if (backupData.TryGetValue("Dispatchers", out var dispatchers))
                    {
                        List<Dispatcher> list = System.Text.Json.JsonSerializer.Deserialize<List<Dispatcher>>(System.Text.Json.JsonSerializer.Serialize(dispatchers));
                        if (list != null) context.Dispatchers.AddRange(list);
                    }
                    if (backupData.TryGetValue("Events", out var events))
                    {
                        List<Event> list = System.Text.Json.JsonSerializer.Deserialize<List<Event>>(System.Text.Json.JsonSerializer.Serialize(events));
                        if (list != null)
                        {
                            if (list != null)
                            {
                                foreach (Event log in list)
                                {
                                    context.Events.Add(new Event
                                    {
                                        PersonId = log.PersonId,
                                        TableName = log.TableName,
                                        ActionType = log.ActionType,                                        
                                        DateTime = log.DateTime,
                                        Query = log.Query,
                                        Person = log.Person
                                    });
                                }
                            }
                        }
                    }
                    if (backupData.TryGetValue("Schedule", out var schedule))
                    {
                        List<Schedule> list = System.Text.Json.JsonSerializer.Deserialize<List<Schedule>>(System.Text.Json.JsonSerializer.Serialize(schedule));
                        if (list != null) context.Schedules.AddRange(list);
                    }
                    if (backupData.TryGetValue("Validations", out var validations))
                    {
                        List<Validation> list = System.Text.Json.JsonSerializer.Deserialize<List<Validation>>(System.Text.Json.JsonSerializer.Serialize(validations));
                        if (list != null)
                        {
                            foreach (Validation validation in list)
                            {
                                context.Validations.Add(new Validation
                                {
                                    DateTime = validation.DateTime,
                                    ScheduleId = validation.ScheduleId,
                                    Schedule = validation.Schedule       
                                });
                            }
                        }
                    }

                    context.SaveChanges();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public static void DownloadCredentials()
        {
            DownloadCredentialsFromFile();
        }        

        public static void LogToConsole(string entry)
        {
            Console.WriteLine(entry);
        }

        public static string GetCredentialsValidity(string login, string password)
        {
            if (userCredentials.ContainsKey(login) && userCredentials[login] == PasswordHandler.GetHashedPassword(password))
            {
                return AccessLevel.ADMIN.ToString();
            }

            if (string.IsNullOrWhiteSpace(login))
            {
                return "login";
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                return "password";
            }

            var person = context.Persons
                .AsNoTracking()
                .Where(p => p.Login == login)
                .Select(p => new
                {
                    p.HashedPassword,
                    AccessLevel = p.Dispatcher != null ? AccessLevel.DISPATCHER.ToString() : p.Driver != null ? AccessLevel.DRIVER.ToString() : AccessLevel.ADMIN.ToString(),
                })
                .SingleOrDefault();

            if (person == null)
            {
                return "login";
            }
            if (!PasswordHandler.VerifyPassword(password, person.HashedPassword))
            {
                return "password";
            }

            return person.AccessLevel;
        }

        public static int GetAccessLevel(User user)
        {
            switch (user.accessLevel)
            {
                case AccessLevel.ADMIN:
                    return 3;

                case AccessLevel.DISPATCHER:
                    return 2;

                case AccessLevel.DRIVER:
                    return 1;

                case AccessLevel.VIEWER:
                default:
                    return 0;
            }
        }

        public static string GetListAsJSON(User user, string directive)
        {
            switch (directive)
            {
                case "":
                    {
                        return "";
                    }

                default:
                    return "1";
            }
        }       

        public static void SetConnectionParams(AppDbContext newContext, string newIpAddress, int newPort)
        {
            context = newContext;
            ipAddress = newIpAddress;
            port = newPort;            
        }
    }
}
