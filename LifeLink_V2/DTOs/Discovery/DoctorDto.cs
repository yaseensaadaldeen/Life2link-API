
using System.ComponentModel.DataAnnotations;

namespace LifeLink_V2.DTOs.Discovery
{
    public class DoctorDto
    {
        public int DoctorId { get; set; }
        public int ProviderId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int? SpecialtyId { get; set; }
        public string? SpecialtyName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? WorkingHours { get; set; }
        public bool IsActive { get; set; }
    }
}