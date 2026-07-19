public interface IAdventurerFactory
{
    Adventurer Create(int id, AdventurerClassTemplate template, GuildRank rank);
    Adventurer CreatePrebuilt(int id, AdventurerClassTemplate template, string name);
}