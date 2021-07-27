using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fr.guiet.kquatre.business.Exceptions
{
    public class NotPlayableSoundTrackException : Exception
    {
        public NotPlayableSoundTrackException(string message) : base(message)
        {

        }
    }
}
