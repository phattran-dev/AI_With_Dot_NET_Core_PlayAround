using DotNetAI.Model;
using DotNetAI.Requests;
using DotNetAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotNetAI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AzureAIController(IAzureDocumentIntelligenceService _azureDocumentIntelligenceService) : ControllerBase
    {
        [HttpPost("cv-parser")]
        public async Task<ActionResult<CV>> CVParserAsync([FromForm] CVParserRequest request)
        {
            try
            {
                var result = await _azureDocumentIntelligenceService.AnalyzeCVAsync(request.File);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
