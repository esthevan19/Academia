using AutoMapper;
using GymManagementDAL.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Extensions.Logging;
using GymManagementBLL.Services.AttachmentService;
using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.MemberViewModels;
using GymManagementDAL.Entities;
using GymManagementDAL.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagementBLL.Services.Classes
{
    public class MemberService : IMemberService
    {

        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IAttachmentService _attachmentService;
        private readonly ILogger<MemberService> _logger;
        private readonly GymDbContext _db;

        #endregion

        #region Constructor

        public MemberService(IUnitOfWork unitOfWork, IMapper mapper, IAttachmentService attachmentService, ILogger<MemberService> logger, GymDbContext db)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _attachmentService = attachmentService;
            _logger = logger;
            _db = db;
        }

        #endregion

        #region GetAllMembers
        public IEnumerable<MemberViewModel> GetAllMembers()
        {

            var members = _unitOfWork.GetRepository<Member>().GetAll();

            if (members == null || !members.Any()) return [];

            return _mapper.Map<IEnumerable<MemberViewModel>>(members);

        }

        #endregion

        #region GetMemberHealthRecord

        public HealthRecordViewModel? GetMemberHealthRecord(int id)
        {

            var memberHealthRecord = _unitOfWork.GetRepository<HealthRecord>().GetById(id);

            if (memberHealthRecord is null) return null;

            return _mapper.Map<HealthRecordViewModel>(memberHealthRecord);

        }


        #endregion

        #region GetMemberDetails

        public MemberViewModel? GetMemberDetails(int id)
        {
            var repo = _unitOfWork.GetRepository<Member>();
            var member = repo.GetById(id);

            if (member is null) return null;

            var mappedMember = _mapper.Map<MemberViewModel>(member);

            var ActiveMembership = _unitOfWork.GetRepository<Membership>()
                .GetAll(m => m.MemberId == id && m.EndDate >= BrazilClock.Now).FirstOrDefault();

            if (ActiveMembership is not null)
            {
                var activePlan = _unitOfWork.GetRepository<Plan>().GetById(ActiveMembership.PlanId);
                mappedMember.PlanName = activePlan?.Name;
                mappedMember.MembershipEndDate = ActiveMembership.EndDate.ToShortDateString();
                mappedMember.MembershipStartDate = ActiveMembership.CreatedAt.ToShortDateString();
            }

            return mappedMember;

        }

        #endregion

        #region CreateMember

        public bool CreateMember(CreateMemberViewModel createdMember)
        {

            try
            {

                if (IsMailExist(createdMember.Email) || IsPhoneExist(createdMember.Phone))
                    return false;

                string photoName = "default-avatar.png";
                if (createdMember.PhotoFile is not null && createdMember.PhotoFile.Length > 0)
                {
                    var uploadedPhoto = _attachmentService.Upload("members", createdMember.PhotoFile);
                    if (!string.IsNullOrEmpty(uploadedPhoto))
                    {
                        photoName = uploadedPhoto;
                    }
                }

                var member = _mapper.Map<Member>(createdMember);
                member.Photo = photoName;

                _unitOfWork.GetRepository<Member>().Add(member);

                bool isCreated = _unitOfWork.SaveChanges() > 0;

                if (!isCreated && photoName != "default-avatar.png")
                    _attachmentService.Delete(photoName, "members");

                return isCreated;

            }
            catch (Exception ex)
            {
                // Registrar detalhes apenas no log do servidor, sem expor dados ao navegador.
                _logger.LogError(ex, "Falha ao cadastrar aluno no banco de dados.");
                return false;
            }

        }

        #endregion

        #region UpdateMember

        public MemberToUpdateViewModel? GetMemberToUpdate(int id)
        {

            var member = _unitOfWork.GetRepository<Member>().GetById(id);

            if (member is null) return null;

            return _mapper.Map<MemberToUpdateViewModel>(member);

        }

        public bool UpdateMember(int id, MemberToUpdateViewModel updatedMember)
        {
            if (id <= 0 || updatedMember is null) return false;

            try
            {
                var emailExist = _unitOfWork.GetRepository<Member>()
                    .GetAll(x => x.Email == updatedMember.Email && x.Id != id);
                var phoneExist = _unitOfWork.GetRepository<Member>()
                    .GetAll(x => x.Phone == updatedMember.Phone && x.Id != id);
                if (emailExist.Any() || phoneExist.Any()) return false;

                var repo = _unitOfWork.GetRepository<Member>();
                var existingMember = repo.GetById(id);
                if (existingMember is null) return false;

                _mapper.Map(updatedMember, existingMember);
                repo.Update(existingMember);
                return _unitOfWork.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao atualizar aluno {MemberId}.", id);
                return false;
            }
        }

        #endregion

        #region RemoveMember

        public bool RemoveMember(int id)
        {
            if (id <= 0) return false;

            try
            {
                using var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable);
                var member = _db.Members.Find(id);
                if (member is null) return false;

                // Nunca apagar dados históricos ou uma conta que ainda pertença a este aluno.
                // A exclusão é permitida somente para um cadastro sem movimentações/vínculos.
                var hasRelatedData =
                    _db.Memberships.Any(x => x.MemberId == id) ||
                    _db.Bookings.Any(x => x.MemberId == id) ||
                    _db.Workouts.Any(x => x.MemberId == id) ||
                    _db.UserClaims.Any(x => x.ClaimType == "gymflow:member_id" &&
                                            x.ClaimValue == id.ToString());
                if (hasRelatedData) return false;

                _db.Members.Remove(member);
                if (_db.SaveChanges() <= 0) return false;
                transaction.Commit();

                // Arquivos são apagados somente após a confirmação do banco.
                try
                {
                    if (!string.IsNullOrWhiteSpace(member.Photo) &&
                        !string.Equals(member.Photo, "default-avatar.png", StringComparison.OrdinalIgnoreCase))
                        _attachmentService.Delete(member.Photo, "members");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Aluno {MemberId} removido do banco, mas foto não pôde ser apagada.", id);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao excluir aluno {MemberId}.", id);
                return false;
            }
        }

        #endregion

        #region HelperMethod

        private bool IsMailExist(string email)
        {
            return _unitOfWork.GetRepository<Member>().GetAll(m => m.Email == email).Any();
        }
        private bool IsPhoneExist(string phone)
        {
            return _unitOfWork.GetRepository<Member>().GetAll(m => m.Phone == phone).Any();
        }

        #endregion
    }
}
