using Mediscribe_AI.Models.RequestsDTO;
using Mediscribe_AI.Serives.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Mediscribe_AI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SoapNoteController : ControllerBase
    {
        private readonly ISoapNotesService _soapnotesservice;

        public SoapNoteController(ISoapNotesService soapnotesservice)
        {
            _soapnotesservice = soapnotesservice;
        }

        [HttpPost("generateSOAPNote")]
        public async Task<IActionResult> Generate([FromBody] SymptomRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PatientNarrative))
                return BadRequest("Patient narrative cannot be empty.");

            var soapNotes = await _soapnotesservice.GenerateSoapNotesAsync(request.PatientNarrative);
            return Ok(soapNotes);
        }
    }
}
