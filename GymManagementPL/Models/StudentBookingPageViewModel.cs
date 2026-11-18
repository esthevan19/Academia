using GymManagementBLL.ViewModels.BookingViewModels;
using GymManagementBLL.ViewModels.SessionViewModels;

namespace GymManagementPL.Models;

public sealed class StudentBookingPageViewModel
{
    public bool NeedsLink { get; set; }
    public bool HasActiveMembership { get; set; }
    public IReadOnlyList<SessionViewModel> AvailableSessions { get; set; } = Array.Empty<SessionViewModel>();
    public IReadOnlyList<MemberBookingViewModel> MyBookings { get; set; } = Array.Empty<MemberBookingViewModel>();
}
