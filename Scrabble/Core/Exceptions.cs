using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scrabble.Core
{
  

    /// <summary>
    /// User - defined exception for the application, to assist in cases
    /// where application exception handling needs to be separated from all other exceptions
    /// </summary>
    public class UnsupportedCoordinateException : Exception
    {
        public UnsupportedCoordinateException()
        {
        }

        public UnsupportedCoordinateException(string message)
            : base(message)
        {
        }

        public UnsupportedCoordinateException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class InvalidMoveException : Exception
    {
        public InvalidMoveException()
        {
        }

        public InvalidMoveException(string message)
            : base(message)
        {
        }

        public InvalidMoveException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }


}
