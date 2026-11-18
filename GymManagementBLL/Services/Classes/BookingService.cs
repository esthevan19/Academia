using AutoMapper;
using GymManagementBLL.Services.Interfaces;
using GymManagementDAL.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Data;
using GymManagementBLL.ViewModels.BookingViewModels;
using GymManagementBLL.ViewModels.MembershipViewModels;
using GymManagementBLL.ViewModels.SessionViewModels;
using GymManagementDAL.Entities;
using GymManagementDAL.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagementBLL.Services.Classes
{
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly GymDbContext _db;

        public BookingService(IUnitOfWork unitOfWork, IMapper mapper, GymDbContext db)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _db = db;
        }

        public IEnumerable<MemberBookingViewModel> GetMemberBookings(int memberId)
        {
            if (memberId <= 0) return [];

            var bookings = _unitOfWork.BookingRepository.GetMemberBookings(memberId);
            return bookings.Select(booking => new MemberBookingViewModel
            {
                SessionId = booking.SessionId,
                Description = booking.Session.Description,
                StartDate = booking.Session.StartDate,
                EndDate = booking.Session.EndDate,
                TrainerName = booking.Session.SessionTrainer != null ? booking.Session.SessionTrainer.Name : "",
                IsAttended = booking.IsAttended
            }).ToList();
        }

        #region GetAllSessions
        public IEnumerable<SessionViewModel> GetAllSessions()
        {
            var sessionsToBook = _unitOfWork.SessionRepository
                                      .GetAllSessionsWithTrainerAndCategory(x => x.EndDate >= BrazilClock.Now)
                                      .OrderByDescending(x => x.StartDate);

            if (!sessionsToBook.Any()) return [];

            var mappedSessionsToBook = _mapper.Map<IEnumerable<SessionViewModel>>(sessionsToBook);

            foreach (var session in mappedSessionsToBook)
            {
                session.AvailableSlots = session.Capacity - _unitOfWork.SessionRepository.GetCountOfBooksSlots(session.Id);
            }

            return mappedSessionsToBook;
        }
        #endregion

        #region GetMembersForDropDown
        public IEnumerable<MemberSelectListViewModel> GetMembersForDropDown(int sessionId)
        {
            var membersIdHasBooking = _unitOfWork.BookingRepository
                                                 .GetAll(x => x.SessionId == sessionId)
                                                 .Select(x => x.MemberId)
                                                 .ToList();

            var activeMemberIds = _unitOfWork.MembershipRepository
                .GetAll(x => x.CreatedAt <= BrazilClock.Now && x.EndDate >= BrazilClock.Now)
                .Select(x => x.MemberId).ToHashSet();

            var members = _unitOfWork.GetRepository<Member>()
                .GetAll(x => !membersIdHasBooking.Contains(x.Id) && activeMemberIds.Contains(x.Id));
            // Get All Members That Not Have Booking Untill => !List.Contains(x.Id)

            return _mapper.Map<IEnumerable<MemberSelectListViewModel>>(members);
        }
        #endregion

        #region GetMembersSession
        public IEnumerable<MemberForSessionViewModel> GetMembersSession(int sessionId)
        {
            var membersForSession = _unitOfWork.BookingRepository.GetSessionById(sessionId);

            return _mapper.Map<IEnumerable<MemberForSessionViewModel>>(membersForSession);
        }
        #endregion

        #region CreateNewBooking
        public bool CreateNewBooking(CreateBookingViewModel createdBooking)
        {
            if (createdBooking is null || createdBooking.MemberId <= 0 || createdBooking.SessionId <= 0)
                return false;

            try
            {
                // Todas as verificações e a gravação acontecem na mesma transação.
                // Serializable evita que duas reservas simultâneas excedam a última vaga.
                using var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable);
                var now = BrazilClock.Now;
                var session = _db.Sessions.AsNoTracking()
                    .SingleOrDefault(x => x.Id == createdBooking.SessionId);
                if (session is null || session.StartDate <= now ||
                    session.EndDate <= now || session.Capacity <= 0)
                    return false;

                var hasActiveMembership = _db.Memberships.AsNoTracking().Any(x =>
                    x.MemberId == createdBooking.MemberId &&
                    x.CreatedAt <= now && x.EndDate >= now);
                if (!hasActiveMembership) return false;

                if (_db.Bookings.Any(x => x.MemberId == createdBooking.MemberId &&
                                          x.SessionId == createdBooking.SessionId))
                    return false;

                if (_db.Bookings.Count(x => x.SessionId == createdBooking.SessionId) >= session.Capacity)
                    return false;

                _db.Bookings.Add(_mapper.Map<Booking>(createdBooking));
                if (_db.SaveChanges() <= 0) return false;
                transaction.Commit();
                return true;
            }
            catch
            {
                // Falha de concorrência/duplicidade: transação descartada sem exceder vagas.
                return false;
            }
        }

        #endregion

        #region IsMemberAttended
        public bool IsMemberAttended(int memberId, int sessionId)
        {
            try
            {
                var session = _unitOfWork.SessionRepository.GetById(sessionId);
                var now = BrazilClock.Now;
                if (session is null || session.StartDate > now || session.EndDate < now) return false;

                var memberSession = _unitOfWork.GetRepository<Booking>()
                                           .GetAll(X => X.MemberId == memberId && X.SessionId == sessionId)
                                           .FirstOrDefault();
                if (memberSession is null || memberSession.IsAttended) return false;

                memberSession.IsAttended = true;
                memberSession.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.GetRepository<Booking>().Update(memberSession);

                return _unitOfWork.SaveChanges() > 0;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region CancelBooking
        public bool CancelBooking(int memberId, int sessionId)
        {
            try
            {
                var session = _unitOfWork.SessionRepository.GetById(sessionId);
                if (session is null || session.StartDate <= BrazilClock.Now) return false;

                var Booking = _unitOfWork.BookingRepository.GetAll(X => X.SessionId == sessionId && X.MemberId == memberId)
                                                           .FirstOrDefault();
                if (Booking is null) return false;

                _unitOfWork.BookingRepository.Delete(Booking);

                return _unitOfWork.SaveChanges() > 0;
            }
            catch
            {
                return false;
            }
        }
        #endregion

    }
}
