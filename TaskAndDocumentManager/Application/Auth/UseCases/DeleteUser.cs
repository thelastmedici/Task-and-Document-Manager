using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Application.Workspaces.Interfaces;

namespace TaskAndDocumentManager.Application.Auth.UseCases;

public class DeleteUser
{
    private readonly IUserRepository _userRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;

    public DeleteUser(
        IUserRepository userRepository,
        IWorkspaceMemberRepository workspaceMemberRepository)
    {
        _userRepository = userRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
    }

    public void Execute(Guid userId, Guid workspaceId)
    {
        if (!_workspaceMemberRepository.IsMember(workspaceId, userId))
        {
            throw new KeyNotFoundException("User not found.");
        }

        _userRepository.Delete(userId);
    }
}
