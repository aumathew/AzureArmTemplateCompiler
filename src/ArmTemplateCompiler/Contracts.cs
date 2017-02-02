using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArmEngine
{
    public class Contracts
    {
        /// <summary>
        /// Helper for null argument check
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="argumentName"></param>
        public static void EnsureArgumentNotNull(object argument, string argumentName)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(argumentName);
            }

            string stringArg = argument as string;

            if (stringArg != null && string.IsNullOrWhiteSpace(stringArg))
            {
                throw new ArgumentNullException(argumentName);
            }
        }
    }
}
