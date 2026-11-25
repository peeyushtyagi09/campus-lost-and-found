namespace LostAndFound.Api.Privacy;

public interface IEmailHasher
{
    string Normalize(string email);

    string Hash(string email);
}
