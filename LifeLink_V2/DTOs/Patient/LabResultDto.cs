namespace LifeLink_V2.DTOs.Patient
{
    public class LabResultDto
    {
        public int LabOrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public IEnumerable<object>? Results { get; set; }
    }
}