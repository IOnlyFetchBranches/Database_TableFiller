using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TableFiller.Data.Models;

namespace TableFiller.Data.Actors
{
    //Literally just holds our master db reference
    abstract class Manager
    {
       internal static  readonly WallyWorldDataContext DB =new WallyWorldDataContext(Settings.ConnectionStrings.Localhost);
    }
}
