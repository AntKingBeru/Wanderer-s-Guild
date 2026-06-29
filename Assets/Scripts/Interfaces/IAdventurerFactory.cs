public interface IAdventurerFactory
{
    Adventurer Create(int id, AdventurerClassTemplate template, GuildRank rank);
}