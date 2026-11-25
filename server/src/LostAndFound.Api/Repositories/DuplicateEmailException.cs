namespace LostAndFound.Api.Repositories;

public sealed class DuplicateEmailException : Exception
{
    public DuplicateEmailException()
        : base("A user with the specified email hash already exists.")
    {
    }

    public DuplicateEmailException(Exception innerException)
        : base("A user with the specified email hash already exists.", innerException)
    {
    }
}
