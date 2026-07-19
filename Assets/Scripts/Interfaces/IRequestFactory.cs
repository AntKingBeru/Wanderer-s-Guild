public interface IRequestFactory
{
    Request Create(int id, RequestTemplate template, GameDate now);
}