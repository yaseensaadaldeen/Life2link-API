namespace LifeLink_V2.DTOs.Patient
{
    public class PrescriptionSummaryDto
    {
        public int PrescriptionId { get; set; }
        public string PrescriptionCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalPriceSyp { get; set; }
    }
}