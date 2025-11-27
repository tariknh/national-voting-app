namespace WebApplication1.Models
{
    // purpose of this model is to provide all the info needed for our homepage
    public class HomeViewModel
    {
        public string? FullName { get; set; }
        public string? Fodselsnr { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }

        // CORRECTED: This is now a standard property.
        // We will set its value in the HomeController.
        public bool IsLoggedIn { get; set; }

        public string? Kommune { get; set; }
        public bool HasVoted { get; set; }
    }
}