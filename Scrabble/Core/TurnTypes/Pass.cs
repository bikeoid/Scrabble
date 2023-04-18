using System;

namespace Scrabble.Core.Types
{
    [Serializable]
    public class Pass : Turn
    {
        public override void Perform(ITurnImplementor implementor)
        {
            implementor.PerformPass();
        }
    }
}
