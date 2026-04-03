namespace LifeLink_V2.DTOs.Pharmacy
{
    public class MedicineDto
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string? Manufacturer { get; set; }
        public decimal PriceSyp { get; set; }
    }
}