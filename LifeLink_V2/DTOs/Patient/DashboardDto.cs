namespace LifeLink_V2.DTOs.Patient
{
    public class PatientDashboardDto
    {
        public int UpcomingAppointments { get; set; }
        public int PendingPrescriptions { get; set; }
        public int UnreadNotifications { get; set; }
        public decimal? OutstandingPaymentsSyp { get; set; }
    }
}