using System;
using Scrabble.Core.Config;
using Scrabble.Core.Types;


namespace Scrabble.Core.Squares
{
    [Serializable]
    public abstract class Square
    {
        private int wordMult;

        private int letterMult;

        private string label;

        private string cssSelector;

        private Tile tile;



        public int LetterMultiplier => letterMult;

        public int WordMultiplier => wordMult;

        public Tile Tile
        {
            get
            {
                return tile;
            }
            set
            {
                tile = value;
            }
        }

        public string ID { get; set; }

        public event EventHandler SquareRefreshed;

        public Coordinate BoardPosition { get; set; }

        public void RefreshNotify(EventArgs e)
        {
            SquareRefreshed?.Invoke(this, e);
        }

        public bool IsEmpty => (tile == null);

        public string CssSelector => cssSelector;

        public string Label => label;

        public Square(int letterMult, int wordMult, string cssSelector, string label)
        {
            this.letterMult = letterMult;
            this.wordMult = wordMult;
            this.cssSelector = cssSelector;
            this.label = label;
            tile = null;
            BoardPosition = null;
        }
    }
}
