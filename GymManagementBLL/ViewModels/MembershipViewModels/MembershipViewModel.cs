namespace GymManagementBLL.ViewModels.MembershipViewModels
{
	public class MembershipViewModel
	{
		public int MemberId { get; set; }
		public int PlanId { get; set; }
		public string MemberName { get; set; } = null!;
		public string PlanName { get; set; } = null!;
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }

        public bool IsActive => StartDate <= GymManagementBLL.Services.Classes.BrazilClock.Now &&
                                EndDate >= GymManagementBLL.Services.Classes.BrazilClock.Now;

        public bool IsScheduled => StartDate > GymManagementBLL.Services.Classes.BrazilClock.Now;

        public string Status => IsActive ? "Ativa" : IsScheduled ? "Agendada" : "Vencida";
	}
}
