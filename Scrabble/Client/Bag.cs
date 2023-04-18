using Scrabble.Core.Config;
using System;
using System.Collections.Generic;
using System.Windows.Documents;

namespace Scrabble.Core.Types
{
    [Serializable]
    public class Bag
    {
        internal int pointer;
        internal TileList inventory;

        public Bag()
        {
            this.pointer = 0;
            this.inventory = new TileList();
            //populate the bag
            foreach (var letter in ScrabbleConfig.LetterQuantity.Keys)
            {
                var nTiles = ScrabbleConfig.LetterQuantity[letter];
                for (int i = 0; i < nTiles; i++) inventory.Add(new Tile(letter));
            }
            this.inventory.Shuffle();
        }

        public bool IsEmpty => this.inventory.Count == 0;

        public void Print()
        {
            foreach (var tile in this.inventory)
            {
                tile.Print();
            }
        }

        public List<Tile> Take(int n)
        {
            if (this.IsEmpty)
                throw new Exception("The bag is empty, you can not take any tiles.");
            return this.inventory.Draw(Math.Min(this.inventory.Count, n));
        }

        public Tile Take() => this.Take(1)[0];

        public void Put(IEnumerable<Tile> tiles)
        {
            this.inventory.AddRange(tiles);
            this.inventory.Shuffle();
        }
    }
}