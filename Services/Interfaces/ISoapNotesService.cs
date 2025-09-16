using Mediscribe_AI.Models.ResponseVM;

namespace Mediscribe_AI.Serives.Interfaces
{
    public interface ISoapNotesService
    {
        Task<SoapNotesResponse> GenerateSoapNotesAsync(string patientNarrative);
    }
}
