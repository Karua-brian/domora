namespace Domora.Application.Common.Authentication;

public interface IAccessTokenGenerator
{
    string Generate(
        Guid userId
    );
}