using BusinessObjects;

namespace Repositories.Interfaces
{
    public interface IHerbalInteractionRepository
    {
        Task<List<HerbalInteraction>> GetConflictsAmong(List<int> medicineIds);
    }
}
