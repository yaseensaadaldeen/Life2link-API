namespace LifeLink_V2.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendWelcomeEmailAsync(string email, string name, string userType);
        Task SendProviderRegistrationEmailAsync(string email, string name, string providerName, string providerType);
        Task SendPasswordResetEmailAsync(string email, string name, string resetToken);

        // ADD THESE METHODS:
        Task SendPasswordResetConfirmationAsync(string email, string name);
        Task SendPasswordChangeNotificationAsync(string email, string name);
        Task SendAccountActivationEmailAsync(string email, string name);
    }
}