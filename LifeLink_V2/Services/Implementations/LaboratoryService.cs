using Microsoft.EntityFrameworkCore;
using LifeLink_V2.Data;
using LifeLink_V2.Models;
using LifeLink_V2.Helpers;
using LifeLink_V2.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LifeLink_V2.Services.Implementations
{
    public class LaboratoryService : ILaboratoryService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<LaboratoryService> _logger;

        public LaboratoryService(AppDbContext context, ILogger<LaboratoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Lab Tests Management

        public async Task<ApiResponse> GetLabTestsAsync(int providerId, int? categoryId = null, string? search = null, int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.LabTests
                    .Include(lt => lt.Provider)
                    .Include(lt => lt.Category)
                    .Where(lt => lt.ProviderId == providerId)
                    .AsQueryable();

                // Apply category filter
                if (categoryId.HasValue)
                    query = query.Where(lt => lt.CategoryId == categoryId.Value);

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    query = query.Where(lt =>
                        lt.TestName.ToLower().Contains(search) ||
                        (lt.TestCode != null && lt.TestCode.ToLower().Contains(search)) ||
                        (lt.Description != null && lt.Description.ToLower().Contains(search)));
                }

                // Get total count for pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var labTests = await query
                    .OrderBy(lt => lt.TestName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(lt => new
                    {
                        lt.LabTestId,
                        lt.TestName,
                        lt.TestCode,
                        Category = lt.Category != null ? new
                        {
                            lt.Category.CategoryId,
                            lt.Category.CategoryName
                        } : null,
                        lt.Description,
                        lt.PriceSyp,
                        lt.PriceUsd,
                        lt.PreparationInstructions,
                        lt.SampleType,
                        lt.TurnaroundTime,
                        lt.CreatedAt,
                        Provider = new
                        {
                            lt.Provider.ProviderId,
                            lt.Provider.ProviderName
                        }
                    })
                    .ToListAsync();

                return ApiResponseHelper.Success(new
                {
                    LabTests = labTests,
                    Pagination = new
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lab tests for provider {ProviderId}", providerId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع الفحوصات المخبرية");
            }
        }

        public async Task<ApiResponse> GetLabTestByIdAsync(int testId)
        {
            try
            {
                var labTest = await _context.LabTests
                    .Include(lt => lt.Provider)
                    .Include(lt => lt.Category)
                    .FirstOrDefaultAsync(lt => lt.LabTestId == testId);

                if (labTest == null)
                    return ApiResponseHelper.NotFound("الفحص المخبري غير موجود");

                var result = new
                {
                    labTest.LabTestId,
                    labTest.TestName,
                    labTest.TestCode,
                    Category = labTest.Category != null ? new
                    {
                        labTest.Category.CategoryId,
                        labTest.Category.CategoryName,
                        labTest.Category.Description
                    } : null,
                    labTest.Description,
                    labTest.PriceSyp,
                    labTest.PriceUsd,
                    labTest.PreparationInstructions,
                    labTest.SampleType,
                    labTest.TurnaroundTime,
                    labTest.CreatedAt,
                    Provider = new
                    {
                        labTest.Provider.ProviderId,
                        labTest.Provider.ProviderName,
                        labTest.Provider.Phone,
                        labTest.Provider.Email
                    }
                };

                return ApiResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lab test with ID {TestId}", testId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع بيانات الفحص المخبري");
            }
        }

        public async Task<ApiResponse> AddLabTestAsync(AddLabTestDto testDto, int currentUserId)
        {
            try
            {
                // Validate provider exists and is a laboratory
                var provider = await _context.Providers
                    .Include(p => p.ProviderType)
                    .FirstOrDefaultAsync(p => p.ProviderId == testDto.ProviderId);

                if (provider == null)
                    return ApiResponseHelper.NotFound("مقدم الخدمة غير موجود");

                if (provider.ProviderType.ProviderTypeName != "Laboratory")
                    return ApiResponseHelper.Error("هذا المقدم ليس مختبراً", 400);

                // Validate category if provided
                if (testDto.CategoryId.HasValue)
                {
                    var category = await _context.LabTestCategories
                        .FirstOrDefaultAsync(c => c.CategoryId == testDto.CategoryId.Value);

                    if (category == null)
                        return ApiResponseHelper.Error("فئة الفحص غير موجودة", 400);
                }

                // Check if test with same name/code already exists for this provider
                var existingTest = await _context.LabTests
                    .FirstOrDefaultAsync(lt => lt.ProviderId == testDto.ProviderId &&
                                            (lt.TestName.ToLower() == testDto.TestName.ToLower() ||
                                             (lt.TestCode != null && lt.TestCode == testDto.TestCode)));

                if (existingTest != null)
                    return ApiResponseHelper.Error("هذا الفحص موجود بالفعل في هذا المختبر", 400);

                // Calculate exchange rate if needed
                decimal? exchangeRate = null;
                if (testDto.PriceSYP > 0 && testDto.PriceUSD > 0)
                {
                    exchangeRate = testDto.PriceSYP / testDto.PriceUSD;
                }

                // Create lab test
                var labTest = new LabTest
                {
                    ProviderId = testDto.ProviderId,
                    TestName = testDto.TestName,
                    TestCode = testDto.TestCode,
                    CategoryId = testDto.CategoryId,
                    Description = testDto.Description,
                    PriceSyp = testDto.PriceSYP,
                    PriceUsd = testDto.PriceUSD,
                    PreparationInstructions = testDto.PreparationInstructions,
                    SampleType = testDto.SampleType,
                    TurnaroundTime = testDto.TurnaroundTime,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.LabTests.AddAsync(labTest);
                await _context.SaveChangesAsync();

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Add Lab Test",
                    Entity = "LabTest",
                    EntityId = labTest.LabTestId.ToString(),
                    Details = $"Added lab test {labTest.TestName}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Lab test added: {TestName} with ID {TestId}",
                    labTest.TestName, labTest.LabTestId);

                return ApiResponseHelper.Success(new
                {
                    LabTestId = labTest.LabTestId,
                    labTest.TestName,
                    labTest.TestCode,
                    labTest.PriceSyp,
                    labTest.PriceUsd
                }, "تم إضافة الفحص المخبري بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding lab test");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء إضافة الفحص المخبري");
            }
        }

        public async Task<ApiResponse> UpdateLabTestAsync(int testId, UpdateLabTestDto testDto, int currentUserId)
        {
            try
            {
                var labTest = await _context.LabTests
                    .FirstOrDefaultAsync(lt => lt.LabTestId == testId);

                if (labTest == null)
                    return ApiResponseHelper.NotFound("الفحص المخبري غير موجود");

                // Update fields if provided
                if (!string.IsNullOrEmpty(testDto.TestName))
                    labTest.TestName = testDto.TestName;

                if (!string.IsNullOrEmpty(testDto.TestCode))
                    labTest.TestCode = testDto.TestCode;

                if (testDto.CategoryId.HasValue)
                    labTest.CategoryId = testDto.CategoryId.Value;

                if (!string.IsNullOrEmpty(testDto.Description))
                    labTest.Description = testDto.Description;

                if (testDto.PriceSYP.HasValue)
                    labTest.PriceSyp = testDto.PriceSYP.Value;

                if (testDto.PriceUSD.HasValue)
                    labTest.PriceUsd = testDto.PriceUSD.Value;

                if (!string.IsNullOrEmpty(testDto.PreparationInstructions))
                    labTest.PreparationInstructions = testDto.PreparationInstructions;

                if (!string.IsNullOrEmpty(testDto.SampleType))
                    labTest.SampleType = testDto.SampleType;

                if (!string.IsNullOrEmpty(testDto.TurnaroundTime))
                    labTest.TurnaroundTime = testDto.TurnaroundTime;

                await _context.SaveChangesAsync();

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Update Lab Test",
                    Entity = "LabTest",
                    EntityId = testId.ToString(),
                    Details = $"Updated lab test {labTest.TestName}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);
                await _context.SaveChangesAsync();

                return ApiResponseHelper.Success(new
                {
                    labTest.LabTestId,
                    labTest.TestName,
                    labTest.TestCode,
                    labTest.PriceSyp,
                    labTest.PriceUsd
                }, "تم تحديث بيانات الفحص المخبري بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lab test {TestId}", testId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء تحديث بيانات الفحص المخبري");
            }
        }

        public async Task<ApiResponse> DeleteLabTestAsync(int testId, int currentUserId)
        {
            try
            {
                var labTest = await _context.LabTests
                    .FirstOrDefaultAsync(lt => lt.LabTestId == testId);

                if (labTest == null)
                    return ApiResponseHelper.NotFound("الفحص المخبري غير موجود");

                // Check if test has any active orders
                var hasActiveOrders = await _context.LabTestOrders
                    .AnyAsync(o => o.TestId == testId &&
                                   o.Status != "Cancelled" &&
                                   o.Status != "Completed");

                if (hasActiveOrders)
                    return ApiResponseHelper.Error("لا يمكن حذف الفحص لأنه مرتبط بطلبات نشطة", 400);

                _context.LabTests.Remove(labTest);

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Delete Lab Test",
                    Entity = "LabTest",
                    EntityId = testId.ToString(),
                    Details = $"Deleted lab test {labTest.TestName}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);
                await _context.SaveChangesAsync();

                return ApiResponseHelper.Success(null, "تم حذف الفحص المخبري بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting lab test {TestId}", testId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء حذف الفحص المخبري");
            }
        }

        #endregion

        #region Lab Test Categories

        public async Task<ApiResponse> GetLabTestCategoriesAsync()
        {
            try
            {
                var categories = await _context.LabTestCategories
                    .OrderBy(c => c.CategoryName)
                    .Select(c => new
                    {
                        c.CategoryId,
                        c.CategoryName,
                        c.Description,
                        c.CreatedAt
                    })
                    .ToListAsync();

                return ApiResponseHelper.Success(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lab test categories");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع فئات الفحوصات");
            }
        }

        public async Task<ApiResponse> AddLabTestCategoryAsync(AddLabTestCategoryDto categoryDto, int currentUserId)
        {
            try
            {
                // Check if category with same name already exists
                var existingCategory = await _context.LabTestCategories
                    .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == categoryDto.CategoryName.ToLower());

                if (existingCategory != null)
                    return ApiResponseHelper.Error("هذه الفئة موجودة بالفعل", 400);

                var category = new LabTestCategory
                {
                    CategoryName = categoryDto.CategoryName,
                    Description = categoryDto.Description,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.LabTestCategories.AddAsync(category);
                await _context.SaveChangesAsync();

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Add Lab Test Category",
                    Entity = "LabTestCategory",
                    EntityId = category.CategoryId.ToString(),
                    Details = $"Added lab test category {category.CategoryName}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);
                await _context.SaveChangesAsync();

                return ApiResponseHelper.Success(new
                {
                    category.CategoryId,
                    category.CategoryName
                }, "تم إضافة فئة الفحوصات بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding lab test category");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء إضافة فئة الفحوصات");
            }
        }

        #endregion

        #region Lab Orders

        public async Task<ApiResponse> GetLabOrdersAsync(int? patientId = null, int? providerId = null,
            string? status = null, DateTime? dateFrom = null, DateTime? dateTo = null,
            int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.LabTestOrders
                    .Include(o => o.Patient)
                        .ThenInclude(p => p.User)
                    .Include(o => o.Provider)
                    .Include(o => o.Test)
                    .Include(o => o.LabTestResult)
                    .AsQueryable();

                // Apply filters
                if (patientId.HasValue)
                    query = query.Where(o => o.PatientId == patientId.Value);

                if (providerId.HasValue)
                    query = query.Where(o => o.ProviderId == providerId.Value);

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(o => o.Status == status);

                if (dateFrom.HasValue)
                    query = query.Where(o => o.CreatedAt >= dateFrom.Value);

                if (dateTo.HasValue)
                    query = query.Where(o => o.CreatedAt <= dateTo.Value);

                // Get total count for pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var orders = await query
                    .OrderByDescending(o => o.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(o => new
                    {
                        o.LabOrderId,
                        o.OrderCode,
                        Patient = new
                        {
                            o.Patient.PatientId,
                            o.Patient.User.FullName,
                            o.Patient.User.Phone
                        },
                        Provider = new
                        {
                            o.Provider.ProviderId,
                            o.Provider.ProviderName
                        },
                        Test = new
                        {
                            o.Test.LabTestId,
                            o.Test.TestName,
                            o.Test.TestCode
                        },
                        o.Status,
                        o.PriceSyp,
                        o.PriceUsd,
                        o.ScheduledAt,
                        o.IsHomeCollection,
                        o.HomeCollectionAddress,
                        o.HomeCollectionPhone,
                        HasResult = o.LabTestResult != null,
                        o.CreatedAt
                    })
                    .ToListAsync();

                return ApiResponseHelper.Success(new
                {
                    Orders = orders,
                    Pagination = new
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lab orders");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع طلبات المختبر");
            }
        }

        public async Task<ApiResponse> GetLabOrderByIdAsync(int orderId)
        {
            try
            {
                var order = await _context.LabTestOrders
                    .Include(o => o.Patient)
                        .ThenInclude(p => p.User)
                    .Include(o => o.Provider)
                    .Include(o => o.Test)
                    .Include(o => o.LabTestResult)
                        .ThenInclude(r => r.FullReportMedFile)
                    .FirstOrDefaultAsync(o => o.LabOrderId == orderId);

                if (order == null)
                    return ApiResponseHelper.NotFound("طلب المختبر غير موجود");

                var result = new
                {
                    order.LabOrderId,
                    order.OrderCode,
                    Patient = new
                    {
                        order.Patient.PatientId,
                        order.Patient.User.FullName,
                        order.Patient.User.Email,
                        order.Patient.User.Phone,
                        order.Patient.NationalId,
                        order.Patient.BloodType,
                        order.Patient.Dob
                    },
                    Provider = new
                    {
                        order.Provider.ProviderId,
                        order.Provider.ProviderName,
                        order.Provider.Address,
                        order.Provider.Phone,
                        order.Provider.Email
                    },
                    Test = new
                    {
                        order.Test.LabTestId,
                        order.Test.TestName,
                        order.Test.TestCode,
                        order.Test.Description,
                        order.Test.PreparationInstructions,
                        order.Test.SampleType,
                        order.Test.TurnaroundTime
                    },
                    order.Status,
                    order.PriceSyp,
                    order.PriceUsd,
                    order.ScheduledAt,
                    order.IsHomeCollection,
                    order.HomeCollectionAddress,
                    order.HomeCollectionPhone,
                    order.DoctorInstructions,
                    order.Notes,
                    order.CreatedAt,
                    Result = order.LabTestResult != null ? new
                    {
                        order.LabTestResult.LabTestResultId,
                        order.LabTestResult.ResultSummary,
                        order.LabTestResult.ResultDataJson,
                        order.LabTestResult.NormalRanges,
                        order.LabTestResult.Interpretations,
                        order.LabTestResult.TechnicianNotes,
                        order.LabTestResult.VerifiedBy,
                        order.LabTestResult.CreatedAt,
                        FullReport = order.LabTestResult.FullReportMedFile != null ? new
                        {
                            order.LabTestResult.FullReportMedFile.MedFileId,
                            order.LabTestResult.FullReportMedFile.MedFileName,
                            order.LabTestResult.FullReportMedFile.ContentType
                        } : null
                    } : null
                };

                return ApiResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lab order with ID {OrderId}", orderId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع بيانات طلب المختبر");
            }
        }

        public async Task<ApiResponse> CreateLabOrderAsync(CreateLabOrderDto orderDto, int currentUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Validate patient exists
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.PatientId == orderDto.PatientId);

                if (patient == null)
                    return ApiResponseHelper.NotFound("المريض غير موجود");

                // Validate provider exists and is a laboratory
                var provider = await _context.Providers
                    .Include(p => p.ProviderType)
                    .FirstOrDefaultAsync(p => p.ProviderId == orderDto.ProviderId);

                if (provider == null)
                    return ApiResponseHelper.NotFound("مقدم الخدمة غير موجود");

                if (provider.ProviderType.ProviderTypeName != "Laboratory")
                    return ApiResponseHelper.Error("هذا المقدم ليس مختبراً", 400);

                // Validate test exists and belongs to this provider
                var test = await _context.LabTests
                    .FirstOrDefaultAsync(t => t.LabTestId == orderDto.TestId &&
                                            t.ProviderId == orderDto.ProviderId);

                if (test == null)
                    return ApiResponseHelper.Error("الفحص غير موجود في هذا المختبر", 400);

                // Validate home collection details if needed
                if (orderDto.IsHomeCollection && string.IsNullOrEmpty(orderDto.HomeCollectionAddress))
                    return ApiResponseHelper.Error("يجب تحديد عنوان التحصيل المنزلي", 400);

                // Generate unique order code
                var orderCode = GenerateLabOrderCode();

                // Calculate exchange rate if needed
                decimal? exchangeRate = null;
                if (test.PriceSyp > 0 && test.PriceUsd > 0)
                {
                    exchangeRate = test.PriceSyp / test.PriceUsd;
                }

                // Create lab order
                var order = new LabTestOrder
                {
                    OrderCode = orderCode,
                    PatientId = orderDto.PatientId,
                    ProviderId = orderDto.ProviderId,
                    TestId = orderDto.TestId,
                    Status = "Pending",
                    PriceSyp = test.PriceSyp,
                    PriceUsd = test.PriceUsd,
                    ScheduledAt = orderDto.ScheduledAt ?? DateTime.UtcNow.AddHours(24), // Default to tomorrow
                    IsHomeCollection = orderDto.IsHomeCollection,
                    HomeCollectionAddress = orderDto.HomeCollectionAddress,
                    HomeCollectionPhone = orderDto.HomeCollectionPhone ?? patient.User.Phone,
                    DoctorInstructions = orderDto.DoctorInstructions,
                    Notes = orderDto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.LabTestOrders.AddAsync(order);
                await _context.SaveChangesAsync();

                // Create notification for patient
                var patientNotification = new Notification
                {
                    UserId = patient.UserId,
                    Title = "طلب مختبر جديد",
                    Message = $"تم إنشاء طلب مختبر جديد برقم {orderCode} للفحص {test.TestName}",
                    Channel = "InApp",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Notifications.AddAsync(patientNotification);

                // Create notification for provider
                var providerNotification = new Notification
                {
                    UserId = provider.UserId,
                    Title = "طلب مختبر جديد",
                    Message = $"طلب مختبر جديد من المريض {patient.User.FullName} للفحص {test.TestName}. الرقم: {orderCode}",
                    Channel = "InApp",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Notifications.AddAsync(providerNotification);

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Create Lab Order",
                    Entity = "LabTestOrder",
                    EntityId = order.LabOrderId.ToString(),
                    Details = $"Created lab order {orderCode} for patient {patient.User.FullName}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Lab order created: {OrderCode} for patient {PatientId}",
                    order.OrderCode, order.PatientId);

                return ApiResponseHelper.Success(new
                {
                    OrderId = order.LabOrderId,
                    OrderCode = order.OrderCode,
                    Status = order.Status,
                    TestName = test.TestName,
                    PriceSYP = order.PriceSyp,
                    PriceUSD = order.PriceUsd,
                    ScheduledAt = order.ScheduledAt
                }, "تم إنشاء طلب المختبر بنجاح");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating lab order");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء إنشاء طلب المختبر");
            }
        }

        public async Task<ApiResponse> UpdateLabOrderStatusAsync(int orderId, string status, int currentUserId)
        {
            try
            {
                var order = await _context.LabTestOrders
                    .Include(o => o.Patient)
                        .ThenInclude(p => p.User)
                    .Include(o => o.Provider)
                    .FirstOrDefaultAsync(o => o.LabOrderId == orderId);

                if (order == null)
                    return ApiResponseHelper.NotFound("طلب المختبر غير موجود");

                // Validate status
                var validStatuses = new[] { "Pending", "Confirmed", "SampleCollected", "Processing", "Completed", "Cancelled" };
                if (!validStatuses.Contains(status))
                    return ApiResponseHelper.Error("حالة غير صالحة", 400);

                var oldStatus = order.Status;
                order.Status = status;

                await _context.SaveChangesAsync();

                // Create notification for status change
                var notification = new Notification
                {
                    UserId = order.Patient.UserId,
                    Title = $"تغيير حالة طلب المختبر",
                    Message = $"تم تغيير حالة طلب المختبر {order.OrderCode} من {oldStatus} إلى {status}",
                    Channel = "InApp",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Notifications.AddAsync(notification);

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Update Lab Order Status",
                    Entity = "LabTestOrder",
                    EntityId = orderId.ToString(),
                    Details = $"Changed lab order status from {oldStatus} to {status}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);
                await _context.SaveChangesAsync();

                return ApiResponseHelper.Success(new
                {
                    order.LabOrderId,
                    order.OrderCode,
                    OldStatus = oldStatus,
                    NewStatus = status
                }, "تم تحديث حالة الطلب بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lab order status for ID {OrderId}", orderId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء تحديث حالة الطلب");
            }
        }

        public async Task<ApiResponse> CancelLabOrderAsync(int orderId, int currentUserId, string reason = null)
        {
            try
            {
                var order = await _context.LabTestOrders
                    .Include(o => o.Patient)
                        .ThenInclude(p => p.User)
                    .FirstOrDefaultAsync(o => o.LabOrderId == orderId);

                if (order == null)
                    return ApiResponseHelper.NotFound("طلب المختبر غير موجود");

                // Check if order can be cancelled
                if (order.Status == "Cancelled")
                    return ApiResponseHelper.Error("الطلب ملغي بالفعل", 400);

                if (order.Status == "Completed")
                    return ApiResponseHelper.Error("لا يمكن إلغاء طلب مكتمل", 400);

                // Check if sample already collected
                if (order.Status == "SampleCollected" || order.Status == "Processing")
                    return ApiResponseHelper.Error("لا يمكن إلغاء الطلب بعد جمع العينة", 400);

                var oldStatus = order.Status;
                order.Status = "Cancelled";
                order.Notes = reason ?? order.Notes;

                await _context.SaveChangesAsync();

                // Create notification for cancellation
                var notification = new Notification
                {
                    UserId = order.Patient.UserId,
                    Title = "تم إلغاء طلب المختبر",
                    Message = $"تم إلغاء طلب المختبر {order.OrderCode}" +
                             (reason != null ? $"\nالسبب: {reason}" : ""),
                    Channel = "InApp",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Notifications.AddAsync(notification);

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Cancel Lab Order",
                    Entity = "LabTestOrder",
                    EntityId = orderId.ToString(),
                    Details = $"Cancelled lab order {order.OrderCode}. Reason: {reason}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);
                await _context.SaveChangesAsync();

                return ApiResponseHelper.Success(new
                {
                    order.LabOrderId,
                    order.OrderCode,
                    Status = "Cancelled",
                    CancellationReason = reason
                }, "تم إلغاء الطلب بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling lab order {OrderId}", orderId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء إلغاء الطلب");
            }
        }

        public async Task<ApiResponse> CompleteLabOrderAsync(int orderId, int currentUserId)
        {
            try
            {
                var order = await _context.LabTestOrders
                    .Include(o => o.Patient)
                        .ThenInclude(p => p.User)
                    .FirstOrDefaultAsync(o => o.LabOrderId == orderId);

                if (order == null)
                    return ApiResponseHelper.NotFound("طلب المختبر غير موجود");

                // Check if order can be marked as completed
                if (order.Status != "Processing")
                    return ApiResponseHelper.Error("يمكن إكمال الطلب الذي قيد المعالجة فقط", 400);

                var oldStatus = order.Status;
                order.Status = "Completed";

                await _context.SaveChangesAsync();

                // Create notification for completion
                var notification = new Notification
                {
                    UserId = order.Patient.UserId,
                    Title = "تم إكمال طلب المختبر",
                    Message = $"تم إكمال طلب المختبر {order.OrderCode}. يمكنك الاطلاع على النتائج",
                    Channel = "InApp",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Notifications.AddAsync(notification);

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Complete Lab Order",
                    Entity = "LabTestOrder",
                    EntityId = orderId.ToString(),
                    Details = $"Completed lab order {order.OrderCode}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);
                await _context.SaveChangesAsync();

                return ApiResponseHelper.Success(new
                {
                    order.LabOrderId,
                    order.OrderCode,
                    Status = "Completed"
                }, "تم إكمال الطلب بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing lab order {OrderId}", orderId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء إكمال الطلب");
            }
        }

        #endregion

        #region Lab Results

        public async Task<ApiResponse> GetLabResultByOrderIdAsync(int orderId)
        {
            try
            {
                var result = await _context.LabTestResults
                    .Include(r => r.LabOrder)
                        .ThenInclude(o => o.Test)
                    .Include(r => r.FullReportMedFile)
                    .FirstOrDefaultAsync(r => r.LabOrderId == orderId);

                if (result == null)
                    return ApiResponseHelper.NotFound("لم يتم إضافة نتائج لهذا الطلب بعد");

                var labResult = new
                {
                    result.LabTestResultId,
                    Order = new
                    {
                        result.LabOrder.LabOrderId,
                        result.LabOrder.OrderCode,
                        Test = new
                        {
                            result.LabOrder.Test.TestName,
                            result.LabOrder.Test.TestCode
                        }
                    },
                    result.ResultSummary,
                    ResultData = !string.IsNullOrEmpty((string?)result.ResultDataJson) ?
                        JsonSerializer.Deserialize<object>((string?)result.ResultDataJson) : null,
                    result.NormalRanges,
                    result.Interpretations,
                    result.TechnicianNotes,
                    result.VerifiedBy,
                    result.CreatedAt,
                    FullReport = result.FullReportMedFile != null ? new
                    {
                        result.FullReportMedFile.MedFileId,
                        result.FullReportMedFile.MedFileName,
                        result.FullReportMedFile.ContentType
                    } : null
                };

                return ApiResponseHelper.Success(labResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lab result for order {OrderId}", orderId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع نتائج الفحص");
            }
        }

        public async Task<ApiResponse> SubmitLabResultAsync(int orderId, SubmitLabResultDto resultDto, int currentUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var order = await _context.LabTestOrders
                    .Include(o => o.Patient)
                        .ThenInclude(p => p.User)
                    .Include(o => o.Test)
                    .FirstOrDefaultAsync(o => o.LabOrderId == orderId);

                if (order == null)
                    return ApiResponseHelper.NotFound("طلب المختبر غير موجود");

                // Check if order is ready for results
                if (order.Status != "Processing")
                    return ApiResponseHelper.Error("لا يمكن إضافة نتائج إلا للطلبات قيد المعالجة", 400);

                // Check if result already exists
                var existingResult = await _context.LabTestResults
                    .FirstOrDefaultAsync(r => r.LabOrderId == orderId);

                if (existingResult != null)
                    return ApiResponseHelper.Error("تم إضافة النتائج بالفعل لهذا الطلب", 400);

                // Validate med file if provided
                if (resultDto.FullReportMedFileId.HasValue)
                {
                    var medFile = await _context.MedFiles
                        .FirstOrDefaultAsync(m => m.MedFileId == resultDto.FullReportMedFileId.Value);

                    if (medFile == null)
                        return ApiResponseHelper.Error("ملف التقرير غير موجود", 400);
                }

                // Validate JSON data if provided
                if (!string.IsNullOrEmpty(resultDto.ResultDataJson))
                {
                    try
                    {
                        JsonSerializer.Deserialize<object>(resultDto.ResultDataJson);
                    }
                    catch
                    {
                        return ApiResponseHelper.Error("بيانات JSON غير صالحة", 400);
                    }
                }

                // Create lab result
                var labResult = new LabTestResult
                {
                    LabOrderId = orderId,
                    ResultSummary = resultDto.ResultSummary,
                    ResultDataJson = resultDto.ResultDataJson,
                    NormalRanges = resultDto.NormalRanges,
                    Interpretations = resultDto.Interpretations,
                    FullReportMedFileId = resultDto.FullReportMedFileId,
                    TechnicianNotes = resultDto.TechnicianNotes,
                    VerifiedBy = resultDto.VerifiedBy,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.LabTestResults.AddAsync(labResult);

                // Update order status to completed
                order.Status = "Completed";

                await _context.SaveChangesAsync();

                // Create notification for patient
                var notification = new Notification
                {
                    UserId = order.Patient.UserId,
                    Title = "نتائج الفحص متاحة",
                    Message = $"نتائج فحص {order.Test.TestName} متاحة الآن. الرقم: {order.OrderCode}",
                    Channel = "InApp",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Notifications.AddAsync(notification);

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Submit Lab Result",
                    Entity = "LabTestResult",
                    EntityId = labResult.LabTestResultId.ToString(),
                    Details = $"Submitted lab result for order {order.OrderCode}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Lab result submitted for order {OrderId}", orderId);

                return ApiResponseHelper.Success(new
                {
                    LabResultId = labResult.LabTestResultId,
                    OrderId = orderId,
                    OrderCode = order.OrderCode,
                    TestName = order.Test.TestName
                }, "تم إضافة نتائج الفحص بنجاح");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error submitting lab result for order {OrderId}", orderId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء إضافة نتائج الفحص");
            }
        }

        public async Task<ApiResponse> UpdateLabResultAsync(int resultId, UpdateLabResultDto resultDto, int currentUserId)
        {
            try
            {
                var labResult = await _context.LabTestResults
                    .Include(r => r.LabOrder)
                        .ThenInclude(o => o.Test)
                    .FirstOrDefaultAsync(r => r.LabTestResultId == resultId);

                if (labResult == null)
                    return ApiResponseHelper.NotFound("نتيجة الفحص غير موجودة");

                // Update fields if provided
                if (!string.IsNullOrEmpty(resultDto.ResultSummary))
                    labResult.ResultSummary = resultDto.ResultSummary;

                if (!string.IsNullOrEmpty(resultDto.ResultDataJson))
                {
                    try
                    {
                        JsonSerializer.Deserialize<object>(resultDto.ResultDataJson);
                        labResult.ResultDataJson = resultDto.ResultDataJson;
                    }
                    catch
                    {
                        return ApiResponseHelper.Error("بيانات JSON غير صالحة", 400);
                    }
                }

                if (!string.IsNullOrEmpty(resultDto.NormalRanges))
                    labResult.NormalRanges = resultDto.NormalRanges;

                if (!string.IsNullOrEmpty(resultDto.Interpretations))
                    labResult.Interpretations = resultDto.Interpretations;

                if (resultDto.FullReportMedFileId.HasValue)
                {
                    var medFile = await _context.MedFiles
                        .FirstOrDefaultAsync(m => m.MedFileId == resultDto.FullReportMedFileId.Value);

                    if (medFile == null)
                        return ApiResponseHelper.Error("ملف التقرير غير موجود", 400);

                    labResult.FullReportMedFileId = resultDto.FullReportMedFileId.Value;
                }

                if (!string.IsNullOrEmpty(resultDto.TechnicianNotes))
                    labResult.TechnicianNotes = resultDto.TechnicianNotes;

                if (!string.IsNullOrEmpty(resultDto.VerifiedBy))
                    labResult.VerifiedBy = resultDto.VerifiedBy;

                await _context.SaveChangesAsync();

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Update Lab Result",
                    Entity = "LabTestResult",
                    EntityId = resultId.ToString(),
                    Details = $"Updated lab result for order {labResult.LabOrder.OrderCode}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);
                await _context.SaveChangesAsync();

                return ApiResponseHelper.Success(new
                {
                    labResult.LabTestResultId,
                    OrderId = labResult.LabOrderId,
                    OrderCode = labResult.LabOrder.OrderCode,
                    TestName = labResult.LabOrder.Test.TestName
                }, "تم تحديث نتائج الفحص بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lab result {ResultId}", resultId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء تحديث نتائج الفحص");
            }
        }

        #endregion

        #region Patient Lab History

        public async Task<ApiResponse> GetPatientLabHistoryAsync(int patientId, int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.LabTestOrders
                    .Include(o => o.Provider)
                    .Include(o => o.Test)
                    .Include(o => o.LabTestResult)
                    .Where(o => o.PatientId == patientId)
                    .OrderByDescending(o => o.CreatedAt)
                    .AsQueryable();

                var totalCount = await query.CountAsync();

                var history = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(o => new
                    {
                        o.LabOrderId,
                        o.OrderCode,
                        Provider = new
                        {
                            o.Provider.ProviderId,
                            o.Provider.ProviderName
                        },
                        Test = new
                        {
                            o.Test.TestName,
                            o.Test.TestCode
                        },
                        o.Status,
                        o.PriceSyp,
                        o.ScheduledAt,
                        o.CreatedAt,
                        HasResult = o.LabTestResult != null,
                        ResultSummary = o.LabTestResult != null ? o.LabTestResult.ResultSummary : null
                    })
                    .ToListAsync();

                return ApiResponseHelper.Success(new
                {
                    History = history,
                    Pagination = new
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lab history for patient {PatientId}", patientId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع سجل الفحوصات");
            }
        }

        #endregion

        #region Provider Dashboard

        public async Task<ApiResponse> GetLaboratoryDashboardAsync(int providerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var provider = await _context.Providers
                    .FirstOrDefaultAsync(p => p.ProviderId == providerId);

                if (provider == null)
                    return ApiResponseHelper.NotFound("مقدم الخدمة غير موجود");

                startDate ??= DateTime.UtcNow.AddDays(-30);
                endDate ??= DateTime.UtcNow;

                // Get total tests count
                var totalTests = await _context.LabTests
                    .CountAsync(lt => lt.ProviderId == providerId);

                // Get orders statistics
                var ordersQuery = _context.LabTestOrders
                    .Where(o => o.ProviderId == providerId &&
                               o.CreatedAt >= startDate &&
                               o.CreatedAt <= endDate);

                var totalOrders = await ordersQuery.CountAsync();
                var completedOrders = await ordersQuery.CountAsync(o => o.Status == "Completed");
                var pendingOrders = await ordersQuery.CountAsync(o => o.Status == "Pending" || o.Status == "Confirmed");
                var processingOrders = await ordersQuery.CountAsync(o => o.Status == "SampleCollected" || o.Status == "Processing");
                var cancelledOrders = await ordersQuery.CountAsync(o => o.Status == "Cancelled");

                // Get revenue
                var revenueSYP = await ordersQuery
                    .Where(o => o.Status == "Completed")
                    .SumAsync(o => o.PriceSyp);

                var revenueUSD = await ordersQuery
                    .Where(o => o.Status == "Completed")
                    .SumAsync(o => o.PriceUsd);

                // Get home collection statistics
                var homeCollectionOrders = await ordersQuery
                    .CountAsync(o => o.IsHomeCollection);

                // Get top tests
                var topTests = await ordersQuery
                    .Include(o => o.Test)
                    .GroupBy(o => new { o.TestId, o.Test.TestName })
                    .Select(g => new
                    {
                        TestId = g.Key.TestId,
                        TestName = g.Key.TestName,
                        OrderCount = g.Count(),
                        TotalRevenueSYP = g.Sum(o => o.PriceSyp)
                    })
                    .OrderByDescending(x => x.OrderCount)
                    .Take(10)
                    .ToListAsync();

                // Get recent orders
                var recentOrders = await _context.LabTestOrders
                    .Include(o => o.Patient.User)
                    .Include(o => o.Test)
                    .Where(o => o.ProviderId == providerId)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(10)
                    .Select(o => new
                    {
                        o.LabOrderId,
                        o.OrderCode,
                        PatientName = o.Patient.User.FullName,
                        TestName = o.Test.TestName,
                        o.Status,
                        o.PriceSyp,
                        o.CreatedAt
                    })
                    .ToListAsync();

                // Get pending results count
                var pendingResults = await ordersQuery
                    .CountAsync(o => o.Status == "Processing" && o.LabTestResult == null);

                return ApiResponseHelper.Success(new
                {
                    ProviderId = providerId,
                    ProviderName = provider.ProviderName,
                    Period = new { StartDate = startDate, EndDate = endDate },
                    Tests = new
                    {
                        Total = totalTests
                    },
                    Orders = new
                    {
                        Total = totalOrders,
                        Completed = completedOrders,
                        Pending = pendingOrders,
                        Processing = processingOrders,
                        Cancelled = cancelledOrders,
                        HomeCollection = homeCollectionOrders,
                        CompletionRate = totalOrders > 0 ? (double)completedOrders / totalOrders * 100 : 0
                    },
                    Results = new
                    {
                        Pending = pendingResults
                    },
                    Revenue = new
                    {
                        SYP = revenueSYP,
                        USD = revenueUSD
                    },
                    TopTests = topTests,
                    RecentOrders = recentOrders
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving laboratory dashboard for provider {ProviderId}", providerId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع لوحة التحكم");
            }
        }

        #endregion

        #region Helper Methods

        private string GenerateLabOrderCode()
        {
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            var randomPart = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            return $"LAB-{datePart}-{randomPart}";
        }

        #endregion
    }
}