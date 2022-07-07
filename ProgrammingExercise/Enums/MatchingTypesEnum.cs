using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgrammingExercise
{
    [Flags]
    public enum MatchingTypesEnum
    {
        Phone = 0,
        Phone1 = 1,
        Phone2 = 3,
        Email = 4,
        Email1 = 5,
        Email2 = 6,
        Zip = 7,
        FirstName = 8,
        LastName =9
    }
}
