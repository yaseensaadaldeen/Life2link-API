namespace LifeLink_V2.DTOs.Pharmacy
{
    public class UploadPrescriptionResponseDto
    {
        public bool Uploaded { get; set; }
        public IEnumerable<int>? PrescriptionIds { get; set; }
    }
}