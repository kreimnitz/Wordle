using System;
using System.Collections.Generic;
using System.IO;

namespace Wordle
{
    class Program
    {
        static void Main(string[] args)
        {
            var validWords = new List<string>();
            var guessWords = new List<string>();
            foreach (var word in File.ReadLines(@"C:\Users\kevin\OneDrive\Desktop\csw19.txt"))
            {
                if (word.Length == 5)
                {
                    validWords.Add(word);
                    guessWords.Add(word);
                }
            }

            Console.WriteLine("Valid Words: " + validWords.Count);

            var rand = new Random();
            var index = rand.Next(validWords.Count);
            var chosenWord = validWords[index];

            Console.WriteLine("Chosen Word: " + chosenWord);
            Console.WriteLine("Guess: TARES");
            var result = EvaluateGuess(chosenWord, "TARES");
            Console.WriteLine(result);
            Console.WriteLine();

            string nextGuess = "TARES";
            while (result != "22222")
            {
                validWords = TrimValidWords(validWords, nextGuess, result);

                nextGuess = GetBestGuess(validWords, guessWords);
                Console.WriteLine("Guess: " + nextGuess);
                result = EvaluateGuess(chosenWord, nextGuess);
                Console.WriteLine(result);
                Console.WriteLine();
            }

            Console.ReadLine();
        }

        private static void DoWordGame(string chosenWord, List<string> validWords, List<string> guessWords)
        {
            Console.WriteLine("Chosen Word: " + chosenWord);
            Console.WriteLine("Guess: TARES");
            var result = EvaluateGuess(chosenWord, "TARES");
            Console.WriteLine(result);
            Console.WriteLine();

            string nextGuess = "TARES";
            while (result != "22222")
            {
                validWords = TrimValidWords(validWords, nextGuess, result);

                nextGuess = GetBestGuess(validWords, guessWords);
                Console.WriteLine("Guess: " + nextGuess);
                result = EvaluateGuess(chosenWord, nextGuess);
                Console.WriteLine(result);
                Console.WriteLine();
            }
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
