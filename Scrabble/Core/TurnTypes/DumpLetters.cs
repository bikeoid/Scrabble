using System;
using System.Collections.Generic;

namespace Scrabble.Core.Types
{
    [Serializable]
    public class DumpLetters : Turn
    {
        internal IEnumerable<Tile> letters;

        public IEnumerable<Tile> Letters => letters;

        public DumpLetters(IEnumerable<Tile> letters)
        {
            this.letters = letters;
        }

        public override void Perform(ITurnImplementor implementor)
        {
            implementor.PerformDumpLetters(this);
        }
    }
}
