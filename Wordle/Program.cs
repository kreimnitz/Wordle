using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Wordle
{
    class Program
    {
        private static string _wordleWordsPath = @"C:\Users\kevin\source\repos\Wordle\Wordle\bin\Debug\net5.0\WordleWords.txt";
        private static string _wordleGuessWordsPath = @"C:\Users\kevin\source\repos\Wordle\Wordle\bin\Debug\net5.0\WordleGuessList.txt";
        private static string _outputFileName = "WordleWordSequences(Entropy-FullGuess-Opt).csv";
        private static string _outputDecisionTreeName = "DesicionTree.md";

        static void Main(string[] args)
        {
            var validWords = ReadWordsFromSingleLineFile(_wordleWordsPath);
            var guessWords = ReadWordsFromSingleLineFile(_wordleGuessWordsPath).Union(validWords).ToList();

            var firstGuess = GetBestGuess(validWords, guessWords);
            ScanAllWords(firstGuess, validWords, guessWords);

            BuildDecisionTree(_outputFileName, _outputDecisionTreeName);
        }

        private static List<string> ReadCSW19()
        {
            var validWords = new List<string>();
            foreach (var word in File.ReadLines(@"C:\Users\kevin\OneDrive\Desktop\csw19.txt"))
            {
                if (word.Length == 5)
                {
                    validWords.Add(word);
                }
            }
            return validWords;
        }

        private static List<string> ReadWordsFromSingleLineFile(string path)
        {
            var line = File.ReadAllText(path);

            var words = line.Split("\",\"");
            words[0] = words[0].Substring(1);
            words[words.Length -1] = words[words.Length - 1].Substring(0, 5);

            return words.ToList();
        }

        private static void ScanAllWords(string firstGuess, List<string> validWords, List<string> guessWords)
        {
            int count = 0;
            var allGameResults = new List<(string ChosenWord, List<(string GuessWord, string Result, int Size)> AnswerSequence)>();
            foreach (var word in validWords)
            {
                var gameResults = PlayGame(firstGuess, word, validWords, guessWords);
                allGameResults.Add((word, gameResults));
                WriteToFile(word, gameResults);
                var percentage = (((double)count++) / validWords.Count).ToString("P", CultureInfo.CurrentCulture);
                Console.WriteLine(word + " " + percentage + " done");
            }
        }

        private static void WriteToFile(string chosenWord, List<(string GuessWord, string Result, int Size)> answerSequence)
        {
            if (!File.Exists(_outputFileName))
            {
                File.Create(_outputFileName).Dispose();
            }

            using (var file = File.AppendText(_outputFileName))
            {
                file.Write(chosenWord);
                file.Write("," + answerSequence.Count);
                foreach (var guess in answerSequence)
                {
                    file.Write("," + guess.GuessWord + " " + guess.Result);
                }
                file.WriteLine();
            }
        }

        private static List<(string GuessWord, string Result, int size)> PlayGame(string firstGuess, string chosenWord, List<string> validWordsIn, List<string> guessWordsIn)
        {
            if (chosenWord == firstGuess)
            {
                return new List<(string, string, int)> { (firstGuess, "22222", 1) };
            }

            var possibleWords = new List<string>(validWordsIn);
            var guessWords = new List<string>(guessWordsIn);
            
            var guesses = new List<(string, string, int)>();
            var result = EvaluateGuess(chosenWord, firstGuess);
            possibleWords = GetNewValidWords(possibleWords, firstGuess, result);
            guesses.Add((firstGuess, result, possibleWords.Count));

            var nextGuess = CheckCacheForNextGuess(firstGuess, result, possibleWords, guessWords);
            result = EvaluateGuess(chosenWord, nextGuess);
            while (result != "22222")
            {
                possibleWords = GetNewValidWords(possibleWords, nextGuess, result);
                guesses.Add((nextGuess, result, possibleWords.Count));

                nextGuess = GetBestGuess(possibleWords, guessWords);
                result = EvaluateGuess(chosenWord, nextGuess);
            }
            guesses.Add((nextGuess, result, 1));

            return guesses;
        }

        private static Dictionary<string, string> _cache = new Dictionary<string, string>();

        private static string CheckCacheForNextGuess(string lastGuess, string lastResult, List<string> possibleWords, List<string> guessWords)
        {
            if (!_cache.ContainsKey(lastGuess + lastResult))
            {
                var nextGuess = GetBestGuess(possibleWords, guessWords);
                _cache.Add(lastGuess + lastResult, nextGuess);
            }

            return _cache[lastGuess + lastResult];
        }

        private static string GetBestGuess(List<string> validWords, List<string> guessWords)
        {
            if (validWords.Count == 1 || validWords.Count == 2)
            {
                return validWords[0];
            }

            var maxWord = "";
            double maxScore = double.NegativeInfinity;
            var allScores = new Dictionary<string, double>();
            foreach (var guessWord in guessWords)
            {
                var score = ScoreGuess(validWords, guessWord);
                allScores.Add(guessWord, score);
                if (score > maxScore || score == maxScore && validWords.Contains(guessWord))
                {
                    maxScore = score;
                    maxWord = guessWord;
                }
            }

            return maxWord;
        }

        private static List<string> GetNewValidWords(List<string> validWords, string guessWord, string guessResult)
        {
            var newList = new List<string>();
            foreach (var word in validWords)
            {
                var tempResult = EvaluateGuess(word, guessWord);
                if (tempResult == guessResult)
                {
                    newList.Add(word);
                }
            }
            return newList;
        }

        private static Dictionary<string, int> GetResultBuckets(List<string> validWords, string guessWord)
        {
            var dictionary = new Dictionary<string, int>();
            foreach (var answerWord in validWords)
            {
                var results = EvaluateGuess(answerWord, guessWord);
                if (!dictionary.ContainsKey(results))
                {
                    dictionary.Add(results, 0);
                }
                dictionary[results]++;
            }
            return dictionary;
        }

        private static double ScoreGuess(List<string> validWords, string guessWord)
        {
            var dictionary = GetResultBuckets(validWords, guessWord);
            return ScoreWithLowestEntropy(dictionary, validWords.Count);
        }

        private static double ScoreWithDoubleGuess(List<string> validWords, string guessWord)
        {
            double expectedRemainingWords = 0;
            var resultCounts = GetResultBuckets(validWords, guessWord);
            foreach (var result in resultCounts.Keys)
            {
                var probability = ((double)resultCounts[result]) / validWords.Count;
                var validWordsAfterFirstGuess = GetNewValidWords(validWords, guessWord, result);
                var secondGuess = GetBestGuess(validWordsAfterFirstGuess, validWordsAfterFirstGuess);
                var buckets = GetResultBuckets(validWordsAfterFirstGuess, secondGuess);
                var expectedRemainingAfterSecondGuess = ScoreWithExpectedRemainingWords(buckets, validWordsAfterFirstGuess.Count);
                expectedRemainingWords += expectedRemainingAfterSecondGuess * probability;
            }
            return expectedRemainingWords;
        }

        private static double ScoreWithExpectedRemainingWords(Dictionary<string, int> resultBuckets, int possibleWordCount)
        {
            double total = 0;
            foreach (var entry in resultBuckets)
            {
                var probability = ((double)entry.Value) / possibleWordCount;
                total += probability * entry.Value;
            }
            return -total;
        }

        private static double ScoreWithStandardDev(Dictionary<string, int> resultBuckets)
        {
            var average = resultBuckets.Values.Sum() / 243;
            var sum = resultBuckets.Values.Sum(d => Math.Pow(d - average, 2));
            var standardDev = Math.Sqrt(sum / 242);
            return -standardDev;
        }

        private static double ScoreWithMeanSquares(Dictionary<string, int> resultBuckets)
        {
            double total = 0;
            foreach (var entry in resultBuckets)
            {
                total += Math.Pow(entry.Value, 2);
            }

            return -(total / resultBuckets.Count);
        }

        private static double ScoreWithLowestEntropy(Dictionary<string, int> resultBuckets, int possibleWordCount)
        {
            double total = 0;
            foreach (var entry in resultBuckets)
            {
                var probability = ((double)entry.Value) / possibleWordCount;
                var score = (-1) * probability * Math.Log2(probability);
                total += score;
            }
            return total;
        }


        private static Dictionary<string, string> _evaluateCache = new Dictionary<string, string>();
        private static string EvaluateGuess(string chosenWord, string guess)
        {
            if (_evaluateCache.ContainsKey(chosenWord + guess))
            {
                return _evaluateCache[chosenWord + guess];
            }

            var usedResultLetterCounts = new Dictionary<char, int>();
            var chosenWordLetterCounts = new Dictionary<char, int>();
            for (int i = 0; i < 5; i++)
            {
                var letter = chosenWord[i];
                if (!chosenWordLetterCounts.ContainsKey(letter))
                {
                    chosenWordLetterCounts[letter] = 1;
                    usedResultLetterCounts[letter] = 0;
                }
                else
                {
                    chosenWordLetterCounts[letter]++;
                }
            }

            var results = new List<int>() { 0, 0, 0, 0, 0};

            for (int i = 0; i < 5; i++)
            {
                var guessLetter = guess[i];
                if (chosenWord[i] == guessLetter)
                {
                    results[i] = 2;
                    usedResultLetterCounts[guessLetter]++;
                }
            }

            for (int i = 0; i < 5; i++)
            {
                var guessLetter = guess[i];
                if (chosenWord.Contains(guessLetter) && usedResultLetterCounts[guessLetter] < chosenWordLetterCounts[guessLetter])
                {
                    results[i] = 1;
                    usedResultLetterCounts[guessLetter]++;
                }
            }

            var result = $"{results[0]}{results[1]}{results[2]}{results[3]}{results[4]}";
            _evaluateCache[chosenWord + guess] = result;
            return result;
        }

        private static void BuildDecisionTree(string fileIn, string fileOut)
        {
            var root = new WordleNode();
            foreach (var line in File.ReadLines(fileIn))
            {
                var splitLine = line.Split(",");
                root.AddSequence(splitLine.Skip(2));
            }

            if (!File.Exists(fileOut))
            {
                File.Create(fileOut).Dispose();
            }

            using (var file = File.AppendText(fileOut))
            {
                foreach (var line in root.ToMarkDownLines(0))
                {
                    file.WriteLine(line);
                }
            }
        }

        public class WordleNode
        {
            public string PreviousClue { get; set; }

            public string Word { get; set; }

            public Dictionary<string, WordleNode> Children { get; set; } = new Dictionary<string, WordleNode>();

            public void AddSequence(IEnumerable<string> sequence)
            {
                var currentNode = this;
                foreach (var step in sequence)
                {
                    var splitStep = step.Split(" ");
                    var clue = splitStep[1];
                    if (currentNode.Word is null)
                    {
                        currentNode.Word = splitStep[0];
                    }

                    if (splitStep[1].Equals("22222"))
                    {
                        break;
                    }

                    WordleNode nextNode;
                    if (!currentNode.Children.TryGetValue(clue, out nextNode))
                    {
                        nextNode = new WordleNode();
                        nextNode.PreviousClue = clue;
                        currentNode.Children.Add(clue, nextNode);
                    }
                    currentNode = nextNode;
                }
            }

            public List<string> ToMarkDownLines(int depth)
            {
                var lines = new List<string>();
                if (depth == 0)
                {
                    lines.Add(Word);
                    foreach (var child in Children.Values.OrderBy(n => n.PreviousClue))
                    {
                        foreach (var line in child.ToMarkDownLines(depth + 1))
                        {
                            lines.Add(line);
                        }
                    }
                    return lines;
                }

                var padding = new string(' ', (depth - 1) * 2);
                if (Children.Any())
                {
                    lines.Add($"{padding}<details>");
                    lines.Add($"{padding}  <summary>{PreviousClue} {Word}</summary>");
                    lines.Add($"{padding}  <blockquote>");
                    foreach (var child in Children.Values.OrderBy(n => n.PreviousClue))
                    {
                        foreach (var line in child.ToMarkDownLines(depth + 2))
                        {
                            lines.Add(line);
                        }
                    }
                    lines.Add($"{padding}  </blockquote>");
                    lines.Add($"{padding}</details>");
                }
                else
                {
                    lines.Add($"{padding}{PreviousClue} {Word}");
                    lines.Add($"<br/>");
                }

                return lines;
            }
        }
    }
}
