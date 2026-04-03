using System.ComponentModel.DataAnnotations;

namespace LifeLink_V2.DTOs.Discovery
{
    public class ClinicDto
    {
        public int ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public int? CityId { get; set; }
        public string? CityName { get; set; }
        public bool IsActive { get; set; }
    }
}