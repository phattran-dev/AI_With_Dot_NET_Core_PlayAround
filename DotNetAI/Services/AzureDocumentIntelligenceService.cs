using Azure;
using Azure.AI.DocumentIntelligence;
using DotNetAI.Model;
using DotnetGeminiSDK.Client.Interfaces;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DotNetAI.Services
{
    public interface IAzureDocumentIntelligenceService
    {
        Task<CV> AnalyzeCVAsync(IFormFile file);
    }
    public class AzureDocumentIntelligenceService : IAzureDocumentIntelligenceService
    {
        private readonly DocumentIntelligenceClient _documentIntelligenceClient;
        private readonly string modelId;
        private readonly IGeminiClient _geminiClient;
        public AzureDocumentIntelligenceService(IConfiguration configuration, IGeminiClient geminiClient)
        {
            var endpoint = configuration.GetValue<string>("AzureDocumentIntelligence:endpoint");
            var apiKey = configuration.GetValue<string>("AzureDocumentIntelligence:apiKey");
            this.modelId = configuration.GetValue<string>("AzureDocumentIntelligence:modelId");

            var credential = new AzureKeyCredential(apiKey);
            this._documentIntelligenceClient = new DocumentIntelligenceClient(new Uri(endpoint), credential);

            _geminiClient = geminiClient;
        }

        public async Task<CV> AnalyzeCVAsync(IFormFile file)
        {
            var memoryStream = await ToMemoryStreamAsync(file.OpenReadStream());
            var binaryData = await BinaryData.FromStreamAsync(memoryStream);
            var operation = await this._documentIntelligenceClient.AnalyzeDocumentAsync(WaitUntil.Completed, this.modelId, binaryData);

            var result = operation.Value;
            var document = result.Documents.FirstOrDefault();

            if (document == null)
            {
                throw new Exception("Document not found");
            }

            var cv = await ExtractData(document);

            return cv;
        }

        private async Task<MemoryStream> ToMemoryStreamAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            return memoryStream;
        }

        private async Task<CV> ExtractData(AnalyzedDocument document)
        {
            var cv = new CV();

            // Process Single Fields
            foreach (KeyValuePair<string, DocumentField> field in document.Fields)
            {
                var fieldValue = field.Value?.ValueString ?? string.Empty;
                switch (field.Key.ToLower())
                {
                    case "name":
                        cv.Name = fieldValue;
                        break;

                    case "title":
                        cv.Role = fieldValue;
                        break;

                    case "overview":
                        cv.Overview = fieldValue;
                        break;
                }
            }

            // Process TechnicalSkills
            if (document.Fields.TryGetValue("TechnicalSkills", out var skillFields) && skillFields.FieldType == DocumentFieldType.List)
            {
                var skills = new List<string>();
                foreach (DocumentField item in skillFields.ValueList)
                {
                    if (item.FieldType == DocumentFieldType.Dictionary)
                    {
                        foreach (KeyValuePair<string, DocumentField> subItem in item.ValueDictionary)
                        {
                            var fieldValue = subItem.Value?.ValueString ?? string.Empty;
                            switch (subItem.Key.ToLower())
                            {
                                case "name":
                                    //var skills = 
                                    skills.Add(fieldValue);
                                    break;
                            }
                        }
                    }
                }
                //cv.TechnicalSkills = await CorrectDataSkills(skills
                cv.TechnicalSkills = skills;
            }

            // Process Certifications
            if (document.Fields.TryGetValue("Certifications", out var ceriticationFields) && ceriticationFields.FieldType == DocumentFieldType.List)
            {
                var ceritications = new List<string>();
                foreach (DocumentField item in ceriticationFields.ValueList)
                {
                    if (item.FieldType == DocumentFieldType.Dictionary)
                    {
                        foreach (KeyValuePair<string, DocumentField> subItem in item.ValueDictionary)
                        {
                            var fieldValue = subItem.Value?.ValueString ?? string.Empty;
                            switch (subItem.Key.ToLower())
                            {
                                case "name":
                                    //var skills = 
                                    ceritications.Add(fieldValue);
                                    break;
                            }
                        }
                    }
                }
                cv.Certifications = ceritications;
            }

            // Process Awards
            if (document.Fields.TryGetValue("Awards", out var awardFields) && awardFields.FieldType == DocumentFieldType.List)
            {
                var awards = new List<string>();
                foreach (DocumentField item in awardFields.ValueList)
                {
                    if (item.FieldType == DocumentFieldType.Dictionary)
                    {
                        foreach (KeyValuePair<string, DocumentField> subItem in item.ValueDictionary)
                        {
                            var fieldValue = subItem.Value?.ValueString ?? string.Empty;
                            switch (subItem.Key.ToLower())
                            {
                                case "name":
                                    //var skills = 
                                    awards.Add(fieldValue);
                                    break;
                            }
                        }
                    }
                }
                cv.Awards = awards;
            }

            // Process WorkExperiences 
            if (document.Fields.TryGetValue("WorkExperiences", out var workExperienceFields) && workExperienceFields.FieldType == DocumentFieldType.List)
            {

                var workExperiences = new List<WorkExperience>();
                foreach (DocumentField item in workExperienceFields.ValueList)
                {
                    if (item.FieldType == DocumentFieldType.Dictionary)
                    {
                        var workExperience = new WorkExperience();
                        foreach (KeyValuePair<string, DocumentField> subItem in item.ValueDictionary)
                        {
                            var fieldValue = subItem.Value?.ValueString ?? string.Empty;
                            switch (subItem.Key.ToLower())
                            {
                                case "project":
                                    workExperience.Project = fieldValue;
                                    break;

                                case "position":
                                    workExperience.Position = fieldValue;
                                    break;

                                case "starttime":
                                    workExperience.StartTime = fieldValue;
                                    break;

                                case "endtime":
                                    workExperience.EndTime = fieldValue;
                                    break;

                                case "description":
                                    workExperience.Description = fieldValue;
                                    break;

                                case "additionaldetail":
                                    workExperience.AdditionalDetail = fieldValue;
                                    break;
                            }
                        }
                        workExperiences.Add(workExperience);
                    }
                }
                cv.WorkExperiences = workExperiences;
            }
            return cv;
        }

        private string CorrectPromptResponse(string rawText)
        {
            string pattern = @"\[""(.*?)"",\s*""(.*?)""\]";
            Match match = Regex.Match(rawText, pattern);
            var correctionResponse = match.Success ? match.Value : "";

            return correctionResponse;
        }

        private async Task<List<string>> CorrectDataSkills(List<string> skills)
        {
            if (!skills.Any())
                return skills;

            var rootPrompt = "You will receive a JSON array containing strings, where each string represents one or more technical skills. These strings may contain multiple concatenated skills (e.g., 'C#Java', 'PythonJavaScript'), variations in capitalization (e.g., 'java', 'Java'), and punctuation (e.g., 'Java.', 'C++'). Your task is: Separate combined skills within each string into individual skills. Ensure the final list contains only unique skills And Return JSON Array: Provide the output as a JSON array containing the cleaned and standardized skill names. \r\n\r Here is the input: ";
            var processPrompt = rootPrompt + JsonSerializer.Serialize(skills);

            var response = await _geminiClient.TextPrompt(processPrompt);

            if (response.Candidates.Any())
            {
                var rawTextSkillsCorrection = response.Candidates[0].Content.Parts[0].Text;
                var rawSkillsCorrection = JsonSerializer.Deserialize<List<string>>(CorrectPromptResponse(rawTextSkillsCorrection));
                var skillsCorrection = rawSkillsCorrection.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                return skillsCorrection;
            }

            return skills;
        }

    }
}
