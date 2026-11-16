namespace GymManagementBLL.ViewModels.BookingViewModels;

public class MemberBookingViewModel
{
    public int SessionId { get; set; }
    public string Description { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string TrainerName { get; set; } = null!;
    public bool IsAttended { get; set; }
}
