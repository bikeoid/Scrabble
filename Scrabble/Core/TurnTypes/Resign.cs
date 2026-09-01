using System;

namespace Scrabble.Core.Types
{
    [Serializable]
    public class Resign : Turn
    {
        public override void Perform(ITurnImplementor implementor)
        {
            implementor.PerformResign();
        }
    }
}
