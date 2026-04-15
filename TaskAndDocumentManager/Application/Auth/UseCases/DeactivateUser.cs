using TaskAndDocumentManager.Application.Auth.Interfaces;

namespace TaskAndDocumentManager.Application.Auth.UseCases;

public class DeactivateUser
{
    private readonly IUserRepository _userRepository;

    public DeactivateUser(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public void Execute(Guid userId)
    {
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
