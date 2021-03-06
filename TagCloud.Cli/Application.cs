﻿using System.Collections.Generic;
using System.Linq;
using TagCloud;
using TagCloud.Interfaces;
using TagCloud.IntermediateClasses;

namespace TagCloudCreator
{
    public class Application
    {
        private readonly IFileReader fileReader;
        private readonly IImageSaver imageSaver;
        private readonly ICloudLayouter layouter;
        private readonly ISizeScheme sizeScheme;
        private readonly IStatisticsCollector statisticsCollector;
        private readonly IVisualizer visualizer;
        private readonly IWordFilter wordFilter;
        private readonly IWordProcessor wordProcessor;

        public Application(
            ICloudLayouter layouter,
            IVisualizer visualizer,
            IFileReader fileReader,
            IImageSaver imageSaver,
            IStatisticsCollector statisticsCollector,
            IWordFilter wordFilter,
            ISizeScheme sizeScheme,
            IWordProcessor wordProcessor)
        {
            this.layouter = layouter;
            this.visualizer = visualizer;
            this.fileReader = fileReader;
            this.imageSaver = imageSaver;
            this.statisticsCollector = statisticsCollector;
            this.wordFilter = wordFilter;
            this.sizeScheme = sizeScheme;
            this.wordProcessor = wordProcessor;
        }

        public Result<None> Run(string inputFile, string outputFile)
        {
            return fileReader.Read(inputFile)
                .Then(inp => inp.Select(s => s.ToLower()))
                .Then(inp => ProcessWords(inp, wordProcessor))
                .Then(inp => ExcludeWords(inp, wordFilter))
                .Then(inp => statisticsCollector.GetStatistics(inp))
                .Then(FillCloud)
                .Then(visualizer.Visualize)
                .Then(img => imageSaver.Save(img, outputFile));
        }

        private Result<IEnumerable<string>> ProcessWords(IEnumerable<string> words, IWordProcessor processor)
        {
            return Result.Of(() => words.Select(word =>
            {
                var processed = processor.Process(word);
                return processed.IsSuccess ? processed.GetValueOrThrow() : word;
            }));
        }

        private Result<IEnumerable<string>> ExcludeWords(IEnumerable<string> words, IWordFilter filter)
        {
            return words.Where(w => !filter.ToExclude(w).GetValueOrThrow()).ToArray();
        }

        private Result<IEnumerable<PositionedElement>> FillCloud(
            IEnumerable<FrequentedWord> statistics)
        {
            return Result.Of(() => statistics.Select(word => sizeScheme.GetSize(word)
                .Then(size => layouter.PutNextRectangle(size))
                .Then(rect => new PositionedElement(word, rect))
                .GetValueOrThrow()));
        }
    }
}