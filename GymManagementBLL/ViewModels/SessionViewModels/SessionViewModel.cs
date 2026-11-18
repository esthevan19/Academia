using GymManagementBLL.Services.Classes;
﻿namespace GymManagementBLL.ViewModels.SessionViewModels
{
	public class SessionViewModel
	{
		public int Id { get; set; }
		public string Description { get; set; } = null!;
		public int Capacity { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public string TrainerName { get; set; } = null!;
		public string CategoryName { get; set; } = null!;
		public int AvailableSlots { get; set; }

		// Computed properties
		public string DateDisplay => StartDate.ToString("dd/MM/yyyy");
		public string TimeRangeDisplay => $"{StartDate:HH:mm} - {EndDate:HH:mm}";
		public TimeSpan Duration => EndDate - StartDate;
		public string Status
		{
			get
			{
				if (StartDate > BrazilClock.Now)
					return "Upcoming";
				else if (StartDate <= DateTime.UtcNow && EndDate >= DateTime.UtcNow)
					return "Ongoing";
				else
					return "Completed";
			}
		}
	}
}
