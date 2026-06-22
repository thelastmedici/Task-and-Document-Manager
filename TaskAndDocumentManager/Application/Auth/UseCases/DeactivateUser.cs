using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Application.Workspaces.Interfaces;

namespace TaskAndDocumentManager.Application.Auth.UseCases;

public class DeactivateUser
{
    private readonly IUserRepository _userRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;

    public DeactivateUser(
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

        var user = _userRepository.GetById(userId);
        if (user is null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        if (!user.IsActive)
        {
            return;
        }

        user.IsActive = false;
        _userRepository.Save(user);
    }
}
