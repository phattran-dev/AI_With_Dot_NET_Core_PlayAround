using System.ComponentModel.DataAnnotations;

namespace DotNetAI.Requests
{
    public class CVParserRequest
    {
        [Required]
        public IFormFile File { get; set; }
    }
}
