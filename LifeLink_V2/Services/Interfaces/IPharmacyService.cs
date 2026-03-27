using LifeLink_V2.Helpers;
using LifeLink_V2.Models;

namespace LifeLink_V2.Services.Interfaces
{
    public interface IPharmacyService
    {
        // Medicine Management
        Task<ApiResponse> GetMedicinesAsync(int providerId, string? search = null, int page = 1, int pageSize = 20);
        Task<ApiResponse> GetMedicineByIdAsync(int medicineId);
        Task<ApiResponse> AddMedicineAsync(AddMedicineDto medicineDto, int currentUserId);
        Task<ApiResponse> UpdateMedicineAsync(int medicineId, UpdateMedicineDto medicineDto, int currentUserId);
        Task<ApiResponse> UpdateMedicineStockAsync(int medicineId, int quantityChange, string reason, int currentUserId);
        Task<ApiResponse> DeleteMedicineAsync(int medicineId, int currentUserId);

        // Pharmacy Orders
        Task<ApiResponse> GetPharmacyOrdersAsync(int? patientId = null, int? providerId = null,
            string? status = null, DateTime? dateFrom = null, DateTime? dateTo = null,
            int page = 1, int pageSize = 20);
        Task<ApiResponse> GetPharmacyOrderByIdAsync(int orderId);
        Task<ApiResponse> CreatePharmacyOrderAsync(CreatePharmacyOrderDto orderDto, int currentUserId);
        Task<ApiResponse> UpdatePharmacyOrderStatusAsync(int orderId, string status, int currentUserId);
        Task<ApiResponse> CancelPharmacyOrderAsync(int orderId, int currentUserId, string reason = null);
        Task<ApiResponse> CompletePharmacyOrderAsync(int orderId, int currentUserId);

        // Order Items
        Task<ApiResponse> AddItemToPharmacyOrderAsync(int orderId, AddPharmacyOrderItemDto itemDto, int currentUserId);
        Task<ApiResponse> UpdatePharmacyOrderItemAsync(int orderId, int itemId, UpdatePharmacyOrderItemDto itemDto, int currentUserId);
        Task<ApiResponse> RemoveItemFromPharmacyOrderAsync(int orderId, int itemId, int currentUserId);

        // Inventory Management
        Task<ApiResponse> GetLowStockMedicinesAsync(int providerId, int threshold = 10);
        Task<ApiResponse> GetMedicineStockHistoryAsync(int medicineId, int page = 1, int pageSize = 20);

        // Provider Pharmacy Dashboard
        Task<ApiResponse> GetPharmacyDashboardAsync(int providerId, DateTime? startDate = null, DateTime? endDate = null);
    }

    public class AddMedicineDto
    {
        public int ProviderId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string? Dosage { get; set; }
        public string? Manufacturer { get; set; }
        public decimal PriceSYP { get; set; }
        public decimal PriceUSD { get; set; }
        public int QuantityInStock { get; set; }
        public int? LowStockThreshold { get; set; } = 10;
        public string? Description { get; set; }
    }

    public class UpdateMedicineDto
    {
        public string? MedicineName { get; set; }
        public string? Dosage { get; set; }
        public string? Manufacturer { get; set; }
        public decimal? PriceSYP { get; set; }
        public decimal? PriceUSD { get; set; }
        public int? QuantityInStock { get; set; }
        public int? LowStockThreshold { get; set; }
        public string? Description { get; set; }
    }

    public class CreatePharmacyOrderDto
    {
        public int PatientId { get; set; }
        public int ProviderId { get; set; }
        public string? Notes { get; set; }
        public string DeliveryAddress { get; set; } = string.Empty;
        public string? DeliveryPhone { get; set; }
        public bool IsDelivery { get; set; } = false;
        public List<AddPharmacyOrderItemDto> Items { get; set; } = new();
    }

    public class AddPharmacyOrderItemDto
    {
        public int MedicineId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPriceSYP { get; set; }
        public decimal UnitPriceUSD { get; set; }
        public string? Instructions { get; set; }
    }

    public class UpdatePharmacyOrderItemDto
    {
        public int? Quantity { get; set; }
        public decimal? UnitPriceSYP { get; set; }
        public decimal? UnitPriceUSD { get; set; }
        public string? Instructions { get; set; }
    }
}