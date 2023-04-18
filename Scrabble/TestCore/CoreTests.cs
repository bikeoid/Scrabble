using Combinatorics;
using Scrabble.Core;
using Scrabble.Core.Config;
using System;
using System.Diagnostics;

namespace ScrabbleTests
{
    [TestClass]
    public class CoreTests
    {
        private WordLookup wordLookup = null;

        public CoreTests()
        {
            string rootpath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Client\wwwroot");
            string filePath = System.IO.Path.Combine(rootpath, "TWL06a.txt");
            InitializeWordList(filePath);

        }

        private void InitializeWordList(string filePath)
        {
            var wordList = new HashSet<string>();
            using (var inFile = new StreamReader(filePath))
            {
                while (!inFile.EndOfStream)
                {
                    var line = inFile.ReadLine();
                    var word = line.Trim().ToUpper();
                    if (word.Length > 1 && !wordList.Contains(word))
                    {
                        // Ensure list of usable distinct words
                        wordList.Add(word);
                    }
                }
            }
            wordLookup = new WordLookup(wordList, true);
        }


        [TestMethod]
        public void CoordTest()
        {
            var c0 = new Coordinate(8, 0);
            var c1 = new Coordinate(8, 5);
            var range = Coordinate.Between(c0, c1);
            //foreach (var coord in range)
            //{
            //    coord.Print();
            //}
            Assert.AreEqual(range.Count, 6);
            Assert.AreEqual(range[0], c0);
            Assert.AreEqual(range[5], c1);
        }


        [TestMethod]
        public void DictionaryTest()
        {
            var tiles = new List<char> { 'C', 'R', 'N', 'O', 'E', 'R', 'L', };

            var watch = new Stopwatch();
            watch.Start();
            var words = wordLookup.FindAllWords(tiles);
            watch.Stop();

            Assert.AreEqual(words.Count, 52);  // This may fail as words are added to dictionary or different dictionary
            //foreach (var word in words)
            //{
            //    Debug.WriteLine(word);
            //}
            Debug.WriteLine($"DictionaryTest - Found {words.Count} words. Lookup time: {watch.Elapsed.Minutes}min {watch.Elapsed.Seconds}sec {watch.Elapsed.Milliseconds}mSec {watch.Elapsed.Microseconds} uSec");
        }

        [TestMethod]
        public void DictionaryTest2()
        {
            var tiles = new List<char> { 'Y', 'R', 'G', 'U', 'N', 'D', 'A', 'I' };

            var watch = new Stopwatch();
            watch.Start();
            var words = wordLookup.FindWordsUsing(tiles, 0);
            watch.Stop();

            Assert.AreEqual(words.Count, 47);   // This may fail as words are added to dictionary or different dictionary
            //foreach (var word in words)
            //{
            //    Debug.WriteLine(word);
            //}
            Debug.WriteLine($"DictionaryTest2 - Found {words.Count} words. Lookup time: {watch.Elapsed.Minutes}min {watch.Elapsed.Seconds}sec {watch.Elapsed.Milliseconds}mSec {watch.Elapsed.Microseconds} uSec");
        }

        [TestMethod]
        public void TwoLetterWordTest()
        {
            var twoLetterWords = WordLookup.TwoLetterWords;

            Assert.AreEqual(twoLetterWords.Count, 104);   // This may fail as words are added to dictionary or different dictionary
        }


        [TestMethod]
        public void CombinatricsTest()
        {
            int comboCount = 0;
            int letterCount = 0;
            var generator = new CombinationGenerator(7, 2);
            while (generator.HasNext())
            {
                var indices = generator.GetNext();
                comboCount++;
                foreach (var index in indices)
                {
                    //Debug.Write($" {index}");
                    letterCount++;
                }
                //Debug.WriteLine("");
            }

            Debug.WriteLine($"CombinatricsTest - Found {comboCount} combinations, {letterCount} letters");
        }
    }
}