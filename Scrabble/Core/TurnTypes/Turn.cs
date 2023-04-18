using System;

namespace Scrabble.Core.Types
{
    [Serializable]
    public abstract class Turn
    {
        public abstract void Perform(ITurnImplementor P_0);

        public Turn()
        {
        }
    }
}
