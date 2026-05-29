using ChatApp.Api.Data;
using ChatApp.Api.Repositories;
using ChatApp.Api.Models;
using ChatApp.Api.Interfaces;

namespace ChatApp.Server.Repositories;

public class AttachmentRepository : GenericRepository<Attachment>, IAttachmentRepository
{
    public AttachmentRepository(AppDbContext context) : base(context) { }
}