using Microsoft.EntityFrameworkCore;
using LifeLink_V2.Data;
using LifeLink_V2.Models;
using LifeLink_V2.Helpers;
using LifeLink_V2.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace LifeLink_V2.Services.Implementations
{
    public class PharmacyService : IPharmacyService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PharmacyService> _logger;

        public PharmacyService(AppDbContext context, ILogger<PharmacyService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Medicine Management

        public async Task<ApiResponse> GetMedicinesAsync(int providerId, string? search = null, int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.Medicines
                    .Include(m => m.Provider)
                    .Where(m => m.ProviderId == providerId)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    query = query.Where(m =>
                        m.MedicineName.ToLower().Contains(search) ||
                        (m.Dosage != null && m.Dosage.ToLower().Contains(search))); 
                      //  (m.Manufacturer != null && m.Manufacturer.ToLower().Contains(search)));
                }

                // Get total count for pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var medicines = await query
                    .OrderBy(m => m.MedicineName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(m => new
                    {
                        m.MedicineId,
                        m.MedicineName,
                        m.Dosage,
                       // m.Manufacturer,
                        m.PriceSyp,
                        m.PriceUsd,
                        m.QuantityInStock,
                        m.LowStockThreshold,
                        m.Description,
                        m.CreatedAt,
                        Provider = new
                        {
                            m.Provider.ProviderId,
                            m.Provider.ProviderName
                        }
                    })
                    .ToListAsync();

                return ApiResponseHelper.Success(new
                {
                    Medicines = medicines,
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
                _logger.LogError(ex, "Error retrieving medicines for provider {ProviderId}", providerId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع الأدوية");
            }
        }

        public async Task<ApiResponse> GetMedicineByIdAsync(int medicineId)
        {
            try
            {
                var medicine = await _context.Medicines
                    .Include(m => m.Provider)
                    .FirstOrDefaultAsync(m => m.MedicineId == medicineId);

                if (medicine == null)
                    return ApiResponseHelper.NotFound("الدواء غير موجود");

                var result = new
                {
                    medicine.MedicineId,
                    medicine.MedicineName,
                    medicine.Dosage,
                   // medicine.Manufacturer,
                    medicine.PriceSyp,
                    medicine.PriceUsd,
                    medicine.QuantityInStock,
                    medicine.LowStockThreshold,
                    medicine.Description,
                    medicine.CreatedAt,
                    Provider = new
                    {
                        medicine.Provider.ProviderId,
                        medicine.Provider.ProviderName,
                        medicine.Provider.Phone,
                        medicine.Provider.Email
                    }
                };

                return ApiResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving medicine with ID {MedicineId}", medicineId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع بيانات الدواء");
            }
        }

        public async Task<ApiResponse> AddMedicineAsync(AddMedicineDto medicineDto, int currentUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Validate provider exists and is a pharmacy
                var provider = await _context.Providers
                    .Include(p => p.ProviderType)
                    .FirstOrDefaultAsync(p => p.ProviderId == medicineDto.ProviderId);

                if (provider == null)
                    return ApiResponseHelper.NotFound("مقدم الخدمة غير موجود");

                if (provider.ProviderType.ProviderTypeName != "Pharmacy")
                    return ApiResponseHelper.Error("هذا المقدم ليس صيدلية", 400);

                // Check if medicine with same name already exists for this provider
                var existingMedicine = await _context.Medicines
                    .FirstOrDefaultAsync(m => m.ProviderId == medicineDto.ProviderId &&
                                           m.MedicineName.ToLower() == medicineDto.MedicineName.ToLower());

                if (existingMedicine != null)
                    return ApiResponseHelper.Error("هذا الدواء موجود بالفعل في هذه الصيدلية", 400);

                // Calculate exchange rate if needed
                decimal? exchangeRate = null;
                if (medicineDto.PriceSYP > 0 && medicineDto.PriceUSD > 0)
                {
                    exchangeRate = medicineDto.PriceSYP / medicineDto.PriceUSD;
                }

                // Create medicine
                var medicine = new Medicine
                {
                    ProviderId = medicineDto.ProviderId,
                    MedicineName = medicineDto.MedicineName,
                    Dosage = medicineDto.Dosage,
                   // Manufacturer = medicineDto.Manufacturer,
                    PriceSyp = medicineDto.PriceSYP,
                    PriceUsd = medicineDto.PriceUSD,
                    QuantityInStock = medicineDto.QuantityInStock,
                    LowStockThreshold = medicineDto.LowStockThreshold ?? 10,
                    Description = medicineDto.Description,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Medicines.AddAsync(medicine);
                await _context.SaveChangesAsync();

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Add Medicine",
                    Entity = "Medicine",
                    EntityId = medicine.MedicineId.ToString(),
                    Details = $"Added medicine {medicine.MedicineName} with initial stock {medicine.QuantityInStock}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Medicine added: {MedicineName} with ID {MedicineId}",
                    medicine.MedicineName, medicine.MedicineId);

                return ApiResponseHelper.Success(new
                {
                    MedicineId = medicine.MedicineId,
                    medicine.MedicineName,
                    medicine.QuantityInStock,
                    medicine.PriceSyp,
                    medicine.PriceUsd
                }, "تم إضافة الدواء بنجاح");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding medicine");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء إضافة الدواء");
            }
        }

        public async Task<ApiResponse> UpdateMedicineAsync(int medicineId, UpdateMedicineDto medicineDto, int currentUserId)
        {
            try
            {
                var medicine = await _context.Medicines
                    .FirstOrDefaultAsync(m => m.MedicineId == medicineId);

                if (medicine == null)
                    return ApiResponseHelper.NotFound("الدواء غير موجود");

                // Update fields if provided
                if (!string.IsNullOrEmpty(medicineDto.MedicineName))
                    medicine.MedicineName = medicineDto.MedicineName;

                if (!string.IsNullOrEmpty(medicineDto.Dosage))
                    medicine.Dosage = medicineDto.Dosage;

                if (!string.IsNullOrEmpty(medicineDto.Manufacturer))
                   // medicine.Manufacturer = medicineDto.Manufacturer;

                if (medicineDto.PriceSYP.HasValue)
                    medicine.PriceSyp = medicineDto.PriceSYP.Value;

                if (medicineDto.PriceUSD.HasValue)
                    medicine.PriceUsd = medicineDto.PriceUSD.Value;

                if (medicineDto.QuantityInStock.HasValue)
                    medicine.QuantityInStock = medicineDto.QuantityInStock.Value;

                if (medicineDto.LowStockThreshold.HasValue)
                    medicine.LowStockThreshold = medicineDto.LowStockThreshold.Value;

                if (!string.IsNullOrEmpty(medicineDto.Description))
                    medicine.Description = medicineDto.Description;

                await _context.SaveChangesAsync();

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Update Medicine",
                    Entity = "Medicine",
                    EntityId = medicineId.ToString(),
                    Details = $"Updated medicine {medicine.MedicineName}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);
                await _context.SaveChangesAsync();

                return ApiResponseHelper.Success(new
                {
                    medicine.MedicineId,
                    medicine.MedicineName,
                    medicine.QuantityInStock,
                    medicine.PriceSyp,
                    medicine.PriceUsd
                }, "تم تحديث بيانات الدواء بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating medicine {MedicineId}", medicineId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء تحديث بيانات الدواء");
            }
        }

        public async Task<ApiResponse> UpdateMedicineStockAsync(int medicineId, int quantityChange, string reason, int currentUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var medicine = await _context.Medicines
                    .FirstOrDefaultAsync(m => m.MedicineId == medicineId);

                if (medicine == null)
                    return ApiResponseHelper.NotFound("الدواء غير موجود");

                // Calculate new quantity
                var newQuantity = medicine.QuantityInStock + quantityChange;

                if (newQuantity < 0)
                    return ApiResponseHelper.Error("الكمية الجديدة لا يمكن أن تكون سالبة", 400);

                var oldQuantity = medicine.QuantityInStock;
                medicine.QuantityInStock = newQuantity;

                await _context.SaveChangesAsync();

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = quantityChange > 0 ? "Add Stock" : "Reduce Stock",
                    Entity = "Medicine",
                    EntityId = medicineId.ToString(),
                    Details = $"Stock changed from {oldQuantity} to {newQuantity}. Reason: {reason}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Check if stock is low and create notification
                if (newQuantity <= medicine.LowStockThreshold)
                {
                    var provider = await _context.Providers
                        .FirstOrDefaultAsync(p => p.ProviderId == medicine.ProviderId);

                    if (provider != null)
                    {
                        var notification = new Notification
                        {
                            UserId = provider.UserId,
                            Title = "تحذير مخزون منخفض",
                            Message = $"مخزون الدواء {medicine.MedicineName} منخفض ({newQuantity} فقط متبقي). الحد الأدنى: {medicine.LowStockThreshold}",
                            Channel = "InApp",
                            CreatedAt = DateTime.UtcNow
                        };

                        await _context.Notifications.AddAsync(notification);
                        await _context.SaveChangesAsync();
                    }
                }

                return ApiResponseHelper.Success(new
                {
                    medicine.MedicineId,
                    medicine.MedicineName,
                    OldQuantity = oldQuantity,
                    NewQuantity = newQuantity,
                    QuantityChange = quantityChange
                }, "تم تحديث المخزون بنجاح");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating stock for medicine {MedicineId}", medicineId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء تحديث المخزون");
            }
        }

        public async Task<ApiResponse> DeleteMedicineAsync(int medicineId, int currentUserId)
        {
            try
            {
                var medicine = await _context.Medicines
                    .FirstOrDefaultAsync(m => m.MedicineId == medicineId);

                if (medicine == null)
                    return ApiResponseHelper.NotFound("الدواء غير موجود");

                // Check if medicine has any active orders
                var hasActiveOrders = await _context.PharmacyOrderItems
                    .AnyAsync(oi => oi.MedicineId == medicineId &&
                                    oi.PharmacyOrder.Status != "Cancelled" &&
                                    oi.PharmacyOrder.Status != "Completed");

                if (hasActiveOrders)
                    return ApiResponseHelper.Error("لا يمكن حذف الدواء لأنه مرتبط بطلبات نشطة", 400);

                // Soft delete (or hard delete based on your requirements)
                _context.Medicines.Remove(medicine);

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Delete Medicine",
                    Entity = "Medicine",
                    EntityId = medicineId.ToString(),
                    Details = $"Deleted medicine {medicine.MedicineName}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);
                await _context.SaveChangesAsync();

                return ApiResponseHelper.Success(null, "تم حذف الدواء بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting medicine {MedicineId}", medicineId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء حذف الدواء");
            }
        }

        #endregion

        #region Pharmacy Orders

        public async Task<ApiResponse> GetPharmacyOrdersAsync(int? patientId = null, int? providerId = null,
            string? status = null, DateTime? dateFrom = null, DateTime? dateTo = null,
            int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.PharmacyOrders
                    .Include(o => o.Patient)
                        .ThenInclude(p => p.User)
                    .Include(o => o.Provider)
                    .Include(o => o.PharmacyOrderItems)
                        .ThenInclude(oi => oi.Medicine)
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
                        o.PharmacyOrderId,
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
                        o.Status,
                        o.TotalSyp,
                        o.TotalUsd,
                        ItemsCount = o.PharmacyOrderItems.Count,
                        Items = o.PharmacyOrderItems.Select(oi => new
                        {
                            oi.MedicineId,
                            oi.Medicine.MedicineName,
                            oi.Quantity,
                            oi.UnitPriceSyp,
                            oi.LineTotalSyp
                        }).ToList(),
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
                _logger.LogError(ex, "Error retrieving pharmacy orders");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع طلبات الصيدلية");
            }
        }

        public async Task<ApiResponse> GetPharmacyOrderByIdAsync(int orderId)
        {
            try
            {
                var order = await _context.PharmacyOrders
                    .Include(o => o.Patient)
                        .ThenInclude(p => p.User)
                    .Include(o => o.Provider)
                    .Include(o => o.PharmacyOrderItems)
                        .ThenInclude(oi => oi.Medicine)
                    .FirstOrDefaultAsync(o => o.PharmacyOrderId == orderId);

                if (order == null)
                    return ApiResponseHelper.NotFound("طلب الصيدلية غير موجود");

                var result = new
                {
                    order.PharmacyOrderId,
                    order.OrderCode,
                    Patient = new
                    {
                        order.Patient.PatientId,
                        order.Patient.User.FullName,
                        order.Patient.User.Email,
                        order.Patient.User.Phone,
                        order.Patient.NationalId
                    },
                    Provider = new
                    {
                        order.Provider.ProviderId,
                        order.Provider.ProviderName,
                        order.Provider.Address,
                        order.Provider.Phone,
                        order.Provider.Email
                    },
                    order.Status,
                    order.TotalSyp,
                   // order.TotalUsd,
                   // order.DeliveryAddress,
                   // order.DeliveryPhone,
///order.IsDelivery,
                   // order.Notes,
                    Items = order.PharmacyOrderItems.Select(oi => new
                    {
                        oi.PharmacyOrderItemId,
                        oi.MedicineId,
                        Medicine = new
                        {
                            oi.Medicine.MedicineName,
                            oi.Medicine.Dosage,
                           // oi.Medicine.Manufacturer
                        },
                        oi.Quantity,
                        oi.UnitPriceSyp,
                        oi.UnitPriceUsd,
                        oi.LineTotalSyp,
                        oi.Instructions
                    }).ToList(),
                    order.CreatedAt
                };

                return ApiResponseHelper.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pharmacy order with ID {OrderId}", orderId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع بيانات طلب الصيدلية");
            }
        }

        public async Task<ApiResponse> CreatePharmacyOrderAsync(CreatePharmacyOrderDto orderDto, int currentUserId)
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

                // Validate provider exists and is a pharmacy
                var provider = await _context.Providers
                    .Include(p => p.ProviderType)
                    .FirstOrDefaultAsync(p => p.ProviderId == orderDto.ProviderId);

                if (provider == null)
                    return ApiResponseHelper.NotFound("مقدم الخدمة غير موجود");

                if (provider.ProviderType.ProviderTypeName != "Pharmacy")
                    return ApiResponseHelper.Error("هذا المقدم ليس صيدلية", 400);

                // Validate items
                if (orderDto.Items == null || !orderDto.Items.Any())
                    return ApiResponseHelper.Error("يجب إضافة أدوية إلى الطلب", 400);

                // Check stock availability and calculate totals
                decimal totalSYP = 0;
                decimal totalUSD = 0;
                var orderItems = new List<PharmacyOrderItem>();
                var stockUpdates = new List<(int MedicineId, int Quantity)>();

                foreach (var itemDto in orderDto.Items)
                {
                    var medicine = await _context.Medicines
                        .FirstOrDefaultAsync(m => m.MedicineId == itemDto.MedicineId &&
                                                m.ProviderId == orderDto.ProviderId);

                    if (medicine == null)
                        return ApiResponseHelper.Error($"الدواء برقم {itemDto.MedicineId} غير موجود في هذه الصيدلية", 400);

                    if (medicine.QuantityInStock < itemDto.Quantity)
                        return ApiResponseHelper.Error($"الدواء {medicine.MedicineName} غير متوفر بالكمية المطلوبة. المخزون المتاح: {medicine.QuantityInStock}", 400);

                    var lineTotalSYP = itemDto.Quantity * itemDto.UnitPriceSYP;
                    var lineTotalUSD = itemDto.Quantity * itemDto.UnitPriceUSD;

                    totalSYP += lineTotalSYP;
                    totalUSD += lineTotalUSD;

                    var orderItem = new PharmacyOrderItem
                    {
                        MedicineId = itemDto.MedicineId,
                        Quantity = itemDto.Quantity,
                        UnitPriceSyp = itemDto.UnitPriceSYP,
                        UnitPriceUsd = itemDto.UnitPriceUSD,
                        LineTotalSyp = lineTotalSYP,
                        Instructions = itemDto.Instructions
                    };

                    orderItems.Add(orderItem);
                    stockUpdates.Add((itemDto.MedicineId, -itemDto.Quantity));
                }

                // Generate unique order code
                var orderCode = GenerateOrderCode();

                // Create order
                var order = new PharmacyOrder
                {
                    OrderCode = orderCode,
                    PatientId = orderDto.PatientId,
                    ProviderId = orderDto.ProviderId,
                    Status = "Pending",
                    TotalSyp = totalSYP,
                    TotalUsd = totalUSD,
                   // DeliveryAddress = orderDto.DeliveryAddress,
                  //  DeliveryPhone = orderDto.DeliveryPhone ?? patient.User.Phone,
                   // IsDelivery = orderDto.IsDelivery,
                   // Notes = orderDto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                // Add order to context first to get ID
                await _context.PharmacyOrders.AddAsync(order);
                await _context.SaveChangesAsync();

                // Add order items with the order ID
                foreach (var orderItem in orderItems)
                {
                    orderItem.PharmacyOrderId = order.PharmacyOrderId;
                    await _context.PharmacyOrderItems.AddAsync(orderItem);
                }

                // Update stock quantities
                foreach (var (medicineId, quantityChange) in stockUpdates)
                {
                    var medicine = await _context.Medicines.FindAsync(medicineId);
                    if (medicine != null)
                    {
                        medicine.QuantityInStock += quantityChange;
                    }
                }

                // Create notification for patient
                var patientNotification = new Notification
                {
                    UserId = patient.UserId,
                    Title = "طلب صيدلية جديد",
                    Message = $"تم إنشاء طلب صيدلية جديد برقم {orderCode}. المجموع: {totalSYP} ل.س",
                    Channel = "InApp",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Notifications.AddAsync(patientNotification);

                // Create notification for provider
                var providerNotification = new Notification
                {
                    UserId = provider.UserId,
                    Title = "طلب صيدلية جديد",
                    Message = $"طلب صيدلية جديد من المريض {patient.User.FullName}. الرقم: {orderCode}",
                    Channel = "InApp",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Notifications.AddAsync(providerNotification);

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Create Pharmacy Order",
                    Entity = "PharmacyOrder",
                    EntityId = order.PharmacyOrderId.ToString(),
                    Details = $"Created pharmacy order {orderCode} for patient {patient.User.FullName}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Pharmacy order created: {OrderCode} for patient {PatientId}",
                    order.OrderCode, order.PatientId);

                return ApiResponseHelper.Success(new
                {
                    OrderId = order.PharmacyOrderId,
                    OrderCode = order.OrderCode,
                    Status = order.Status,
                    TotalSYP = order.TotalSyp,
                    TotalUSD = order.TotalUsd,
                    ItemsCount = orderItems.Count
                }, "تم إنشاء طلب الصيدلية بنجاح");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating pharmacy order");
                return ApiResponseHelper.InternalError("حدث خطأ أثناء إنشاء طلب الصيدلية");
            }
        }

        public async Task<ApiResponse> UpdatePharmacyOrderStatusAsync(int orderId, string status, int currentUserId)
        {
            try
            {
                var order = await _context.PharmacyOrders
                    .Include(o => o.Patient)
                        .ThenInclude(p => p.User)
                    .Include(o => o.Provider)
                    .FirstOrDefaultAsync(o => o.PharmacyOrderId == orderId);

                if (order == null)
                    return ApiResponseHelper.NotFound("طلب الصيدلية غير موجود");

                // Validate status
                var validStatuses = new[] { "Pending", "Confirmed", "Processing", "ReadyForPickup", "OutForDelivery", "Delivered", "Completed", "Cancelled" };
                if (!validStatuses.Contains(status))
                    return ApiResponseHelper.Error("حالة غير صالحة", 400);

                var oldStatus = order.Status;
                order.Status = status;

                await _context.SaveChangesAsync();

                // Create notification for status change
                var notification = new Notification
                {
                    UserId = order.Patient.UserId,
                    Title = $"تغيير حالة طلب الصيدلية",
                    Message = $"تم تغيير حالة طلب الصيدلية {order.OrderCode} من {oldStatus} إلى {status}",
                    Channel = "InApp",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Notifications.AddAsync(notification);

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Update Pharmacy Order Status",
                    Entity = "PharmacyOrder",
                    EntityId = orderId.ToString(),
                    Details = $"Changed pharmacy order status from {oldStatus} to {status}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);
                await _context.SaveChangesAsync();

                return ApiResponseHelper.Success(new
                {
                    order.PharmacyOrderId,
                    order.OrderCode,
                    OldStatus = oldStatus,
                    NewStatus = status
                }, "تم تحديث حالة الطلب بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating pharmacy order status for ID {OrderId}", orderId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء تحديث حالة الطلب");
            }
        }

        public async Task<ApiResponse> CancelPharmacyOrderAsync(int orderId, int currentUserId, string reason = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var order = await _context.PharmacyOrders
                    .Include(o => o.Patient)
                        .ThenInclude(p => p.User)
                    .Include(o => o.PharmacyOrderItems)
                    .FirstOrDefaultAsync(o => o.PharmacyOrderId == orderId);

                if (order == null)
                    return ApiResponseHelper.NotFound("طلب الصيدلية غير موجود");

                // Check if order can be cancelled
                if (order.Status == "Cancelled")
                    return ApiResponseHelper.Error("الطلب ملغي بالفعل", 400);

                if (order.Status == "Completed" || order.Status == "Delivered")
                    return ApiResponseHelper.Error("لا يمكن إلغاء طلب مكتمل أو تم تسليمه", 400);

                var oldStatus = order.Status;
                order.Status = "Cancelled";

                // Return items to stock
                foreach (var item in order.PharmacyOrderItems)
                {
                    var medicine = await _context.Medicines.FindAsync(item.MedicineId);
                    if (medicine != null)
                    {
                        medicine.QuantityInStock += item.Quantity;
                    }
                }

                await _context.SaveChangesAsync();

                // Create notification for cancellation
                var notification = new Notification
                {
                    UserId = order.Patient.UserId,
                    Title = "تم إلغاء طلب الصيدلية",
                    Message = $"تم إلغاء طلب الصيدلية {order.OrderCode}" +
                             (reason != null ? $"\nالسبب: {reason}" : ""),
                    Channel = "InApp",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Notifications.AddAsync(notification);

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Cancel Pharmacy Order",
                    Entity = "PharmacyOrder",
                    EntityId = orderId.ToString(),
                    Details = $"Cancelled pharmacy order {order.OrderCode}. Reason: {reason}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ApiResponseHelper.Success(new
                {
                    order.PharmacyOrderId,
                    order.OrderCode,
                    Status = "Cancelled",
                    CancellationReason = reason
                }, "تم إلغاء الطلب بنجاح");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error cancelling pharmacy order {OrderId}", orderId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء إلغاء الطلب");
            }
        }

        public async Task<ApiResponse> CompletePharmacyOrderAsync(int orderId, int currentUserId)
        {
            try
            {
                var order = await _context.PharmacyOrders
                    .Include(o => o.Patient)
                        .ThenInclude(p => p.User)
                    .FirstOrDefaultAsync(o => o.PharmacyOrderId == orderId);

                if (order == null)
                    return ApiResponseHelper.NotFound("طلب الصيدلية غير موجود");

                // Check if order can be marked as completed
                if (order.Status != "Delivered" && order.Status != "ReadyForPickup")
                    return ApiResponseHelper.Error("يمكن إكمال الطلب المسلّم أو الجاهز للاستلام فقط", 400);

                var oldStatus = order.Status;
                order.Status = "Completed";

                await _context.SaveChangesAsync();

                // Create notification for completion
                var notification = new Notification
                {
                    UserId = order.Patient.UserId,
                    Title = "تم إكمال طلب الصيدلية",
                    Message = $"تم إكمال طلب الصيدلية {order.OrderCode}",
                    Channel = "InApp",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Notifications.AddAsync(notification);

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Complete Pharmacy Order",
                    Entity = "PharmacyOrder",
                    EntityId = orderId.ToString(),
                    Details = $"Completed pharmacy order {order.OrderCode}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);
                await _context.SaveChangesAsync();

                return ApiResponseHelper.Success(new
                {
                    order.PharmacyOrderId,
                    order.OrderCode,
                    Status = "Completed"
                }, "تم إكمال الطلب بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing pharmacy order {OrderId}", orderId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء إكمال الطلب");
            }
        }

        #endregion

        #region Order Items

        public async Task<ApiResponse> AddItemToPharmacyOrderAsync(int orderId, AddPharmacyOrderItemDto itemDto, int currentUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var order = await _context.PharmacyOrders
                    .Include(o => o.PharmacyOrderItems)
                    .FirstOrDefaultAsync(o => o.PharmacyOrderId == orderId);

                if (order == null)
                    return ApiResponseHelper.NotFound("طلب الصيدلية غير موجود");

                // Check if order can be modified
                if (order.Status != "Pending" && order.Status != "Confirmed")
                    return ApiResponseHelper.Error("لا يمكن تعديل الطلب في هذه الحالة", 400);

                // Validate medicine exists and is from same provider
                var medicine = await _context.Medicines
                    .FirstOrDefaultAsync(m => m.MedicineId == itemDto.MedicineId &&
                                            m.ProviderId == order.ProviderId);

                if (medicine == null)
                    return ApiResponseHelper.Error("الدواء غير موجود في هذه الصيدلية", 400);

                // Check stock availability
                if (medicine.QuantityInStock < itemDto.Quantity)
                    return ApiResponseHelper.Error($"الدواء {medicine.MedicineName} غير متوفر بالكمية المطلوبة. المخزون المتاح: {medicine.QuantityInStock}", 400);

                // Check if medicine already exists in order
                var existingItem = order.PharmacyOrderItems
                    .FirstOrDefault(oi => oi.MedicineId == itemDto.MedicineId);

                if (existingItem != null)
                {
                    // Update existing item
                    existingItem.Quantity += itemDto.Quantity;
                    existingItem.LineTotalSyp += itemDto.Quantity * itemDto.UnitPriceSYP;
                }
                else
                {
                    // Create new item
                    var orderItem = new PharmacyOrderItem
                    {
                        PharmacyOrderId = orderId,
                        MedicineId = itemDto.MedicineId,
                        Quantity = itemDto.Quantity,
                        UnitPriceSyp = itemDto.UnitPriceSYP,
                        UnitPriceUsd = itemDto.UnitPriceUSD,
                        LineTotalSyp = itemDto.Quantity * itemDto.UnitPriceSYP,
                        Instructions = itemDto.Instructions
                    };

                    await _context.PharmacyOrderItems.AddAsync(orderItem);
                }

                // Update order totals
                var itemTotalSYP = itemDto.Quantity * itemDto.UnitPriceSYP;
                var itemTotalUSD = itemDto.Quantity * itemDto.UnitPriceUSD;

                order.TotalSyp += itemTotalSYP;
                order.TotalUsd += itemTotalUSD;

                // Update stock
                medicine.QuantityInStock -= itemDto.Quantity;

                await _context.SaveChangesAsync();

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Add Item to Pharmacy Order",
                    Entity = "PharmacyOrder",
                    EntityId = orderId.ToString(),
                    Details = $"Added {itemDto.Quantity} of medicine {medicine.MedicineName} to order {order.OrderCode}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ApiResponseHelper.Success(new
                {
                    OrderId = orderId,
                    MedicineId = itemDto.MedicineId,
                    Quantity = itemDto.Quantity,
                    ItemTotalSYP = itemTotalSYP,
                    ItemTotalUSD = itemTotalUSD,
                    NewOrderTotalSYP = order.TotalSyp
                }, "تم إضافة الدواء إلى الطلب بنجاح");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding item to pharmacy order {OrderId}", orderId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء إضافة الدواء إلى الطلب");
            }
        }

        public async Task<ApiResponse> UpdatePharmacyOrderItemAsync(int orderId, int itemId, UpdatePharmacyOrderItemDto itemDto, int currentUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var order = await _context.PharmacyOrders
                    .Include(o => o.PharmacyOrderItems)
                    .FirstOrDefaultAsync(o => o.PharmacyOrderId == orderId);

                if (order == null)
                    return ApiResponseHelper.NotFound("طلب الصيدلية غير موجود");

                var orderItem = order.PharmacyOrderItems
                    .FirstOrDefault(oi => oi.PharmacyOrderItemId == itemId);

                if (orderItem == null)
                    return ApiResponseHelper.NotFound("عنصر الطلب غير موجود");

                // Check if order can be modified
                if (order.Status != "Pending" && order.Status != "Confirmed")
                    return ApiResponseHelper.Error("لا يمكن تعديل الطلب في هذه الحالة", 400);

                var medicine = await _context.Medicines
                    .FirstOrDefaultAsync(m => m.MedicineId == orderItem.MedicineId);

                if (medicine == null)
                    return ApiResponseHelper.Error("الدواء غير موجود", 400);

                // Calculate quantity difference
                int quantityDifference = 0;
                if (itemDto.Quantity.HasValue)
                {
                    quantityDifference = itemDto.Quantity.Value - orderItem.Quantity;

                    // Check stock availability for increase
                    if (quantityDifference > 0 && medicine.QuantityInStock < quantityDifference)
                        return ApiResponseHelper.Error($"الدواء {medicine.MedicineName} غير متوفر بالكمية المطلوبة. المخزون المتاح: {medicine.QuantityInStock}", 400);
                }

                // Update order totals (remove old values first)
                order.TotalSyp -= orderItem.LineTotalSyp;

                // Update item
                if (itemDto.Quantity.HasValue)
                    orderItem.Quantity = itemDto.Quantity.Value;

                if (itemDto.UnitPriceSYP.HasValue)
                    orderItem.UnitPriceSyp = itemDto.UnitPriceSYP.Value;

                if (itemDto.UnitPriceUSD.HasValue)
                    orderItem.UnitPriceUsd = itemDto.UnitPriceUSD.Value;

                if (!string.IsNullOrEmpty(itemDto.Instructions))
                    orderItem.Instructions = itemDto.Instructions;

                // Recalculate line total
                orderItem.LineTotalSyp = orderItem.Quantity * orderItem.UnitPriceSyp;

                // Add new line total to order total
                order.TotalSyp += orderItem.LineTotalSyp;

                // Update stock if quantity changed
                if (quantityDifference != 0)
                {
                    medicine.QuantityInStock -= quantityDifference;
                }

                await _context.SaveChangesAsync();

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Update Pharmacy Order Item",
                    Entity = "PharmacyOrder",
                    EntityId = orderId.ToString(),
                    Details = $"Updated item {medicine.MedicineName} in order {order.OrderCode}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ApiResponseHelper.Success(new
                {
                    OrderItemId = itemId,
                    MedicineId = orderItem.MedicineId,
                    Quantity = orderItem.Quantity,
                    UnitPriceSYP = orderItem.UnitPriceSyp,
                    LineTotalSYP = orderItem.LineTotalSyp,
                    NewOrderTotalSYP = order.TotalSyp
                }, "تم تحديث عنصر الطلب بنجاح");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating pharmacy order item {ItemId} in order {OrderId}", itemId, orderId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء تحديث عنصر الطلب");
            }
        }

        public async Task<ApiResponse> RemoveItemFromPharmacyOrderAsync(int orderId, int itemId, int currentUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var order = await _context.PharmacyOrders
                    .Include(o => o.PharmacyOrderItems)
                    .FirstOrDefaultAsync(o => o.PharmacyOrderId == orderId);

                if (order == null)
                    return ApiResponseHelper.NotFound("طلب الصيدلية غير موجود");

                var orderItem = order.PharmacyOrderItems
                    .FirstOrDefault(oi => oi.PharmacyOrderItemId == itemId);

                if (orderItem == null)
                    return ApiResponseHelper.NotFound("عنصر الطلب غير موجود");

                // Check if order can be modified
                if (order.Status != "Pending" && order.Status != "Confirmed")
                    return ApiResponseHelper.Error("لا يمكن تعديل الطلب في هذه الحالة", 400);

                var medicine = await _context.Medicines
                    .FirstOrDefaultAsync(m => m.MedicineId == orderItem.MedicineId);

                if (medicine != null)
                {
                    // Return stock
                    medicine.QuantityInStock += orderItem.Quantity;
                }

                // Update order total
                order.TotalSyp -= orderItem.LineTotalSyp;

                // Remove item
                _context.PharmacyOrderItems.Remove(orderItem);

                await _context.SaveChangesAsync();

                // Create activity log
                var activityLog = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = "Remove Item from Pharmacy Order",
                    Entity = "PharmacyOrder",
                    EntityId = orderId.ToString(),
                    Details = $"Removed item {medicine?.MedicineName} from order {order.OrderCode}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(activityLog);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ApiResponseHelper.Success(new
                {
                    OrderId = orderId,
                    ItemId = itemId,
                    NewOrderTotalSYP = order.TotalSyp
                }, "تم حذف الدواء من الطلب بنجاح");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error removing item {ItemId} from pharmacy order {OrderId}", itemId, orderId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء حذف الدواء من الطلب");
            }
        }

        #endregion

        #region Inventory Management

        public async Task<ApiResponse> GetLowStockMedicinesAsync(int providerId, int threshold = 10)
        {
            try
            {
                var medicines = await _context.Medicines
                    .Where(m => m.ProviderId == providerId &&
                                m.QuantityInStock <= (m.LowStockThreshold))
                    .OrderBy(m => m.QuantityInStock)
                    .Select(m => new
                    {
                        m.MedicineId,
                        m.MedicineName,
                        m.Dosage,
                        m.QuantityInStock,
                        m.LowStockThreshold,
                        m.PriceSyp,
                        Status = m.QuantityInStock == 0 ? "نفذ" : "منخفض"
                    })
                    .ToListAsync();

                return ApiResponseHelper.Success(new
                {
                    Medicines = medicines,
                    TotalCount = medicines.Count,
                    Threshold = threshold
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving low stock medicines for provider {ProviderId}", providerId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع الأدوية منخفضة المخزون");
            }
        }

        public async Task<ApiResponse> GetMedicineStockHistoryAsync(int medicineId, int page = 1, int pageSize = 20)
        {
            try
            {
                // This would typically come from a stock history table
                // For now, we'll get order history that affected this medicine
                var query = _context.PharmacyOrderItems
                    .Include(oi => oi.PharmacyOrder)
                    .Include(oi => oi.PharmacyOrder.Patient.User)
                    .Where(oi => oi.MedicineId == medicineId)
                    .OrderByDescending(oi => oi.PharmacyOrder.CreatedAt)
                    .AsQueryable();

                var totalCount = await query.CountAsync();

                var history = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(oi => new
                    {
                        Date = oi.PharmacyOrder.CreatedAt,
                        OrderCode = oi.PharmacyOrder.OrderCode,
                        QuantityChange = -oi.Quantity, // Negative because it reduces stock
                        Type = "Order",
                        Patient = oi.PharmacyOrder.Patient.User.FullName,
                        Status = oi.PharmacyOrder.Status
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
                _logger.LogError(ex, "Error retrieving stock history for medicine {MedicineId}", medicineId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع سجل المخزون");
            }
        }

        #endregion

        #region Provider Pharmacy Dashboard

        public async Task<ApiResponse> GetPharmacyDashboardAsync(int providerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var provider = await _context.Providers
                    .FirstOrDefaultAsync(p => p.ProviderId == providerId);

                if (provider == null)
                    return ApiResponseHelper.NotFound("مقدم الخدمة غير موجود");

                startDate ??= DateTime.UtcNow.AddDays(-30);
                endDate ??= DateTime.UtcNow;

                // Get total medicines count
                var totalMedicines = await _context.Medicines
                    .CountAsync(m => m.ProviderId == providerId);

                // Get low stock medicines count
                var lowStockMedicines = await _context.Medicines
                    .CountAsync(m => m.ProviderId == providerId &&
                                    m.QuantityInStock <= (m.LowStockThreshold ));

                // Get out of stock medicines count
                var outOfStockMedicines = await _context.Medicines
                    .CountAsync(m => m.ProviderId == providerId && m.QuantityInStock == 0);

                // Get orders statistics
                var ordersQuery = _context.PharmacyOrders
                    .Where(o => o.ProviderId == providerId &&
                               o.CreatedAt >= startDate &&
                               o.CreatedAt <= endDate);

                var totalOrders = await ordersQuery.CountAsync();
                var completedOrders = await ordersQuery.CountAsync(o => o.Status == "Completed");
                var pendingOrders = await ordersQuery.CountAsync(o => o.Status == "Pending" || o.Status == "Confirmed");
                var cancelledOrders = await ordersQuery.CountAsync(o => o.Status == "Cancelled");

                // Get revenue
                var revenueSYP = await ordersQuery
                    .Where(o => o.Status == "Completed")
                    .SumAsync(o => o.TotalSyp);

                var revenueUSD = await ordersQuery
                    .Where(o => o.Status == "Completed")
                    .SumAsync(o => o.TotalUsd);

                // Get top selling medicines
                var topMedicines = await _context.PharmacyOrderItems
                    .Include(oi => oi.Medicine)
                    .Include(oi => oi.PharmacyOrder)
                    .Where(oi => oi.PharmacyOrder.ProviderId == providerId &&
                                oi.PharmacyOrder.CreatedAt >= startDate &&
                                oi.PharmacyOrder.CreatedAt <= endDate)
                    .GroupBy(oi => new { oi.MedicineId, oi.Medicine.MedicineName })
                    .Select(g => new
                    {
                        MedicineId = g.Key.MedicineId,
                        MedicineName = g.Key.MedicineName,
                        TotalQuantity = g.Sum(oi => oi.Quantity),
                        TotalRevenueSYP = g.Sum(oi => oi.LineTotalSyp)
                    })
                    .OrderByDescending(x => x.TotalQuantity)
                    .Take(10)
                    .ToListAsync();

                // Get recent orders
                var recentOrders = await _context.PharmacyOrders
                    .Include(o => o.Patient.User)
                    .Where(o => o.ProviderId == providerId)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(10)
                    .Select(o => new
                    {
                        o.PharmacyOrderId,
                        o.OrderCode,
                        PatientName = o.Patient.User.FullName,
                        o.Status,
                        o.TotalSyp,
                        o.CreatedAt
                    })
                    .ToListAsync();

                return ApiResponseHelper.Success(new
                {
                    ProviderId = providerId,
                    ProviderName = provider.ProviderName,
                    Period = new { StartDate = startDate, EndDate = endDate },
                    Inventory = new
                    {
                        TotalMedicines = totalMedicines,
                        LowStockMedicines = lowStockMedicines,
                        OutOfStockMedicines = outOfStockMedicines
                    },
                    Orders = new
                    {
                        Total = totalOrders,
                        Completed = completedOrders,
                        Pending = pendingOrders,
                        Cancelled = cancelledOrders,
                        CompletionRate = totalOrders > 0 ? (double)completedOrders / totalOrders * 100 : 0
                    },
                    Revenue = new
                    {
                        SYP = revenueSYP,
                        USD = revenueUSD
                    },
                    TopSellingMedicines = topMedicines,
                    RecentOrders = recentOrders
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pharmacy dashboard for provider {ProviderId}", providerId);
                return ApiResponseHelper.InternalError("حدث خطأ أثناء استرجاع لوحة التحكم");
            }
        }

        #endregion

        #region Helper Methods

        private string GenerateOrderCode()
        {
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            var randomPart = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            return $"PHARM-{datePart}-{randomPart}";
        }

        #endregion
    }
}