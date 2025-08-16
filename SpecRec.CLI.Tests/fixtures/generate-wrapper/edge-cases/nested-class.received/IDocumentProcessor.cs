using System;
using System.Collections.Generic;

namespace EdgeCases
{
    public interface IDocumentProcessorWrapper
    {
        void ProcessDocument(string content, DocumentProcessor.ProcessingOptions options);
        DocumentProcessor.ProcessingResult[] GetResults();
        void ClearResults();
        DocumentProcessor.ProcessingOptions CreateDefaultOptions();
    }
}