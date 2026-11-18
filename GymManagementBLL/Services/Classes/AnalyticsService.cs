using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.AnalyticsViewModels;
using GymManagementDAL.Entities;
using GymManagementDAL.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagementBLL.Services.Classes
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AnalyticsService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public AnalyticsViewModel GetAnalyticsData()
        {

            var sessions = _unitOfWork.GetRepository<Session>().GetAll();
            var now = BrazilClock.Now;

            return new AnalyticsViewModel()
            {
                TotalMembers = _unitOfWork.GetRepository<Member>().GetAll().Count(),
                ActiveMembers = _unitOfWork.GetRepository<Membership>().GetAll(x => x.CreatedAt <= now && x.EndDate >= now).Select(x => x.MemberId).Distinct().Count(),
                TotalTrainers = _unitOfWork.GetRepository<Trainer>().GetAll().Count(),
                UpcomingSessions = sessions.Count(x => x.StartDate > now),
                OngoingSessions = sessions.Count(x => x.StartDate <= now && x.EndDate >= now),
                CompletedSessions = sessions.Count(x => x.EndDate < now)
            };
        }
    }
}
