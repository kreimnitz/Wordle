using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Wordle
{
    class Program
    {
        private static string _wordleWordsPath = @"C:\Users\kevin\source\repos\Wordle\Wordle\bin\Debug\net5.0\WordleWords.txt";
        private static string _wordleGuessWordsPath = @"C:\Users\kevin\source\repos\Wordle\Wordle\bin\Debug\net5.0\WordleGuessList.txt";
        private static string _firstGuess = "soare";

        static void Main(string[] args)
        {
            var validWords = ReadWordsFromSingleLineFile(_wordleWordsPath);
            var guessWords = ReadWordsFromSingleLineFile(_wordleGuessWordsPath);

            ScanAllWords(validWords, guessWords);

            Console.ReadLine();
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

        private static void ScanAllWords(List<string> validWords, List<string> guessWords)
        {
            var allGameResults = new List<(string ChosenWord, List<(string GuessWord, string Result, int Size)> AnswerSequence)>();
            foreach (var word in validWords)
            {
                var gameResults = PlayGame(word, validWords, guessWords);
                allGameResults.Add((word, gameResults));
                WriteToFile(word, gameResults);
                Console.WriteLine(word + " done");
            }
        }

        private static void WriteToFile(string chosenWord, List<(string GuessWord, string Result, int Size)> answerSequence)
        {
            using (var file = File.AppendText("WordleWordSequences(FullGuess).csv"))
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

        private static List<(string GuessWord, string Result, int size)> PlayGame(string chosenWord, List<string> validWordsIn, List<string> guessWordsIn)
        {
            var firstGuess = _firstGuess;
            if (chosenWord == firstGuess)
            {
                return new List<(string, string, int)> { (firstGuess, "22222", 1) };
            }

            var possibleWords = new List<string>(validWordsIn);
            var guessWords = new List<string>(guessWordsIn);
            
            var guesses = new List<(string, string, int)>();
            var result = EvaluateGuess(chosenWord, firstGuess);
            possibleWords = TrimValidWords(possibleWords, firstGuess, result);
            guesses.Add((firstGuess, result, possibleWords.Count));

            var nextGuess = CheckCacheForNextGuess(firstGuess, result, possibleWords, guessWords);
            result = EvaluateGuess(chosenWord, nextGuess);
            while (result != "22222")
            {
                possibleWords = TrimValidWords(possibleWords, nextGuess, result);
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
            if (validWords.Count == 1)
            {
                return validWords[0];
            }

            var maxWord = "";
            double maxScore = 0;
            foreach (var guessWord in guessWords)
            {
                var score = ScoreGuess(validWords, guessWord);
                if (score > maxScore)
                {
                    maxScore = score;
                    maxWord = guessWord;
                }
            }

            return maxWord;
        }

        private static List<string> TrimValidWords(List<string> validWords, string guessWord, string guessResult)
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

        private static double ScoreGuess(List<string> validWords, string guessWord)
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

            double total = 0;
            foreach (var entry in dictionary)
            {
                var probability = ((double) entry.Value) / validWords.Count;
                var score = (-1) * probability * Math.Log2(probability);
                total += score;
            }
            return total;
        }

        private static string EvaluateGuess(string chosenWord, string guess)
        {
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

            return $"{results[0]}{results[1]}{results[2]}{results[3]}{results[4]}";
        }
    }
}
