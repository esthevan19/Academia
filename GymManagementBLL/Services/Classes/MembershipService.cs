using AutoMapper;
using GymManagementDAL.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Data;
using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.MembershipViewModels;
using GymManagementDAL.Entities;
using GymManagementDAL.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagementBLL.Services.Classes
{
    public class MembershipService : IMembershipService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly GymDbContext _db;

        public MembershipService(IUnitOfWork unitOfWork, IMapper mapper, GymDbContext db)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _db = db;
        }

        public IEnumerable<MembershipViewModel> GetAllMemberships()
        {

            var memberships = _unitOfWork.MembershipRepository.GetAllMembershipsWithMemberAndPLan(x => true)
                .OrderByDescending(x => x.EndDate).ToList();
            if (!memberships.Any()) return [];
            return _mapper.Map<IEnumerable<MembershipViewModel>>(memberships);
        }

        public bool CreateMembership(CreateMembershipViewModel createdMembership)
        {
            if (createdMembership is null || createdMembership.MemberId <= 0 || createdMembership.PlanId <= 0)
                return false;

            try
            {
                // Impede duas requisições simultâneas de criarem matrículas conflitantes.
                using var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable);
                var now = BrazilClock.Now;
                var startDate = createdMembership.StartDate ?? now;
                var plan = _db.Plans.AsNoTracking()
                    .SingleOrDefault(x => x.Id == createdMembership.PlanId && x.IsActive);
                if (plan is null || plan.DurationDays <= 0 ||
                    !_db.Members.Any(x => x.Id == createdMembership.MemberId))
                    return false;

                if (_db.Memberships.Any(x => x.MemberId == createdMembership.MemberId &&
                                             x.EndDate >= now))
                    return false;

                // O esquema atual usa (MemberId, PlanId) como chave primária.
                // Não apagar registros antigos para permitir rematrícula: preservar histórico.
                if (_db.Memberships.Any(x => x.MemberId == createdMembership.MemberId &&
                                             x.PlanId == createdMembership.PlanId))
                    return false;

                var membership = _mapper.Map<Membership>(createdMembership);
                membership.CreatedAt = startDate;
                membership.EndDate = startDate.AddDays(plan.DurationDays);
                _db.Memberships.Add(membership);

                if (_db.SaveChanges() <= 0) return false;
                transaction.Commit();
                return true;
            }
            catch
            {
                // Em falha ou conflito concorrente, a transação é revertida.
                return false;
            }
        }

        public bool DeleteMembership(int id)
        {
            try
            {
                // Cancelar encerra a vigência sem apagar o histórico financeiro do aluno.
                var now = BrazilClock.Now;
                var repo = _unitOfWork.MembershipRepository;
                var membership = repo.GetAll(x => x.MemberId == id &&
                    x.EndDate >= now)
                    .OrderByDescending(x => x.EndDate).FirstOrDefault();
                if (membership is null) return false;

                membership.EndDate = now.AddTicks(-1);
                membership.UpdatedAt = DateTime.UtcNow;
                repo.Update(membership);
                return _unitOfWork.SaveChanges() > 0;
            }
            catch
            {
                return false;
            }
        }

        public IEnumerable<MemberSelectListViewModel> GetMembersForDropDown()
        {
            var members = _unitOfWork.GetRepository<Member>().GetAll();
            return _mapper.Map<IEnumerable<MemberSelectListViewModel>>(members);
        }

        public IEnumerable<PlanSelectListViewModel> GetPlansForDropDown()
        {
            var plans = _unitOfWork.GetRepository<Plan>().GetAll(x => x.IsActive == true);
            return _mapper.Map<IEnumerable<PlanSelectListViewModel>>(plans);
        }

    }
}
