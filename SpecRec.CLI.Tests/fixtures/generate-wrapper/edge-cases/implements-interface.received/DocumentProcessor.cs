using System;
using System.Collections.Generic;

namespace EdgeCases
{
    public class DocumentProcessor
    {
        private readonly List<ProcessingResult> _results = new();

        public void ProcessDocument(string content, ProcessingOptions options)
        {
            var result = new ProcessingResult
            {
                Content = content,
                Timestamp = DateTime.Now,
                Status = ProcessingStatus.Completed,
                Options = options
            };
            _results.Add(result);
        }

        public ProcessingResult[] GetResults()
        {
            return _results.ToArray();
        }

        public void ClearResults()
        {
            _results.Clear();
        }

        public ProcessingOptions CreateDefaultOptions()
        {
            return new ProcessingOptions
            {
                EnableValidation = true,
                MaxLength = 1000
            };
        }

        // Nested public class - should be available for wrapping
        public class ProcessingOptions
        {
            public bool EnableValidation { get; set; }
            public int MaxLength { get; set; }
            public string Format { get; set; } = "text";

            public bool IsValid()
            {
                return MaxLength > 0;
            }
        }

        // Nested public class - should be available for wrapping  
        public class ProcessingResult
        {
            public string Content { get; set; } = "";
            public DateTime Timestamp { get; set; }
            public ProcessingStatus Status { get; set; }
            public ProcessingOptions Options { get; set; } = new();
        }

        // Nested public enum
        public enum ProcessingStatus
        {
            Pending,
            InProgress,
            Completed,
            Failed
        }

        // Private nested class - should NOT be wrapped
        private class InternalState
        {
            public int ProcessedCount { get; set; }
        }
    }
}