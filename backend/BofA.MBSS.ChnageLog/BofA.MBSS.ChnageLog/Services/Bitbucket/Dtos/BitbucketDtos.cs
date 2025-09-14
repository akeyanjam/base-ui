namespace BofA.MBSS.ChnageLog.Services.Bitbucket.Dtos;

// Bitbucket API Response Models
public record BitbucketPullRequest(
    int Id,
    string Title,
    string State,
    DateTimeOffset? UpdatedDate,
    BitbucketRef FromRef,
    BitbucketRef ToRef,
    BitbucketAuthor? Author,
    BitbucketLinks Links);

public record BitbucketRef(
    string Id,
    string DisplayId,
    BitbucketRepository Repository);

public record BitbucketRepository(
    string Name,
    string Slug,
    BitbucketProject Project);

public record BitbucketProject(
    string Key,
    string Name);

public record BitbucketAuthor(
    BitbucketUser User);

public record BitbucketUser(
    string Name,
    string EmailAddress,
    string DisplayName);

public record BitbucketLinks(
    List<BitbucketLink> Self);

public record BitbucketLink(
    string Href);

public record BitbucketPage<T>(
    List<T> Values,
    int Size,
    int Limit,
    bool IsLastPage,
    int Start);
