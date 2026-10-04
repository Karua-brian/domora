using Domora.Domain.Common.Exceptions;
using Domora.Domain.Users.ValueObjects;

namespace Domora.Domain.Users;

public sealed class User
{
    public Guid Id { get; }

    public UserEmail Email { get; private set; }

    public UserPassword PasswordHash { get; private set; }

    private User(
        Guid id, 
        UserEmail email, 
        UserPassword passwordHash
    )
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
    }

    public static User Register(
        UserEmail email, 
        UserPassword passwordHash
    )
    {
        return new User(
            Guid.NewGuid(), 
            email, 
            passwordHash
        );
    }

    // public void UpdateEmail(UserEmail email)
    // {
    //     Email = email;
    // }

    // public void UpdatePassword(UserPassword password)
    // {
    //     Password = password;
    // }
}