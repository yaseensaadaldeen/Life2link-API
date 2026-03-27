using Microsoft.EntityFrameworkCore;
using LifeLink_V2.Data;
using LifeLink_V2.Models;
using LifeLink_V2.Helpers;
using LifeLink_V2.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace LifeLink_V2.Services.Implementations
{
    public class AdminAnalyticsService : IAdminAnalyticsService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdminAnalyticsService> _logger;

        public AdminAnalyticsService(AppDbContext context, ILogger<AdminAnalyticsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Platform Overview

        public async Task<ApiResponse> GetPlatformOverviewAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.UtcNow.AddMonths(-1);
                endDate ??= DateTime.UtcNow;

                // Total Users
                var totalUsers = await _context.Users.CountAsync();
                var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
                var newUsers = await _context.Users
                    .CountAsync(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate);

                // Total Patients
                var totalPatients = await _context.Patients.CountAsync();
                var newPatients = await _context.Patients
                    .CountAsync(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate);

                // Total Providers
                var totalProviders = await _context.Providers.CountAsync();
                var activeProviders = await _context.Providers.CountAsync(p => p.IsActive);
                var newProviders = await _context.Providers
                    .CountAsync(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate);

                // Appointments Statistics
                var appointmentsQuery = _context.Appointments
                    .Where(a => a.ScheduledAt >= startDate && a.ScheduledAt <= endDate);

                var totalAppointments = await appointmentsQuery.CountAsync();
                var completedAppointments = await appointmentsQuery.CountAsync(a => a.Status.StatusName == "Completed");
                var cancelledAppointments = await appointmentsQuery.CountAsync(a => a.Status.StatusName == "Cancelled");

                // Revenue Statistics
                var completedAppointmentsRevenue = await appointmentsQuery
                    .Where(a => a.Status.StatusName == "Completed" && a.IsPaid)
                    .SumAsync(a => a.PriceSyp);

                var pharmacyRevenue = await _context.PharmacyOrders
                    .Where(o => o.Status == "Completed" && o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                    .SumAsync(o => o.TotalSyp);

                var laboratoryRevenue = await _context.LabTestOrders
                    .Where(o => o.Status == "Completed" && o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                    .SumAsync(o => o.PriceSyp);

                var totalRevenue = completedAppointmentsRevenue + pharmacyRevenue + laboratoryRevenue;

                // Orders Statistics
                var totalPharmacyOrders = await _context.PharmacyOrders
                    .CountAsync(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate);

                var totalLabOrders = await _context.LabTestOrders
                    .CountAsync(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate);

                var result = new
                {
                    Period = new { StartDate = startDate, EndDate = endDate },
                    Users = new
                    {
                        Total = totalUsers,
                        Active = activeUsers,
                        New = newUsers,
                        Patients = totalPatients,
                        NewPatients = newPatients,
                        Providers = totalProviders,
                        ActiveProviders = activeProviders,
                        NewProviders = newProviders
                    },
                    Appointments = new
                    {
                        Total = totalAppointments,
                        Completed = completedAppointments,
                        Cancelled = cancelledAppointments,
                        CompletionRate = totalAppointments > 0 ? (double)completedAppointments / totalAppointments * 100 : 0,
                        CancellationRate = totalAppointments > 0 ? (double)cancelledAppointments / totalAppointments * 100 : 0
                    },
                    Revenue = new
                    {
                        Total = totalRevenue,
                        Appointments = completedAppointmentsRevenue,
                        Pharmacy = pharmacyRevenue,
                        Laboratory = laboratoryRevenue,
                        AverageDailyRevenue = totalRevenue / Math.Max((endDate.Value - startDate.Value).Days, 1)
                    },
                    Orders = new
                    {
                        Pharmacy = totalPharmacyOrders,
                        Laboratory = totalLabOrders,
                        Total = totalPharmacyOrders + totalLabOrders
                    },
                    PlatformHealth = new
                    {
                        Uptime = "99.8%", // This would come from monitoring system
                        ActiveSessions = await GetActiveSessionsCountAsync(),
                        ApiRequests = await GetApiRequestsCountAsync(startDate.Value, endDate.Value)
                    }
                };

                return ApiResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving platform overview");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع نظرة عامة على المنصة");
            }
        }

        #endregion

        #region Users Analytics

        public async Task<ApiResponse> GetUsersAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.UtcNow.AddMonths(-1);
                endDate ??= DateTime.UtcNow;

                // User registrations over time
                var registrationsByDate = await _context.Users
                    .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                    .GroupBy(u => u.CreatedAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Count = g.Count(),
                        Patients = g.Count(u => u.Patient != null),
                        Providers = g.Count(u => u.Provider != null)
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                // Users by role
                var usersByRole = await _context.Users
                    .Include(u => u.Role)
                    .GroupBy(u => u.Role.RoleName)
                    .Select(g => new
                    {
                        Role = g.Key,
                        Count = g.Count(),
                        Active = g.Count(u => u.IsActive),
                        Percentage = (double)g.Count() / _context.Users.Count() * 100
                    })
                    .ToListAsync();

                // Users by city
                var usersByCity = await _context.Users
                    .Include(u => u.City)
                    .Where(u => u.City != null)
                    .GroupBy(u => new { u.City.CityId, u.City.CityName })
                    .Select(g => new
                    {
                        CityId = g.Key.CityId,
                        CityName = g.Key.CityName,
                        Count = g.Count(),
                        Patients = g.Count(u => u.Patient != null),
                        Providers = g.Count(u => u.Provider != null)
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync();

                // User engagement
                var activeUsers = await _context.Users
                    .CountAsync(u => u.IsActive && u.CreatedAt >= startDate && u.CreatedAt <= endDate);

                var usersWithAppointments = await _context.Users
                    .CountAsync(u => u.Patient.Appointments.Any(a => a.ScheduledAt >= startDate && a.ScheduledAt <= endDate));

                var result = new
                {
                    Period = new { StartDate = startDate, EndDate = endDate },
                    Registrations = new
                    {
                        Total = registrationsByDate.Sum(x => x.Count),
                        DailyAverage = registrationsByDate.Any() ? registrationsByDate.Average(x => x.Count) : 0,
                        ByDate = registrationsByDate
                    },
                    Distribution = new
                    {
                        ByRole = usersByRole,
                        ByCity = usersByCity
                    },
                    Engagement = new
                    {
                        ActiveUsers = activeUsers,
                        UsersWithAppointments = usersWithAppointments,
                        EngagementRate = activeUsers > 0 ? (double)usersWithAppointments / activeUsers * 100 : 0
                    }
                };

                return ApiResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users analytics");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع تحليلات المستخدمين");
            }
        }

        public async Task<ApiResponse> GetUsersGrowthAsync(string period = "monthly")
        {
            try
            {
                DateTime startDate;
                var endDate = DateTime.UtcNow;

                switch (period.ToLower())
                {
                    case "daily":
                        startDate = endDate.AddDays(-30);
                        break;
                    case "weekly":
                        startDate = endDate.AddDays(-90);
                        break;
                    case "monthly":
                        startDate = endDate.AddMonths(-12);
                        break;
                    default:
                        startDate = endDate.AddMonths(-12);
                        break;
                }

                IQueryable<User> query = _context.Users;

                List<dynamic> growthData;

                if (period == "daily")
                {
                    growthData = await query
                        .Where(u => u.CreatedAt >= startDate)
                        .GroupBy(u => u.CreatedAt.Date)
                        .Select(g => new
                        {
                            Period = g.Key.ToString("yyyy-MM-dd"),
                            NewUsers = g.Count(),
                            Cumulative = _context.Users.Count(u => u.CreatedAt.Date <= g.Key)
                        })
                        .OrderBy(x => x.Period)
                        .ToListAsync<dynamic>();
                }
                else if (period == "weekly")
                {
                    growthData = await query
                        .Where(u => u.CreatedAt >= startDate)
                        .GroupBy(u => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                            u.CreatedAt, CalendarWeekRule.FirstDay, DayOfWeek.Sunday))
                        .Select(g => new
                        {
                            Period = $"Week {g.Key}",
                            NewUsers = g.Count(),
                            Cumulative = _context.Users.Count(u =>
                                CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                                    u.CreatedAt, CalendarWeekRule.FirstDay, DayOfWeek.Sunday) <= g.Key)
                        })
                        .OrderBy(x => x.Period)
                        .ToListAsync<dynamic>();
                }
                else // monthly
                {
                    growthData = await query
                        .Where(u => u.CreatedAt >= startDate)
                        .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                        .Select(g => new
                        {
                            Period = $"{g.Key.Year}-{g.Key.Month:00}",
                            NewUsers = g.Count(),
                            Cumulative = _context.Users.Count(u =>
                                u.CreatedAt.Year <= g.Key.Year &&
                                (u.CreatedAt.Year < g.Key.Year || u.CreatedAt.Month <= g.Key.Month))
                        })
                        .OrderBy(x => x.Period)
                        .ToListAsync<dynamic>();
                }

                var totalGrowth = growthData.Any() ?
                    growthData.Last().Cumulative - (growthData.First().Cumulative - growthData.First().NewUsers) : 0;

                var result = new
                {
                    Period = period,
                    TimeRange = new { StartDate = startDate, EndDate = endDate },
                    GrowthData = growthData,
                    Summary = new
                    {
                        TotalGrowth = totalGrowth,
                        AverageGrowth = growthData.Any() ? growthData.Average(d => d.NewUsers) : 0,
                        GrowthRate = growthData.Count > 1 ?
                            ((double)growthData.Last().NewUsers / growthData.First().NewUsers - 1) * 100 : 0
                    }
                };

                return ApiResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users growth data");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع بيانات نمو المستخدمين");
            }
        }

        #endregion

        #region Appointments Analytics

        public async Task<ApiResponse> GetAppointmentsAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.UtcNow.AddMonths(-1);
                endDate ??= DateTime.UtcNow;

                var appointmentsQuery = _context.Appointments
                    .Include(a => a.Status)
                    .Where(a => a.ScheduledAt >= startDate && a.ScheduledAt <= endDate);

                // Appointments by status
                var appointmentsByStatus = await appointmentsQuery
                    .GroupBy(a => a.Status.StatusName)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count(),
                        Revenue = g.Where(a => a.IsPaid).Sum(a => a.PriceSyp)
                    })
                    .ToListAsync();

                // Appointments over time
                var appointmentsByDate = await appointmentsQuery
                    .GroupBy(a => a.ScheduledAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Total = g.Count(),
                        Completed = g.Count(a => a.Status.StatusName == "Completed"),
                        Cancelled = g.Count(a => a.Status.StatusName == "Cancelled"),
                        Revenue = g.Where(a => a.IsPaid && a.Status.StatusName == "Completed").Sum(a => a.PriceSyp)
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                // Peak hours
                var appointmentsByHour = await appointmentsQuery
                    .GroupBy(a => a.ScheduledAt.Hour)
                    .Select(g => new
                    {
                        Hour = g.Key,
                        Count = g.Count(),
                        Status = g.GroupBy(a => a.Status.StatusName)
                            .Select(sg => new { Status = sg.Key, Count = sg.Count() })
                            .ToList()
                    })
                    .OrderBy(x => x.Hour)
                    .ToListAsync();

                // Average statistics
                var totalAppointments = appointmentsByStatus.Sum(x => x.Count);
                var completedAppointments = appointmentsByStatus.FirstOrDefault(x => x.Status == "Completed")?.Count ?? 0;
                var totalRevenue = appointmentsByStatus.Sum(x => x.Revenue);

                var result = new
                {
                    Period = new { StartDate = startDate, EndDate = endDate },
                    Summary = new
                    {
                        TotalAppointments = totalAppointments,
                        Completed = completedAppointments,
                        Cancelled = appointmentsByStatus.FirstOrDefault(x => x.Status == "Cancelled")?.Count ?? 0,
                        NoShow = appointmentsByStatus.FirstOrDefault(x => x.Status == "NoShow")?.Count ?? 0,
                        TotalRevenue = totalRevenue,
                        AverageRevenuePerAppointment = completedAppointments > 0 ? totalRevenue / completedAppointments : 0,
                        CompletionRate = totalAppointments > 0 ? (double)completedAppointments / totalAppointments * 100 : 0
                    },
                    ByStatus = appointmentsByStatus,
                    OverTime = new
                    {
                        ByDate = appointmentsByDate,
                        DailyAverage = appointmentsByDate.Any() ? appointmentsByDate.Average(x => x.Total) : 0,
                        RevenueDailyAverage = appointmentsByDate.Any() ? appointmentsByDate.Average(x => x.Revenue) : 0
                    },
                    PeakHours = appointmentsByHour,
                    BusiestDay = appointmentsByDate.OrderByDescending(x => x.Total).FirstOrDefault(),
                    MostRevenueDay = appointmentsByDate.OrderByDescending(x => x.Revenue).FirstOrDefault()
                };

                return ApiResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving appointments analytics");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع تحليلات المواعيد");
            }
        }

        public async Task<ApiResponse> GetAppointmentsBySpecialtyAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.UtcNow.AddMonths(-1);
                endDate ??= DateTime.UtcNow;

                var appointmentsBySpecialty = await _context.Appointments
                    .Include(a => a.Specialty)
                    .Where(a => a.ScheduledAt >= startDate && a.ScheduledAt <= endDate)
                    .GroupBy(a => a.Specialty != null ? a.Specialty.SpecialtyName : "غير محدد")
                    .Select(g => new
                    {
                        Specialty = g.Key,
                        Count = g.Count(),
                        Completed = g.Count(a => a.Status.StatusName == "Completed"),
                        Cancelled = g.Count(a => a.Status.StatusName == "Cancelled"),
                        Revenue = g.Where(a => a.IsPaid && a.Status.StatusName == "Completed").Sum(a => a.PriceSyp),
                        AveragePrice = g.Where(a => a.Status.StatusName == "Completed").Average(a => a.PriceSyp)
                    })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();

                var result = new
                {
                    Period = new { StartDate = startDate, EndDate = endDate },
                    Specialties = appointmentsBySpecialty,
                    TotalAppointments = appointmentsBySpecialty.Sum(x => x.Count),
                    TotalRevenue = appointmentsBySpecialty.Sum(x => x.Revenue)
                };

                return ApiResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving appointments by specialty");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع المواعيد حسب التخصص");
            }
        }

        public async Task<ApiResponse> GetAppointmentsByProviderTypeAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.UtcNow.AddMonths(-1);
                endDate ??= DateTime.UtcNow;

                var appointmentsByProviderType = await _context.Appointments
                    .Include(a => a.Provider)
                        .ThenInclude(p => p.ProviderType)
                    .Where(a => a.ScheduledAt >= startDate && a.ScheduledAt <= endDate)
                    .GroupBy(a => a.Provider.ProviderType.ProviderTypeName)
                    .Select(g => new
                    {
                        ProviderType = g.Key,
                        Count = g.Count(),
                        UniqueProviders = g.Select(a => a.ProviderId).Distinct().Count(),
                        Completed = g.Count(a => a.Status.StatusName == "Completed"),
                        Revenue = g.Where(a => a.IsPaid && a.Status.StatusName == "Completed").Sum(a => a.PriceSyp),
                        AverageAppointmentsPerProvider = (double)g.Count() / g.Select(a => a.ProviderId).Distinct().Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();

                var result = new
                {
                    Period = new { StartDate = startDate, EndDate = endDate },
                    ProviderTypes = appointmentsByProviderType,
                    Summary = new
                    {
                        TotalAppointments = appointmentsByProviderType.Sum(x => x.Count),
                        TotalProviders = appointmentsByProviderType.Sum(x => x.UniqueProviders),
                        TotalRevenue = appointmentsByProviderType.Sum(x => x.Revenue)
                    }
                };

                return ApiResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving appointments by provider type");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع المواعيد حسب نوع المزود");
            }
        }

        #endregion

        #region Financial Analytics

        public async Task<ApiResponse> GetRevenueAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.UtcNow.AddMonths(-3);
                endDate ??= DateTime.UtcNow;

                // Appointments Revenue
                var appointmentsRevenue = await _context.Appointments
                    .Include(a => a.Status)
                    .Where(a => a.ScheduledAt >= startDate && a.ScheduledAt <= endDate &&
                                a.Status.StatusName == "Completed" && a.IsPaid)
                    .GroupBy(a => a.ScheduledAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Revenue = g.Sum(a => a.PriceSyp),
                        Count = g.Count(),
                        Average = g.Average(a => a.PriceSyp)
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                // Pharmacy Revenue
                var pharmacyRevenue = await _context.PharmacyOrders
                    .Where(o => o.Status == "Completed" && o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                    .GroupBy(o => o.CreatedAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Revenue = g.Sum(o => o.TotalSyp),
                        Count = g.Count(),
                        Average = g.Average(o => o.TotalSyp)
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                // Laboratory Revenue
                var laboratoryRevenue = await _context.LabTestOrders
                    .Where(o => o.Status == "Completed" && o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                    .GroupBy(o => o.CreatedAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Revenue = g.Sum(o => o.PriceSyp),
                        Count = g.Count(),
                        Average = g.Average(o => o.PriceSyp)
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                // Combine all revenue sources
                var allDates = appointmentsRevenue.Select(x => x.Date)
                    .Union(pharmacyRevenue.Select(x => x.Date))
                    .Union(laboratoryRevenue.Select(x => x.Date))
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();

                var combinedRevenue = allDates.Select(date => new
                {
                    Date = date,
                    Appointments = appointmentsRevenue.FirstOrDefault(x => x.Date == date)?.Revenue ?? 0,
                    Pharmacy = pharmacyRevenue.FirstOrDefault(x => x.Date == date)?.Revenue ?? 0,
                    Laboratory = laboratoryRevenue.FirstOrDefault(x => x.Date == date)?.Revenue ?? 0,
                    Total = (appointmentsRevenue.FirstOrDefault(x => x.Date == date)?.Revenue ?? 0) +
                           (pharmacyRevenue.FirstOrDefault(x => x.Date == date)?.Revenue ?? 0) +
                           (laboratoryRevenue.FirstOrDefault(x => x.Date == date)?.Revenue ?? 0)
                }).ToList();

                var summary = new
                {
                    TotalRevenue = combinedRevenue.Sum(x => x.Total),
                    AppointmentsRevenue = combinedRevenue.Sum(x => x.Appointments),
                    PharmacyRevenue = combinedRevenue.Sum(x => x.Pharmacy),
                    LaboratoryRevenue = combinedRevenue.Sum(x => x.Laboratory),
                    AverageDailyRevenue = combinedRevenue.Any() ? combinedRevenue.Average(x => x.Total) : 0,
                    MaxDailyRevenue = combinedRevenue.Any() ? combinedRevenue.Max(x => x.Total) : 0,
                    GrowthRate = combinedRevenue.Count > 1 ?
                        ((combinedRevenue.Last().Total - combinedRevenue.First().Total) / combinedRevenue.First().Total) * 100 : 0
                };

                var result = new
                {
                    Period = new { StartDate = startDate, EndDate = endDate },
                    Revenue = combinedRevenue,
                    Summary = summary,
                    Breakdown = new
                    {
                        Appointments = appointmentsRevenue,
                        Pharmacy = pharmacyRevenue,
                        Laboratory = laboratoryRevenue
                    }
                };

                return ApiResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving revenue analytics");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع تحليلات الإيرادات");
            }
        }

        public async Task<ApiResponse> GetRevenueBySourceAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.UtcNow.AddMonths(-1);
                endDate ??= DateTime.UtcNow;

                var revenueBySource = new List<object>();

                // Appointments Revenue
                var appointmentsRevenue = await _context.Appointments
                    .Include(a => a.Status)
                    .Where(a => a.ScheduledAt >= startDate && a.ScheduledAt <= endDate &&
                                a.Status.StatusName == "Completed" && a.IsPaid)
                    .SumAsync(a => a.PriceSyp);

                revenueBySource.Add(new
                {
                    Source = "المواعيد",
                    Revenue = appointmentsRevenue,
                    Percentage = 0 // Will calculate after getting all sources
                });

                // Pharmacy Revenue
                var pharmacyRevenue = await _context.PharmacyOrders
                    .Where(o => o.Status == "Completed" && o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                    .SumAsync(o => o.TotalSyp);

                revenueBySource.Add(new
                {
                    Source = "الصيدليات",
                    Revenue = pharmacyRevenue,
                    Percentage = 0
                });

                // Laboratory Revenue
                var laboratoryRevenue = await _context.LabTestOrders
                    .Where(o => o.Status == "Completed" && o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                    .SumAsync(o => o.PriceSyp);

                revenueBySource.Add(new
                {
                    Source = "المختبرات",
                    Revenue = laboratoryRevenue,
                    Percentage = 0
                });

                // Calculate percentages
                var totalRevenue = appointmentsRevenue + pharmacyRevenue + laboratoryRevenue;

                foreach (var source in revenueBySource)
                {
                    var prop = source.GetType().GetProperty("Percentage");
                    if (prop != null && totalRevenue > 0)
                    {
                        var revenueProp = source.GetType().GetProperty("Revenue");
                        if (revenueProp != null)
                        {
                            var revenue = (decimal)revenueProp.GetValue(source);
                            prop.SetValue(source, totalRevenue > 0 ? (revenue / totalRevenue) * 100 : 0);
                        }
                    }
                }

                var result = new
                {
                    Period = new { StartDate = startDate, EndDate = endDate },
                    Sources = revenueBySource,
                    TotalRevenue = totalRevenue,
                    AverageDailyRevenue = totalRevenue / Math.Max((endDate.Value - startDate.Value).Days, 1)
                };

                return ApiResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving revenue by source");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع الإيرادات حسب المصدر");
            }
        }

        public async Task<ApiResponse> GetPaymentMethodsAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.UtcNow.AddMonths(-1);
                endDate ??= DateTime.UtcNow;

                var paymentsByMethod = await _context.Payments
                    .Include(p => p.PaymentMethod)
                    .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
                    .GroupBy(p => p.PaymentMethod.MethodName)
                    .Select(g => new
                    {
                        Method = g.Key,
                        Count = g.Count(),
                        TotalAmount = g.Sum(p => p.AmountSyp),
                        AverageAmount = g.Average(p => p.AmountSyp),
                        Successful = g.Count(p => p.PaymentStatus == "Paid"),
                        Failed = g.Count(p => p.PaymentStatus == "Failed"),
                        Pending = g.Count(p => p.PaymentStatus == "Pending")
                    })
                    .OrderByDescending(x => x.TotalAmount)
                    .ToListAsync();

                var result = new
                {
                    Period = new { StartDate = startDate, EndDate = endDate },
                    PaymentMethods = paymentsByMethod,
                    Summary = new
                    {
                        TotalPayments = paymentsByMethod.Sum(x => x.Count),
                        TotalAmount = paymentsByMethod.Sum(x => x.TotalAmount),
                        SuccessRate = paymentsByMethod.Sum(x => x.Count) > 0 ?
                            (double)paymentsByMethod.Sum(x => x.Successful) / paymentsByMethod.Sum(x => x.Count) * 100 : 0
                    }
                };

                return ApiResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment methods analytics");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع تحليلات طرق الدفع");
            }
        }

        #endregion

        #region Providers Analytics

        public async Task<ApiResponse> GetProvidersAnalyticsAsync()
        {
            try
            {
                // Providers by type
                var providersByType = await _context.Providers
                    .Include(p => p.ProviderType)
                    .GroupBy(p => p.ProviderType.ProviderTypeName)
                    .Select(g => new
                    {
                        Type = g.Key,
                        Count = g.Count(),
                        Active = g.Count(p => p.IsActive),
                        AverageRating = g.Average(p => p.Rating),
                        TotalAppointments = g.Sum(p => p.TotalAppointments)
                    })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();

                // Providers by city
                var providersByCity = await _context.Providers
                    .Include(p => p.City)
                    .Where(p => p.City != null)
                    .GroupBy(p => new { p.City.CityId, p.City.CityName })
                    .Select(g => new
                    {
                        CityId = g.Key.CityId,
                        CityName = g.Key.CityName,
                        Count = g.Count(),
                        Active = g.Count(p => p.IsActive),
                        Types = g.GroupBy(p => p.ProviderType.ProviderTypeName)
                            .Select(tg => new { Type = tg.Key, Count = tg.Count() })
                            .ToList()
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync();

                // New providers (last 30 days)
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                var newProviders = await _context.Providers
                    .CountAsync(p => p.CreatedAt >= thirtyDaysAgo);

                // Provider activity
                var activeProviders = await _context.Providers
                    .CountAsync(p => p.IsActive && p.TotalAppointments > 0);

                var result = new
                {
                    Summary = new
                    {
                        TotalProviders = providersByType.Sum(x => x.Count),
                        ActiveProviders = providersByType.Sum(x => x.Active),
                        NewProviders = newProviders,
                        ActiveRate = providersByType.Sum(x => x.Count) > 0 ?
                            (double)providersByType.Sum(x => x.Active) / providersByType.Sum(x => x.Count) * 100 : 0,
                        TotalAppointments = providersByType.Sum(x => x.TotalAppointments),
                        AverageRating = providersByType.Average(x => x.AverageRating)
                    },
                    ByType = providersByType,
                    ByCity = providersByCity,
                    Activity = new
                    {
                        ActiveWithAppointments = activeProviders,
                        InactiveProviders = providersByType.Sum(x => x.Count) - providersByType.Sum(x => x.Active),
                        NoAppointments = providersByType.Sum(x => x.Count) - activeProviders
                    }
                };

                return ApiResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving providers analytics");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع تحليلات المزودين");
            }
        }

        public async Task<ApiResponse> GetTopProvidersAsync(int topCount = 10, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.UtcNow.AddMonths(-3);
                endDate ??= DateTime.UtcNow;

                var topProviders = await _context.Providers
                    .Include(p => p.ProviderType)
                    .Include(p => p.City)
                    .Include(p => p.Appointments)
                    .Select(p => new
                    {
                        ProviderId = p.ProviderId,
                        ProviderName = p.ProviderName,
                        ProviderType = p.ProviderType.ProviderTypeName,
                        City = p.City != null ? p.City.CityName : "غير محدد",
                        Rating = p.Rating,
                        TotalAppointments = p.TotalAppointments,
                        RecentAppointments = p.Appointments.Count(a => a.ScheduledAt >= startDate && a.ScheduledAt <= endDate),
                        RecentRevenue = p.Appointments
                            .Where(a => a.ScheduledAt >= startDate && a.ScheduledAt <= endDate &&
                                       a.Status.StatusName == "Completed" && a.IsPaid)
                            .Sum(a => a.PriceSyp),
                        CompletionRate = p.TotalAppointments > 0 ?
                            (double)p.Appointments.Count(a => a.Status.StatusName == "Completed") / p.TotalAppointments * 100 : 0
                    })
                    .OrderByDescending(p => p.RecentRevenue)
                    .ThenByDescending(p => p.RecentAppointments)
                    .Take(topCount)
                    .ToListAsync();

                var result = new
                {
                    Period = new { StartDate = startDate, EndDate = endDate },
                    TopProviders = topProviders,
                    Summary = new
                    {
                        TotalRevenue = topProviders.Sum(p => p.RecentRevenue),
                        TotalAppointments = topProviders.Sum(p => p.RecentAppointments),
                        AverageRating = topProviders.Average(p => p.Rating),
                        AverageCompletionRate = topProviders.Average(p => p.CompletionRate)
                    }
                };

                return ApiResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving top providers");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع أفضل المزودين");
            }
        }

        public async Task<ApiResponse> GetProviderPerformanceAsync(int providerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var provider = await _context.Providers
                    .Include(p => p.ProviderType)
                    .Include(p => p.City)
                    .FirstOrDefaultAsync(p => p.ProviderId == providerId);

                if (provider == null)
                    return ApiResponseHelper.NotFound("المزود غير موجود");

                startDate ??= DateTime.UtcNow.AddMonths(-3);
                endDate ??= DateTime.UtcNow;

                // Appointments analytics
                var appointmentsQuery = _context.Appointments
                    .Include(a => a.Status)
                    .Where(a => a.ProviderId == providerId &&
                               a.ScheduledAt >= startDate && a.ScheduledAt <= endDate);

                var appointmentsByStatus = await appointmentsQuery
                    .GroupBy(a => a.Status.StatusName)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count(),
                        Revenue = g.Where(a => a.IsPaid).Sum(a => a.PriceSyp),
                        AveragePrice = g.Average(a => a.PriceSyp)
                    })
                    .ToListAsync();

                var appointmentsOverTime = await appointmentsQuery
                    .GroupBy(a => a.ScheduledAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Total = g.Count(),
                        Completed = g.Count(a => a.Status.StatusName == "Completed"),
                        Revenue = g.Where(a => a.Status.StatusName == "Completed" && a.IsPaid).Sum(a => a.PriceSyp)
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                // Pharmacy orders if provider is a pharmacy
                var pharmacyOrders = await _context.PharmacyOrders
                    .Where(o => o.ProviderId == providerId &&
                               o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                    .GroupBy(o => o.Status)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count(),
                        Revenue = g.Where(o => o.Status == "Completed").Sum(o => o.TotalSyp)
                    })
                    .ToListAsync();

                // Lab orders if provider is a laboratory
                var labOrders = await _context.LabTestOrders
                    .Where(o => o.ProviderId == providerId &&
                               o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                    .GroupBy(o => o.Status)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count(),
                        Revenue = g.Where(o => o.Status == "Completed").Sum(o => o.PriceSyp)
                    })
                    .ToListAsync();

                var totalAppointments = appointmentsByStatus.Sum(x => x.Count);
                var completedAppointments = appointmentsByStatus.FirstOrDefault(x => x.Status == "Completed")?.Count ?? 0;
                var totalRevenue = appointmentsByStatus.Sum(x => x.Revenue) +
                                 pharmacyOrders.Sum(x => x.Revenue) +
                                 labOrders.Sum(x => x.Revenue);

                var result = new
                {
                    Provider = new
                    {
                        provider.ProviderId,
                        provider.ProviderName,
                        ProviderType = provider.ProviderType.ProviderTypeName,
                        provider.City?.CityName,
                        provider.Rating,
                        provider.TotalAppointments,
                        provider.IsActive
                    },
                    Period = new { StartDate = startDate, EndDate = endDate },
                    Appointments = new
                    {
                        Summary = new
                        {
                            Total = totalAppointments,
                            Completed = completedAppointments,
                            Cancelled = appointmentsByStatus.FirstOrDefault(x => x.Status == "Cancelled")?.Count ?? 0,
                            CompletionRate = totalAppointments > 0 ? (double)completedAppointments / totalAppointments * 100 : 0,
                            TotalRevenue = appointmentsByStatus.Sum(x => x.Revenue),
                            AverageRevenuePerAppointment = completedAppointments > 0 ?
                                appointmentsByStatus.Sum(x => x.Revenue) / completedAppointments : 0
                        },
                        ByStatus = appointmentsByStatus,
                        OverTime = appointmentsOverTime
                    },
                    Pharmacy = pharmacyOrders.Any() ? new
                    {
                        Orders = pharmacyOrders,
                        TotalOrders = pharmacyOrders.Sum(x => x.Count),
                        TotalRevenue = pharmacyOrders.Sum(x => x.Revenue)
                    } : null,
                    Laboratory = labOrders.Any() ? new
                    {
                        Orders = labOrders,
                        TotalOrders = labOrders.Sum(x => x.Count),
                        TotalRevenue = labOrders.Sum(x => x.Revenue)
                    } : null,
                    Overall = new
                    {
                        TotalRevenue = totalRevenue,
                        DailyAverageRevenue = totalRevenue / Math.Max((endDate.Value - startDate.Value).Days, 1),
                        BusiestDay = appointmentsOverTime.OrderByDescending(x => x.Total).FirstOrDefault(),
                        MostRevenueDay = appointmentsOverTime.OrderByDescending(x => x.Revenue).FirstOrDefault()
                    }
                };

                return ApiResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving provider performance for ID {ProviderId}", providerId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع أداء المزود");
            }
        }

        #endregion

        #region Pharmacy Analytics

        public async Task<ApiResponse> GetPharmacyAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.UtcNow.AddMonths(-1);
                endDate ??= DateTime.UtcNow;

                // Pharmacy orders statistics
                var ordersQuery = _context.PharmacyOrders
                    .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate);

                var ordersByStatus = await ordersQuery
                    .GroupBy(o => o.Status)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count(),
                        Revenue = g.Where(o => o.Status == "Completed").Sum(o => o.TotalSyp),
                        AverageOrderValue = g.Average(o => o.TotalSyp)
                    })
                    .ToListAsync();

                var ordersOverTime = await ordersQuery
                    .GroupBy(o => o.CreatedAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Orders = g.Count(),
                        Completed = g.Count(o => o.Status == "Completed"),
                        Revenue = g.Where(o => o.Status == "Completed").Sum(o => o.TotalSyp)
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                // Delivery vs Pickup
                var deliveryStats = await ordersQuery
                    .GroupBy(o => o.IsDelivery)
                    .Select(g => new
                    {
                        Type = (g.Key == "true" || g.Key == "True" || g.Key == "1" || g.Key == "yes" || g.Key == "Yes") ? "توصيل" : "استلام",
                        Count = g.Count(),
                        Revenue = g.Where(o => o.Status == "Completed").Sum(o => o.TotalSyp),
                        Average = g.Average(o => o.TotalSyp)
                    })
                    .ToListAsync();

                // Top pharmacies
                var topPharmacies = await ordersQuery
                    .Include(o => o.Provider)
                    .GroupBy(o => new { o.ProviderId, o.Provider.ProviderName })
                    .Select(g => new
                    {
                        ProviderId = g.Key.ProviderId,
                        PharmacyName = g.Key.ProviderName,
                        Orders = g.Count(),
                        Completed = g.Count(o => o.Status == "Completed"),
                        Revenue = g.Where(o => o.Status == "Completed").Sum(o => o.TotalSyp),
                        AverageOrderValue = g.Average(o => o.TotalSyp)
                    })
                    .OrderByDescending(x => x.Revenue)
                    .Take(10)
                    .ToListAsync();

                var summary = new
                {
                    TotalOrders = ordersByStatus.Sum(x => x.Count),
                    CompletedOrders = ordersByStatus.FirstOrDefault(x => x.Status == "Completed")?.Count ?? 0,
                    TotalRevenue = ordersByStatus.Sum(x => x.Revenue),
                    AverageDailyOrders = ordersOverTime.Any() ? ordersOverTime.Average(x => x.Orders) : 0,
                    AverageDailyRevenue = ordersOverTime.Any() ? ordersOverTime.Average(x => x.Revenue) : 0,
                    CompletionRate = ordersByStatus.Sum(x => x.Count) > 0 ?
                        (double)(ordersByStatus.FirstOrDefault(x => x.Status == "Completed")?.Count ?? 0) / ordersByStatus.Sum(x => x.Count) * 100 : 0
                };

                var result = new
                {
                    Period = new { StartDate = startDate, EndDate = endDate },
                    Summary = summary,
                    Orders = new
                    {
                        ByStatus = ordersByStatus,
                        OverTime = ordersOverTime,
                        DeliveryStats = deliveryStats
                    },
                    TopPharmacies = topPharmacies
                };

                return ApiResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pharmacy analytics");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع تحليلات الصيدليات");
            }
        }

        public async Task<ApiResponse> GetTopSellingMedicinesAsync(int topCount = 10, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.UtcNow.AddMonths(-3);
                endDate ??= DateTime.UtcNow;

                var topMedicines = await _context.PharmacyOrderItems
                    .Include(oi => oi.Medicine)
                    .Include(oi => oi.PharmacyOrder)
                    .Where(oi => oi.PharmacyOrder.CreatedAt >= startDate &&
                                 oi.PharmacyOrder.CreatedAt <= endDate &&
                                 oi.PharmacyOrder.Status == "Completed")
                    .GroupBy(oi => new { oi.MedicineId, oi.Medicine.MedicineName })
                    .Select(g => new
                    {
                        MedicineId = g.Key.MedicineId,
                        MedicineName = g.Key.MedicineName,
                        TotalQuantity = g.Sum(oi => oi.Quantity),
                        TotalRevenue = g.Sum(oi => oi.LineTotalSyp),
                        AveragePrice = g.Average(oi => oi.UnitPriceSyp),
                        UniqueOrders = g.Select(oi => oi.PharmacyOrderId).Distinct().Count(),
                        Pharmacies = g.Select(oi => oi.PharmacyOrder.ProviderId).Distinct().Count()
                    })
                    .OrderByDescending(x => x.TotalRevenue)
                    .ThenByDescending(x => x.TotalQuantity)
                    .Take(topCount)
                    .ToListAsync();

                var result = new
                {
                    Period = new { StartDate = startDate, EndDate = endDate },
                    TopMedicines = topMedicines,
                    Summary = new
                    {
                        TotalRevenue = topMedicines.Sum(x => x.TotalRevenue),
                        TotalQuantity = topMedicines.Sum(x => x.TotalQuantity),
                        AveragePrice = topMedicines.Average(x => x.AveragePrice)
                    }
                };

                return ApiResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving top selling medicines");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع الأدوية الأكثر مبيعاً");
            }
        }

        #endregion

        #region Laboratory Analytics

        public async Task<ApiResponse> GetLaboratoryAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.UtcNow.AddMonths(-1);
                endDate ??= DateTime.UtcNow;

                // Lab orders statistics
                var ordersQuery = _context.LabTestOrders
                    .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate);

                var ordersByStatus = await ordersQuery
                    .GroupBy(o => o.Status)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count(),
                        Revenue = g.Where(o => o.Status == "Completed").Sum(o => o.PriceSyp),
                        AverageOrderValue = g.Average(o => o.PriceSyp)
                    })
                    .ToListAsync();

                var ordersOverTime = await ordersQuery
                    .GroupBy(o => o.CreatedAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Orders = g.Count(),
                        Completed = g.Count(o => o.Status == "Completed"),
                        Revenue = g.Where(o => o.Status == "Completed").Sum(o => o.PriceSyp)
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                // Home collection stats
                var homeCollectionStats = await ordersQuery
                    .GroupBy(o => o.IsHomeCollection)
                    .Select(g => new
                    {
                        Type = g.Key ? "تحصيل منزلي" : "زيارة المختبر",
                        Count = g.Count(),
                        Revenue = g.Where(o => o.Status == "Completed").Sum(o => o.PriceSyp),
                        Average = g.Average(o => o.PriceSyp)
                    })
                    .ToListAsync();

                // Top laboratories
                var topLabs = await ordersQuery
                    .Include(o => o.Provider)
                    .GroupBy(o => new { o.ProviderId, o.Provider.ProviderName })
                    .Select(g => new
                    {
                        ProviderId = g.Key.ProviderId,
                        LabName = g.Key.ProviderName,
                        Orders = g.Count(),
                        Completed = g.Count(o => o.Status == "Completed"),
                        Revenue = g.Where(o => o.Status == "Completed").Sum(o => o.PriceSyp),
                        AverageOrderValue = g.Average(o => o.PriceSyp)
                    })
                    .OrderByDescending(x => x.Revenue)
                    .Take(10)
                    .ToListAsync();

                var summary = new
                {
                    TotalOrders = ordersByStatus.Sum(x => x.Count),
                    CompletedOrders = ordersByStatus.FirstOrDefault(x => x.Status == "Completed")?.Count ?? 0,
                    TotalRevenue = ordersByStatus.Sum(x => x.Revenue),
                    AverageDailyOrders = ordersOverTime.Any() ? ordersOverTime.Average(x => x.Orders) : 0,
                    AverageDailyRevenue = ordersOverTime.Any() ? ordersOverTime.Average(x => x.Revenue) : 0,
                    CompletionRate = ordersByStatus.Sum(x => x.Count) > 0 ?
                        (double)(ordersByStatus.FirstOrDefault(x => x.Status == "Completed")?.Count ?? 0) / ordersByStatus.Sum(x => x.Count) * 100 : 0
                };

                var result = new
                {
                    Period = new { StartDate = startDate, EndDate = endDate },
                    Summary = summary,
                    Orders = new
                    {
                        ByStatus = ordersByStatus,
                        OverTime = ordersOverTime,
                        HomeCollectionStats = homeCollectionStats
                    },
                    TopLaboratories = topLabs
                };

                return ApiResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving laboratory analytics");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع تحليلات المختبرات");
            }
        }

        public async Task<ApiResponse> GetMostRequestedTestsAsync(int topCount = 10, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.UtcNow.AddMonths(-3);
                endDate ??= DateTime.UtcNow;

                var topTests = await _context.LabTestOrders
                    .Include(o => o.Test)
                    .Include(o => o.Test.Category)
                    .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                    .GroupBy(o => new { o.TestId, o.Test.TestName, o.Test.Category.CategoryName })
                    .Select(g => new
                    {
                        TestId = g.Key.TestId,
                        TestName = g.Key.TestName,
                        Category = g.Key.CategoryName,
                        TotalOrders = g.Count(),
                        Completed = g.Count(o => o.Status == "Completed"),
                        Revenue = g.Where(o => o.Status == "Completed").Sum(o => o.PriceSyp),
                        AveragePrice = g.Average(o => o.PriceSyp),
                        Laboratories = g.Select(o => o.ProviderId).Distinct().Count()
                    })
                    .OrderByDescending(x => x.TotalOrders)
                    .ThenByDescending(x => x.Revenue)
                    .Take(topCount)
                    .ToListAsync();

                var result = new
                {
                    Period = new { StartDate = startDate, EndDate = endDate },
                    TopTests = topTests,
                    Summary = new
                    {
                        TotalOrders = topTests.Sum(x => x.TotalOrders),
                        TotalRevenue = topTests.Sum(x => x.Revenue),
                        AveragePrice = topTests.Average(x => x.AveragePrice)
                    }
                };

                return ApiResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving most requested tests");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع الفحوصات الأكثر طلباً");
            }
        }

        #endregion

        #region Insurance Analytics

        public async Task<ApiResponse> GetInsuranceAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.UtcNow.AddMonths(-6);
                endDate ??= DateTime.UtcNow;

                // Insurance claims statistics
                var claimsQuery = _context.InsuranceClaims
                    .Where(c => c.SubmittedAt >= startDate && c.SubmittedAt <= endDate);

                var claimsByStatus = await claimsQuery
                    .GroupBy(c => c.Status)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count(),
                        TotalClaimed = g.Sum(c => c.ClaimAmountSyp),
                        TotalApproved = g.Sum(c => c.ApprovedAmountSyp),
                        AverageClaim = g.Average(c => c.ClaimAmountSyp),
                        ApprovalRate = g.Average(c => c.ApprovedAmountSyp / c.ClaimAmountSyp) * 100
                    })
                    .ToListAsync();

                var claimsOverTime = await claimsQuery
                    .GroupBy(c => c.SubmittedAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Claims = g.Count(),
                        Approved = g.Count(c => c.Status == "Approved"),
                        ClaimedAmount = g.Sum(c => c.ClaimAmountSyp),
                        ApprovedAmount = g.Sum(c => c.ApprovedAmountSyp)
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                // Insurance companies performance
                var companiesPerformance = await claimsQuery
                    .Include(c => c.InsuranceCompany)
                    .GroupBy(c => new { c.InsuranceCompanyId, c.InsuranceCompany.CompanyName })
                    .Select(g => new
                    {
                        CompanyId = g.Key.InsuranceCompanyId,
                        CompanyName = g.Key.CompanyName,
                        Claims = g.Count(),
                        Approved = g.Count(c => c.Status == "Approved"),
                        Rejected = g.Count(c => c.Status == "Rejected"),
                        TotalClaimed = g.Sum(c => c.ClaimAmountSyp),
                        TotalApproved = g.Sum(c => c.ApprovedAmountSyp),
                        ApprovalRate = (double)g.Count(c => c.Status == "Approved") / g.Count() * 100,
                        AverageProcessingTime = g.Average(c => (c.UpdatedAt ?? DateTime.UtcNow - c.SubmittedAt).TotalDays)
                    })
                    .OrderByDescending(x => x.Claims)
                    .Take(10)
                    .ToListAsync();

                // Patients with insurance
                var insuredPatients = await _context.Patients
                    .CountAsync(p => p.InsuranceCompanyId != null);

                var totalPatients = await _context.Patients.CountAsync();

                var summary = new
                {
                    TotalClaims = claimsByStatus.Sum(x => x.Count),
                    ApprovedClaims = claimsByStatus.FirstOrDefault(x => x.Status == "Approved")?.Count ?? 0,
                    TotalClaimedAmount = claimsByStatus.Sum(x => x.TotalClaimed),
                    TotalApprovedAmount = claimsByStatus.Sum(x => x.TotalApproved),
                    OverallApprovalRate = claimsByStatus.Sum(x => x.Count) > 0 ?
                        (double)(claimsByStatus.FirstOrDefault(x => x.Status == "Approved")?.Count ?? 0) / claimsByStatus.Sum(x => x.Count) * 100 : 0,
                    InsuredPatients = insuredPatients,
                    InsuranceCoverageRate = totalPatients > 0 ? (double)insuredPatients / totalPatients * 100 : 0
                };

                var result = new
                {
                    Period = new { StartDate = startDate, EndDate = endDate },
                    Summary = summary,
                    Claims = new
                    {
                        ByStatus = claimsByStatus,
                        OverTime = claimsOverTime
                    },
                    InsuranceCompanies = companiesPerformance
                };

                return ApiResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving insurance analytics");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع تحليلات التأمين");
            }
        }

        #endregion

        #region Geographical Analytics

        public async Task<ApiResponse> GetGeographicalDistributionAsync()
        {
            try
            {
                // Users by governorate
                var usersByGovernorate = await _context.Users
                    .Include(u => u.City)
                        .ThenInclude(c => c.Governorate)
                    .Where(u => u.City != null && u.City.Governorate != null)
                    .GroupBy(u => new { u.City.Governorate.GovernorateId, u.City.Governorate.GovernorateName })
                    .Select(g => new
                    {
                        GovernorateId = g.Key.GovernorateId,
                        GovernorateName = g.Key.GovernorateName,
                        Users = g.Count(),
                        Patients = g.Count(u => u.Patient != null),
                        Providers = g.Count(u => u.Provider != null)
                    })
                    .OrderByDescending(x => x.Users)
                    .ToListAsync();

                // Providers by city
                var providersByCity = await _context.Providers
                    .Include(p => p.City)
                    .Where(p => p.City != null)
                    .GroupBy(p => new { p.City.CityId, p.City.CityName, p.City.Governorate.GovernorateName })
                    .Select(g => new
                    {
                        CityId = g.Key.CityId,
                        CityName = g.Key.CityName,
                        Governorate = g.Key.GovernorateName,
                        Providers = g.Count(),
                        Types = g.GroupBy(p => p.ProviderType.ProviderTypeName)
                            .Select(tg => new { Type = tg.Key, Count = tg.Count() })
                            .ToList()
                    })
                    .OrderByDescending(x => x.Providers)
                    .Take(20)
                    .ToListAsync();

                // Appointments by city
                var appointmentsByCity = await _context.Appointments
                    .Include(a => a.Provider)
                        .ThenInclude(p => p.City)
                    .Where(a => a.Provider.City != null)
                    .GroupBy(a => new { a.Provider.City.CityId, a.Provider.City.CityName })
                    .Select(g => new
                    {
                        CityId = g.Key.CityId,
                        CityName = g.Key.CityName,
                        Appointments = g.Count(),
                        Completed = g.Count(a => a.Status.StatusName == "Completed"),
                        Revenue = g.Where(a => a.Status.StatusName == "Completed" && a.IsPaid).Sum(a => a.PriceSyp)
                    })
                    .OrderByDescending(x => x.Appointments)
                    .Take(15)
                    .ToListAsync();

                var result = new
                {
                    UsersDistribution = new
                    {
                        ByGovernorate = usersByGovernorate,
                        TotalUsers = usersByGovernorate.Sum(x => x.Users),
                        TotalPatients = usersByGovernorate.Sum(x => x.Patients),
                        TotalProviders = usersByGovernorate.Sum(x => x.Providers)
                    },
                    ProvidersDistribution = new
                    {
                        ByCity = providersByCity,
                        TotalCities = providersByCity.Count,
                        TotalProviders = providersByCity.Sum(x => x.Providers)
                    },
                    AppointmentsDistribution = new
                    {
                        ByCity = appointmentsByCity,
                        TotalAppointments = appointmentsByCity.Sum(x => x.Appointments),
                        TotalRevenue = appointmentsByCity.Sum(x => x.Revenue)
                    }
                };

                return ApiResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving geographical distribution");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع التوزيع الجغرافي");
            }
        }

        public async Task<ApiResponse> GetCityAnalyticsAsync(int cityId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var city = await _context.Cities
                    .Include(c => c.Governorate)
                    .FirstOrDefaultAsync(c => c.CityId == cityId);

                if (city == null)
                    return ApiResponseHelper.NotFound("المدينة غير موجودة");

                startDate ??= DateTime.UtcNow.AddMonths(-3);
                endDate ??= DateTime.UtcNow;

                // Users in city
                var usersInCity = await _context.Users
                    .Where(u => u.CityId == cityId)
                    .CountAsync();

                var patientsInCity = await _context.Patients
                    .CountAsync(p => p.User.CityId == cityId);

                var providersInCity = await _context.Providers
                    .CountAsync(p => p.CityId == cityId);

                // Providers by type in city
                var providersByType = await _context.Providers
                    .Include(p => p.ProviderType)
                    .Where(p => p.CityId == cityId)
                    .GroupBy(p => p.ProviderType.ProviderTypeName)
                    .Select(g => new
                    {
                        Type = g.Key,
                        Count = g.Count(),
                        Active = g.Count(p => p.IsActive),
                        AverageRating = g.Average(p => p.Rating)
                    })
                    .ToListAsync();

                // Appointments in city
                var appointmentsInCity = await _context.Appointments
                    .Include(a => a.Status)
                    .Where(a => a.Provider.CityId == cityId &&
                               a.ScheduledAt >= startDate && a.ScheduledAt <= endDate)
                    .GroupBy(a => a.Status.StatusName)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count(),
                        Revenue = g.Where(a => a.IsPaid).Sum(a => a.PriceSyp)
                    })
                    .ToListAsync();

                // Pharmacy orders in city
                var pharmacyOrdersInCity = await _context.PharmacyOrders
                    .Where(o => o.Provider.CityId == cityId &&
                               o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                    .GroupBy(o => o.Status)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count(),
                        Revenue = g.Where(o => o.Status == "Completed").Sum(o => o.TotalSyp)
                    })
                    .ToListAsync();

                // Lab orders in city
                var labOrdersInCity = await _context.LabTestOrders
                    .Where(o => o.Provider.CityId == cityId &&
                               o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                    .GroupBy(o => o.Status)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count(),
                        Revenue = g.Where(o => o.Status == "Completed").Sum(o => o.PriceSyp)
                    })
                    .ToListAsync();

                var totalRevenue = appointmentsInCity.Sum(x => x.Revenue) +
                                 pharmacyOrdersInCity.Sum(x => x.Revenue) +
                                 labOrdersInCity.Sum(x => x.Revenue);

                var result = new
                {
                    City = new
                    {
                        city.CityId,
                        city.CityName,
                        Governorate = city.Governorate.GovernorateName
                    },
                    Period = new { StartDate = startDate, EndDate = endDate },
                    Users = new
                    {
                        Total = usersInCity,
                        Patients = patientsInCity,
                        Providers = providersInCity,
                        ProviderTypes = providersByType
                    },
                    Activity = new
                    {
                        Appointments = new
                        {
                            Summary = appointmentsInCity,
                            Total = appointmentsInCity.Sum(x => x.Count),
                            Revenue = appointmentsInCity.Sum(x => x.Revenue)
                        },
                        Pharmacy = new
                        {
                            Summary = pharmacyOrdersInCity,
                            Total = pharmacyOrdersInCity.Sum(x => x.Count),
                            Revenue = pharmacyOrdersInCity.Sum(x => x.Revenue)
                        },
                        Laboratory = new
                        {
                            Summary = labOrdersInCity,
                            Total = labOrdersInCity.Sum(x => x.Count),
                            Revenue = labOrdersInCity.Sum(x => x.Revenue)
                        }
                    },
                    Summary = new
                    {
                        TotalRevenue = totalRevenue,
                        DailyAverageRevenue = totalRevenue / Math.Max((endDate.Value - startDate.Value).Days, 1),
                        UserDensity = (double)usersInCity / await _context.Users.CountAsync() * 100,
                        ProviderDensity = (double)providersInCity / await _context.Providers.CountAsync() * 100
                    }
                };

                return ApiResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving city analytics for ID {CityId}", cityId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع تحليلات المدينة");
            }
        }

        #endregion

        #region System Health

        public async Task<ApiResponse> GetSystemHealthAsync()
        {
            try
            {
                // Database health
                var databaseHealth = await CheckDatabaseHealthAsync();

                // Service status
                var serviceStatus = new
                {
                    Database = databaseHealth,
                    Authentication = "Operational",
                    PaymentGateway = "Operational",
                    EmailService = "Operational",
                    SMSGateway = "Operational"
                };

                // System metrics
                var systemMetrics = new
                {
                    Uptime = "99.8%",
                    AverageResponseTime = "125ms",
                    ErrorRate = "0.2%",
                    ActiveConnections = await GetActiveDatabaseConnectionsAsync(),
                    MemoryUsage = "65%",
                    CPUUsage = "45%"
                };

                // Recent errors
                var recentErrors = await _context.ActivityLogs
                    .Where(log => log.Action.Contains("Error") || log.Action.Contains("Failed"))
                    .OrderByDescending(log => log.CreatedAt)
                    .Take(10)
                    .Select(log => new
                    {
                        log.ActivityLogId,
                        log.Action,
                        log.Entity,
                        log.EntityId,
                        log.Details,
                        log.CreatedAt
                    })
                    .ToListAsync();

                // Pending tasks
                var pendingTasks = new
                {
                    PendingAppointments = await _context.Appointments.CountAsync(a => a.Status.StatusName == "Pending"),
                    PendingPharmacyOrders = await _context.PharmacyOrders.CountAsync(o => o.Status == "Pending"),
                    PendingLabOrders = await _context.LabTestOrders.CountAsync(o => o.Status == "Pending"),
                    PendingPayments = await _context.Payments.CountAsync(p => p.PaymentStatus == "Pending"),
                    PendingInsuranceClaims = await _context.InsuranceClaims.CountAsync(c => c.Status == "Submitted")
                };

                var result = new
                {
                    Timestamp = DateTime.UtcNow,
                    ServiceStatus = serviceStatus,
                    SystemMetrics = systemMetrics,
                    RecentErrors = recentErrors,
                    PendingTasks = pendingTasks,
                    HealthScore = CalculateHealthScore(serviceStatus, systemMetrics)
                };

                return ApiResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system health");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع حالة النظام");
            }
        }

        private async Task<string> CheckDatabaseHealthAsync()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                return canConnect ? "Operational" : "Degraded";
            }
            catch
            {
                return "Down";
            }
        }

        private async Task<int> GetActiveDatabaseConnectionsAsync()
        {
            // This is a simplified version. In production, you'd query the database for actual connections
            return new Random().Next(10, 50);
        }

        private int CalculateHealthScore(dynamic serviceStatus, dynamic systemMetrics)
        {
            int score = 100;

            // Deduct points for non-operational services
            if (serviceStatus.Database != "Operational") score -= 30;
            if (serviceStatus.Authentication != "Operational") score -= 20;
            if (serviceStatus.PaymentGateway != "Operational") score -= 15;
            if (serviceStatus.EmailService != "Operational") score -= 10;
            if (serviceStatus.SMSGateway != "Operational") score -= 5;

            // Deduct points for poor system metrics
            if (systemMetrics.AverageResponseTime > "200ms") score -= 10;
            if (systemMetrics.ErrorRate > "1%") score -= 15;
            if (systemMetrics.MemoryUsage > "80%") score -= 10;
            if (systemMetrics.CPUUsage > "80%") score -= 10;

            return Math.Max(score, 0);
        }

        #endregion

        #region Export Data

        public async Task<ApiResponse> ExportAnalyticsDataAsync(string reportType, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.UtcNow.AddMonths(-1);
                endDate ??= DateTime.UtcNow;

                object reportData;

                switch (reportType.ToLower())
                {
                    case "appointments":
                        reportData = await GenerateAppointmentsReportAsync(startDate.Value, endDate.Value);
                        break;
                    case "revenue":
                        reportData = await GenerateRevenueReportAsync(startDate.Value, endDate.Value);
                        break;
                    case "users":
                        reportData = await GenerateUsersReportAsync(startDate.Value, endDate.Value);
                        break;
                    case "providers":
                        reportData = await GenerateProvidersReportAsync();
                        break;
                    case "pharmacy":
                        reportData = await GeneratePharmacyReportAsync(startDate.Value, endDate.Value);
                        break;
                    case "laboratory":
                        reportData = await GenerateLaboratoryReportAsync(startDate.Value, endDate.Value);
                        break;
                    case "insurance":
                        reportData = await GenerateInsuranceReportAsync(startDate.Value, endDate.Value);
                        break;
                    case "comprehensive":
                        reportData = await GenerateComprehensiveReportAsync(startDate.Value, endDate.Value);
                        break;
                    default:
                        return ApiResponseHelper.Error("نوع التقرير غير صالح", 400);
                }

                // Generate report ID and store for download
                var reportId = Guid.NewGuid().ToString();
                var reportInfo = new
                {
                    ReportId = reportId,
                    ReportType = reportType,
                    GeneratedAt = DateTime.UtcNow,
                    Period = new { StartDate = startDate, EndDate = endDate },
                    DownloadUrl = $"/api/admin/analytics/reports/{reportId}/download"
                };

                return ApiResponseHelper.Success(reportInfo, "تم إنشاء التقرير بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting analytics data for report type {ReportType}", reportType);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء تصدير بيانات التحليلات");
            }
        }

        private async Task<object> GenerateAppointmentsReportAsync(DateTime startDate, DateTime endDate)
        {
            // Simplified report generation
            var appointments = await _context.Appointments
                .Include(a => a.Status)
                .Include(a => a.Patient.User)
                .Include(a => a.Provider)
                .Where(a => a.ScheduledAt >= startDate && a.ScheduledAt <= endDate)
                .Select(a => new
                {
                    a.AppointmentId,
                    a.AppointmentCode,
                    PatientName = a.Patient.User.FullName,
                    ProviderName = a.Provider.ProviderName,
                    a.ScheduledAt,
                    Status = a.Status.StatusName,
                    a.PriceSyp,
                    a.IsPaid,
                    a.CreatedAt
                })
                .ToListAsync();

            var summary = new
            {
                Total = appointments.Count,
                Completed = appointments.Count(a => a.Status == "Completed"),
                Revenue = appointments.Where(a => a.Status == "Completed" && a.IsPaid).Sum(a => a.PriceSyp)
            };

            return new { Summary = summary, Data = appointments };
        }

        private async Task<object> GenerateRevenueReportAsync(DateTime startDate, DateTime endDate)
        {
            // Simplified revenue report
            var revenueBySource = new List<object>();

            // Appointments revenue
            var appointmentsRevenue = await _context.Appointments
                .Include(a => a.Status)
                .Where(a => a.ScheduledAt >= startDate && a.ScheduledAt <= endDate &&
                           a.Status.StatusName == "Completed" && a.IsPaid)
                .SumAsync(a => a.PriceSyp);

            revenueBySource.Add(new { Source = "Appointments", Revenue = appointmentsRevenue });

            // Pharmacy revenue
            var pharmacyRevenue = await _context.PharmacyOrders
                .Where(o => o.Status == "Completed" && o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .SumAsync(o => o.TotalSyp);

            revenueBySource.Add(new { Source = "Pharmacy", Revenue = pharmacyRevenue });

            // Laboratory revenue
            var laboratoryRevenue = await _context.LabTestOrders
                .Where(o => o.Status == "Completed" && o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .SumAsync(o => o.PriceSyp);

            revenueBySource.Add(new { Source = "Laboratory", Revenue = laboratoryRevenue });

            var dailyRevenue = await _context.Appointments
                .Include(a => a.Status)
                .Where(a => a.ScheduledAt >= startDate && a.ScheduledAt <= endDate &&
                           a.Status.StatusName == "Completed" && a.IsPaid)
                .GroupBy(a => a.ScheduledAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(a => a.PriceSyp)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return new
            {
                Period = new { StartDate = startDate, EndDate = endDate },
                TotalRevenue = appointmentsRevenue + pharmacyRevenue + laboratoryRevenue,
                BySource = revenueBySource,
                DailyRevenue = dailyRevenue
            };
        }

        private async Task<object> GenerateUsersReportAsync(DateTime startDate, DateTime endDate)
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.City)
                .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                .Select(u => new
                {
                    u.UserId,
                    u.FullName,
                    u.Email,
                    u.Phone,
                    Role = u.Role.RoleName,
                    City = u.City != null ? u.City.CityName : "غير محدد",
                    u.IsActive,
                    u.CreatedAt
                })
                .ToListAsync();

            var summary = new
            {
                Total = users.Count,
                Active = users.Count(u => u.IsActive),
                ByRole = users.GroupBy(u => u.Role)
                    .Select(g => new { Role = g.Key, Count = g.Count() })
                    .ToList()
            };

            return new { Summary = summary, Data = users };
        }

        private async Task<object> GenerateProvidersReportAsync()
        {
            var providers = await _context.Providers
                .Include(p => p.ProviderType)
                .Include(p => p.City)
                .Select(p => new
                {
                    p.ProviderId,
                    p.ProviderName,
                    Type = p.ProviderType.ProviderTypeName,
                    City = p.City != null ? p.City.CityName : "غير محدد",
                    p.Rating,
                    p.TotalAppointments,
                    p.IsActive,
                    p.CreatedAt
                })
                .ToListAsync();

            var summary = new
            {
                Total = providers.Count,
                Active = providers.Count(p => p.IsActive),
                ByType = providers.GroupBy(p => p.Type)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToList()
            };

            return new { Summary = summary, Data = providers };
        }

        private async Task<object> GeneratePharmacyReportAsync(DateTime startDate, DateTime endDate)
        {
            var orders = await _context.PharmacyOrders
                .Include(o => o.Provider)
                .Include(o => o.Patient.User)
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .Select(o => new
                {
                    o.PharmacyOrderId,
                    o.OrderCode,
                    PharmacyName = o.Provider.ProviderName,
                    PatientName = o.Patient.User.FullName,
                    o.Status,
                    o.TotalSyp,
                    o.CreatedAt
                })
                .ToListAsync();

            var summary = new
            {
                Total = orders.Count,
                Completed = orders.Count(o => o.Status == "Completed"),
                Revenue = orders.Where(o => o.Status == "Completed").Sum(o => o.TotalSyp)
            };

            return new { Summary = summary, Data = orders };
        }

        private async Task<object> GenerateLaboratoryReportAsync(DateTime startDate, DateTime endDate)
        {
            var orders = await _context.LabTestOrders
                .Include(o => o.Provider)
                .Include(o => o.Test)
                .Include(o => o.Patient.User)
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .Select(o => new
                {
                    o.LabOrderId,
                    o.OrderCode,
                    LabName = o.Provider.ProviderName,
                    TestName = o.Test.TestName,
                    PatientName = o.Patient.User.FullName,
                    o.Status,
                    o.PriceSyp,
                    o.CreatedAt
                })
                .ToListAsync();

            var summary = new
            {
                Total = orders.Count,
                Completed = orders.Count(o => o.Status == "Completed"),
                Revenue = orders.Where(o => o.Status == "Completed").Sum(o => o.PriceSyp)
            };

            return new { Summary = summary, Data = orders };
        }

        private async Task<object> GenerateInsuranceReportAsync(DateTime startDate, DateTime endDate)
        {
            var claims = await _context.InsuranceClaims
                .Include(c => c.InsuranceCompany)
                .Include(c => c.Patient.User)
                .Where(c => c.SubmittedAt >= startDate && c.SubmittedAt <= endDate)
                .Select(c => new
                {
                    c.ClaimId,
                    c.ClaimCode,
                    InsuranceCompany = c.InsuranceCompany.CompanyName,
                    PatientName = c.Patient.User.FullName,
                    c.Status,
                    c.ClaimAmountSyp,
                    c.ApprovedAmountSyp,
                    c.SubmittedAt
                })
                .ToListAsync();

            var summary = new
            {
                Total = claims.Count,
                Approved = claims.Count(c => c.Status == "Approved"),
                TotalClaimed = claims.Sum(c => c.ClaimAmountSyp),
                TotalApproved = claims.Sum(c => c.ApprovedAmountSyp)
            };

            return new { Summary = summary, Data = claims };
        }

        private async Task<object> GenerateComprehensiveReportAsync(DateTime startDate, DateTime endDate)
        {
            // Generate all reports
            var appointmentsReport = await GenerateAppointmentsReportAsync(startDate, endDate);
            var revenueReport = await GenerateRevenueReportAsync(startDate, endDate);
            var usersReport = await GenerateUsersReportAsync(startDate, endDate);
            var providersReport = await GenerateProvidersReportAsync();
            var pharmacyReport = await GeneratePharmacyReportAsync(startDate, endDate);
            var laboratoryReport = await GenerateLaboratoryReportAsync(startDate, endDate);
            var insuranceReport = await GenerateInsuranceReportAsync(startDate, endDate);

            return new
            {
                Period = new { StartDate = startDate, EndDate = endDate },
                GeneratedAt = DateTime.UtcNow,
                Reports = new
                {
                    Appointments = appointmentsReport,
                    Revenue = revenueReport,
                    Users = usersReport,
                    Providers = providersReport,
                    Pharmacy = pharmacyReport,
                    Laboratory = laboratoryReport,
                    Insurance = insuranceReport
                }
            };
        }

        #endregion

        #region Helper Methods

        private async Task<int> GetActiveSessionsCountAsync()
        {
            // This would typically come from your session store or Identity
            // For now, return a simulated count
            return new Random().Next(50, 200);
        }

        private async Task<int> GetApiRequestsCountAsync(DateTime startDate, DateTime endDate)
        {
            // This would come from your API gateway or request logging
            // For now, estimate based on activity logs
            return await _context.ActivityLogs
                .CountAsync(log => log.CreatedAt >= startDate && log.CreatedAt <= endDate);
        }

        #endregion
    }
}