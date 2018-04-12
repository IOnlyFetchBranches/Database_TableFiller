using System;
using System.Linq;
using System.Threading;
using TableFiller.Data.Actors;
using TableFiller.Data.Models;
using TableFiller.Util;
using static TableFiller.Util.Logger; //Again use static to simplify from Logger.LogG to just LogG

namespace TableFiller
{
    class Program
    {
        /*
         * Driver class of the program...
         * Although Solutions carry dependencies, it's important to not Linq will not work as well without 
         * the System.Data.Linq Assembly, to enable it [Or any other suspected installed but not found dependencies]:
         * right click on References -> Add Reference -> Assemblies
         * If you have also created a dll, you can click on project to use another project in your solution as a dependency.
         */

            //Our DB
        private static WallyWorldDataContext db = Manager.DB;

        static void Main(string[] args)
        {
            LogG("Begin...");
            LogG("Testing Components...");
            Generators.AddressGen.GenAddress();

            //TODO: Move this down
            /*
            var result7 = Fillers.Inventory.GenerateCategories();
            if (!result7.Equals(new Settings.Return()))
                throw new Exception(result7.Message);
            
             var result8 = Fillers.Inventory.GenerateItems();
            if (!result8.Equals(new Settings.Return()))
                throw new Exception(result8.Message);

                */


            //End move

            var result8 = Fillers.Inventory.GenerateInventories();
            if (!result8.Equals(new Settings.Return()))
                throw new Exception(result8.Message);


            LogG("Ensuring Stores...");
            
            //Fill Stores if empty

            var hasAnyStores = (
                from store in db.StoreInfos
                select store).Any();

            if (hasAnyStores)
            {
                LogG("Found Stores.");
                //Set first store as default
                var defaultStore = (
                    from store in db.StoreInfos
                    select store).First();

                //Set it
                Settings.Defaults.Store = defaultStore.STRID;

                LogG("Checking Depts.");
                //Create departments for the store if needed
                var hasDepts = (
                    from depts in db.Departments
                    where depts.STRID == defaultStore.STRID
                    select depts).Any();

                if (!hasDepts)
                {
                    LogG("No Depts found, creating...");
                    //If it doesnt have any, then make them
                    var result= Fillers.Departments.CreateFor(defaultStore.STRID);
                    if (!result.Equals(new Settings.Return()))
                        Logger.LogErr("Fillers:Dept", result.Message);
                    else
                    {
                        LogG("Departments Exist Now...");

                    }
                }
            }
            else
            {
                //Create stores
                var result = Fillers.StoreInfo.Fill(5);

                if (!result.Equals(new Settings.Return()))
                    throw new Exception(result.Message);
           



               

                //Create depts
                
                var defaultStore = (
                    from store in db.StoreInfos
                    select store).First();

               

                //Set it
                Settings.Defaults.Store = defaultStore.STRID;

                LogG("Stores Created... \nDef STRID:" + Settings.Defaults.Store);


                LogG("Creating Depts...");

                var result3 = Fillers.Departments.CreateFor(defaultStore.STRID);
                if (!result3.Equals(new Settings.Return()))
                    Logger.LogErr("Fillers:Dept", result3.Message);
                else
                {
                    LogG("Departments Exist Now...");

                }
             

                //Create default employees
                var result4 = Fillers.Employees.Fill(100, Settings.Defaults.Store);

                if (!result4.Equals(new Settings.Return()))
                    Logger.LogErr("Fillers:StoreInfo", result4.Message);
               


                //Set Mangers
                var result5 = Fillers.Employees.Managers.UpdateFor(defaultStore.STRID);

                if (!result5.Equals(new Settings.Return()))
                    throw new Exception(result5.Message);


                //Set Wages
                Fillers.Employees.Wages.Make();


                //Set Contacts
                var result6 = Fillers.Employees.Addresses.Fill();


                result6 = Fillers.Employees.Addresses.MakeEmergency();

                if (!result6.Equals(new Settings.Return()))
                    throw new Exception(result6.Message);


            



            }

            //For now we will just take the first store and set it as default. {Comment to search for= Set it}
            //However if you wish you may remove line that assigns it above and sub your own in Settings.Defaults.Store



            //Close Filter
            Generators.AddressGen.CloseFilter();
            Generators.IDGen.CloseFilter();
            Generators.NameGen.CloseFilter();








            //List departments in database.
            var result2 = Listers.Departments.List();


            LogG("System", "Database Connection OK");

          

            //Show Menu

            WriteL("");

            WriteL("\t\tYou can exit now!");

            while (true)
            {
                Thread.Sleep(1500);
            }
            
        }
    }
}
