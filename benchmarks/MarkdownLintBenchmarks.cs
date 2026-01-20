using System.Linq;
using BenchmarkDotNet.Attributes;
using MarkdownLintVS.Linting;
using Microsoft.VSDiagnostics;

namespace MarkdownLintVS.Benchmarks
{
    [CPUUsageDiagnoser]
    public class MarkdownLintBenchmarks
    {
        private string _smallDocument;
        private string _mediumDocument;
        private string _largeDocument;
        private MarkdownLintAnalyzer _analyzer;
        [GlobalSetup]
        public void Setup()
        {
            _analyzer = new MarkdownLintAnalyzer();
            // Small document - typical README
            _smallDocument = @"# My Project

This is a simple project description.

## Features

- Feature 1
- Feature 2
- Feature 3

## Installation

```bash
npm install my-project
```

## Usage

```csharp
var client = new MyClient();
client.DoSomething();
```

## License

MIT License
";
            // Medium document - more complex with various elements
            _mediumDocument = GenerateMediumDocument();
            // Large document - stress test
            _largeDocument = GenerateLargeDocument();
        }

        private string GenerateMediumDocument()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# Project Documentation");
            sb.AppendLine();
            for (var i = 1; i <= 10; i++)
            {
                sb.AppendLine($"## Section {i}");
                sb.AppendLine();
                sb.AppendLine($"This is the content of section {i}. It contains multiple sentences. Here is some more text to make it realistic.");
                sb.AppendLine();
                sb.AppendLine("### Subsection");
                sb.AppendLine();
                sb.AppendLine("- Item 1");
                sb.AppendLine("- Item 2");
                sb.AppendLine("- Item 3");
                sb.AppendLine();
                sb.AppendLine("```csharp");
                sb.AppendLine("public void Method()");
                sb.AppendLine("{");
                sb.AppendLine("    Console.WriteLine(\"Hello\");");
                sb.AppendLine("}");
                sb.AppendLine("```");
                sb.AppendLine();
                sb.AppendLine("> This is a blockquote with some important information.");
                sb.AppendLine();
                sb.AppendLine("Here is a [link](https://example.com) and some **bold** and *italic* text.");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string GenerateLargeDocument()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# Large Documentation");
            sb.AppendLine();
            for (var i = 1; i <= 50; i++)
            {
                sb.AppendLine($"## Chapter {i}");
                sb.AppendLine();
                sb.AppendLine($"Content for chapter {i}. This section covers important topics related to the main subject matter.");
                sb.AppendLine();
                for (var j = 1; j <= 3; j++)
                {
                    sb.AppendLine($"### Section {i}.{j}");
                    sb.AppendLine();
                    sb.AppendLine("1. First ordered item");
                    sb.AppendLine("2. Second ordered item");
                    sb.AppendLine("3. Third ordered item");
                    sb.AppendLine();
                    sb.AppendLine("| Column A | Column B | Column C |");
                    sb.AppendLine("|----------|----------|----------|");
                    sb.AppendLine("| Data 1   | Data 2   | Data 3   |");
                    sb.AppendLine("| Data 4   | Data 5   | Data 6   |");
                    sb.AppendLine();
                    sb.AppendLine("```javascript");
                    sb.AppendLine("function example() {");
                    sb.AppendLine("    return 'hello';");
                    sb.AppendLine("}");
                    sb.AppendLine("```");
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        [Benchmark(Baseline = true)]
        public int AnalyzeSmallDocument()
        {
            return _analyzer.Analyze(_smallDocument, null).Count();
        }

        [Benchmark]
        public int AnalyzeMediumDocument()
        {
            return _analyzer.Analyze(_mediumDocument, null).Count();
        }

        [Benchmark]
        public int AnalyzeLargeDocument()
        {
            return _analyzer.Analyze(_largeDocument, null).Count();
        }

        [Benchmark]
        public MarkdownDocumentAnalysis ParseSmallDocument()
        {
            return new MarkdownDocumentAnalysis(_smallDocument);
        }

        [Benchmark]
        public MarkdownDocumentAnalysis ParseMediumDocument()
        {
            return new MarkdownDocumentAnalysis(_mediumDocument);
        }

        [Benchmark]
        public MarkdownDocumentAnalysis ParseLargeDocument()
        {
            return new MarkdownDocumentAnalysis(_largeDocument);
        }
    }
}