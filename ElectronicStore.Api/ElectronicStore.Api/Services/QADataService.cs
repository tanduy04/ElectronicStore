using ElectronicStore.Api.Models;

namespace ElectronicStore.Api.Services
{
    public class QADataService
    {
        public List<QADocument> ParseQAFile(string filePath)
        {
            var documents = new List<QADocument>();

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var lines = File.ReadAllLines(filePath);
            string? currentQuestion = null;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    continue;
                }

                if (trimmedLine.StartsWith("Q:", StringComparison.OrdinalIgnoreCase))
                {
                    currentQuestion = trimmedLine.Substring(2).Trim();
                }
                else if (trimmedLine.StartsWith("A:", StringComparison.OrdinalIgnoreCase) && currentQuestion != null)
                {
                    var answer = trimmedLine.Substring(2).Trim();

                    documents.Add(new QADocument
                    {
                        Question = currentQuestion,
                        Answer = answer
                    });

                    currentQuestion = null;
                }
            }

            return documents;
        }
    }
}
