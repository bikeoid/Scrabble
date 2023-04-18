using Scrabble.Core.Config;
using System;
using System.Collections.Generic;

namespace Scrabble.Core.Types
{
    [Serializable]
    public class Bag
    {
        public List<Tile> Inventory { get; set; }

        public Bag()
        {
            this.Inventory = new List<Tile>();
        }

        public void InitGameTileBag()
        {
            //populate the bag
            foreach (var letter in ScrabbleConfig.LetterQuantity.Keys)
            {
                var nTiles = ScrabbleConfig.LetterQuantity[letter];
                for (int i = 0; i < nTiles; i++) Inventory.Add(new Tile(letter));
            }
            this.Inventory.Shuffle();
        }

        public bool IsEmpty { get { return this.Inventory.Count == 0; } }

        public void Print()
        {
            foreach (var tile in this.Inventory)
            {
                tile.Print();
            }
        }

        public List<Tile> Take(int n)
        {
            if (this.IsEmpty)
                throw new Exception("The bag is empty, you can not take any tiles.");
            if (n > Inventory.Count) n = Inventory.Count;
            List<Tile> takeList = Inventory.Take<Tile>(n).ToList<Tile>();
            Inventory.RemoveRange(0, n);

            return takeList;
        }

        //public Tile Take() => this.Take(1)[0];

        public void Put(IEnumerable<Tile> tiles)
        {
            this.Inventory.AddRange(tiles);
            this.Inventory.Shuffle();
        }
    }
}