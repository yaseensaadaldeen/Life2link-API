using System.ComponentModel.DataAnnotations;

namespace LifeLink_V2.DTOs.Discovery
{
    public class SlotDto
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public bool IsAvailable { get; set; }
    }
}