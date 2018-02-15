using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TableFiller.Data.Models;
using static TableFiller.Util.Logger;

namespace TableFiller.Data.Actors
{
    /// <summary>
    /// Provides methods for listing various tables...
    /// </summary>
    //This class lists tables
    public abstract class Listers
    {
        public static  class Departments
        {
            //Grab our db again
            internal static WallyWorldDataContext db = Manager.DB;
                
            public static Settings.Return List()
            {


                try
                {
                    LogG("Printing [DID,NAME,STRID,MEID, LastRestock");

                    foreach (var row in db.Departments)
                    {
                        WriteL(row.DID + " " + row.DPRTName + " " + row.STRID + " " + row.MEID + " " +
                               row.DPRTLastRestock);
                    }

                    return new Settings.Return(Settings.ReturnResult.Ok);
                }
                catch (Exception e)
                {
                    return new Settings.Return(Settings.ReturnResult.Bad)
                        .WithMessage(e.Message); //Is the same as new Settings.Return(e.Message Settings.ReturnResult.Bad)
                }
            }

            
        }
    }

   
}
