using System.ComponentModel.DataAnnotations;

namespace LifeLink_V2.DTOs.Pharmacy
{
    public class CreateOrderDto
    {
        [Required]
        public int PatientId { get; set; }

        [Required]
        public int PharmacyId { get; set; }

        [Required]
        public List<CreateOrderItemDto> Items { get; set; } = new();

        public string? Notes { get; set; }
    }

    public class CreateOrderItemDto
    {
        [Required]
        public int MedicineId { get; set; }

        [Required]
        [Range(1, 1000)]
        public int Quantity { get; set; }
    }
}