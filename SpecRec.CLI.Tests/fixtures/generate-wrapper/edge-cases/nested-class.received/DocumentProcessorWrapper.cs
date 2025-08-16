using System;
using System.Collections.Generic;

namespace EdgeCases
{
    public class DocumentProcessorWrapper : IDocumentProcessorWrapper
    {
        private readonly DocumentProcessor _wrapped;

        public DocumentProcessorWrapper(DocumentProcessor wrapped)
        {
            _wrapped = wrapped;
        }

        public void ProcessDocument(string content, DocumentProcessor.ProcessingOptions options)
        {
            _wrapped.ProcessDocument(content, options);
        }

        public DocumentProcessor.ProcessingResult[] GetResults()
        {
            return _wrapped.GetResults();
        }

        public void ClearResults()
        {
            _wrapped.ClearResults();
        }

        public DocumentProcessor.ProcessingOptions CreateDefaultOptions()
        {
            return _wrapped.CreateDefaultOptions();
        }
    }
}