using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TableFiller.Data.Models;
using TableFiller.Util;
using TableFiller.Util.Models;
using static System.Console;
using static TableFiller.Util.Generators.IDGen;
using static TableFiller.Util.Logger;

//Make the static import to make simple Console Write calls.

namespace TableFiller.Data.Actors
{
    //When we get to filling Employee Contact info, besure to use the Store that each employee is assigned to a base for their address.
    //I.e a store in Altanta would not have an employee working in it's departmenet with a contact address in statesboro.
    public abstract class Fillers
    {
        //Going to be using LinQ here
        //First grab the Data Context we created from the Linq to Sql class [Data\Models]
        internal static WallyWorldDataContext db = Manager.DB;
        #region Employee Filler
        public abstract class Employees 
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="amount">Amount of records to populate</param>
            /// <param name="STRID">Store to populate for</param>
            /// <returns></returns>
            public static Settings.Return Fill(int amount, string STRID)
            {
                //For randomness
                Random rand = new Random(DateTime.Today.Millisecond);
                //List that we add All Our Employee Models to
                List<Employee> newEmployess = new List<Employee>();

              
                LogG("Filling Store:"+STRID);

                //List of depts FROM THE SAME STORE
                var deptList = (
                    from depts in db.Departments
                    where depts.STRID == STRID
                    select depts
                ).ToList();

                if (!deptList.Any())
                {
                    return new Settings.Return("Could not find any depts matching the provided strid!",Settings.ReturnResult.Bad);
                }

                

                                
                //Calculate manger spots Num of depts - num of managers...
                int mangerSpots = deptList.Count - (
                                      from emp in db.Employees
                                      where emp.Position == "Manager"
                                      select emp).Count();
                var indexInDeptList = 0;
                var indexInPositionList = 0;
                LogG("Manger Count:" + mangerSpots);
                //Generate randomness here
                for (int x = 0; x < amount; x++)
                {
                    
                    //Going to be using LinQ here
                    var newEmployee = new Employee();

                    newEmployee.CurrentStatus = "Active";
                    newEmployee.EnrollmentDate = DateTime.Now;
                    newEmployee.OffDaysEarned = 10; //Default offdays is 10.
                    newEmployee.OffDaysUsed = 0;
                    //Assign depts as evenly as possible...
                    newEmployee.DID = deptList[indexInDeptList].DID;

                    var dptRecord = (
                        from dept in db.Departments
                        where dept.DID == newEmployee.DID
                        select dept
                        ).Single();

                    newEmployee.STRID = dptRecord.STRID;

                    //Generate Position In Departments
                    List<string> positionList;
                    switch (deptList[indexInDeptList].DPRTName)
                    {
                        case "Produce":
                            positionList = new List<string>()
                            {
                                "Associate","Stocker","Checkout","Janitor"
                            };
                            break;
                        case "Gardening":
                            positionList = new List<string>()
                            {
                                "Associate","Stocker","Checkout","Janitor","Gardener"
                            };
                            break;
                        case "Automotive":
                            positionList = new List<string>()
                            {
                                "Associate","Stocker","Mechanic","Janitor"
                            };
                            break;
                        case "Electronics":
                            positionList = new List<string>()
                            {
                                "Associate","Stocker","Checkout","Janitor","Repair Technician"
                            };
                            break;
                        case "General":
                            positionList = new List<string>()
                            {
                                "Associate","Loss Prevention","Checkout","Janitor","Cart Collector","IT"
                            };
                            break;
                        default:
                            throw new Exception("Dept is invalid...");
                            
                               
                    }

                    //If within the first few , make manger
                    if (x < mangerSpots)
                    {
                        newEmployee.Position = "Manager";
                    }
                    else
                    {
                        if (indexInPositionList >= positionList.Count)
                        {
                            indexInPositionList=0;
                        }
                        
                        //Random.Next() upper bound is EXCLUSIVE
                        newEmployee.Position = positionList[indexInPositionList];
                        indexInPositionList++;
                       
                    }

                    //Increment
                    indexInDeptList++;
                    //Reset if needed
                    if (indexInDeptList == deptList.Count)
                        indexInDeptList = 0;

                    //Handle names
                    Name fullName = Generators.NameGen.Gen(false, false);

                    LogG("Current Emp:" + fullName);

                    //Assign
                    newEmployee.FirstName = fullName.First;
                    newEmployee.LastName = fullName.Last;

                    

                    //Finally gen the EID using the name
                    newEmployee.EID = EID(fullName.First, fullName.Last, newEmployee.DID,
                        newEmployee.Position);
                    
                    

                    //Add to Employees
                    newEmployess.Add(newEmployee);

                    //In Our Employee Contact Filler we'll need to make sure that the Assigned Address Make sense as compared to the store's info.



                }

                LogG("Finished Generating employees.");

                foreach (Employee e in newEmployess)
                {
                    WriteL(e.EID + " "+ e.FirstName + " " + e.LastName +" " + e.Position);
                }

                //Going to be using LinQ here
                db.Employees.InsertAllOnSubmit(newEmployess);
                db.SubmitChanges(ConflictMode.FailOnFirstConflict);

            

                
                return new Settings.Return();

            }

            //Inner class for populating all the MEID Fields in other classes
            public abstract class Managers
            {
                /// <summary>
                /// This should be Called by default
                /// Assigns Department managers
                /// </summary>
                /// <param name="STRID"></param>
                /// <returns></returns>
                public static Settings.Return UpdateFor(string STRID)
                {
                    LogG("Updating Manager IDs");


                    //First get all managers
                    var managers = new List<Employee>();

                    foreach (Employee e in db.Employees)
                    {
                        if (e.Position == "Manager")
                            if(e.Department.STRID == STRID)
                                managers.Add(e);

                    }





                    /*
                    //NO longer going this route!
                    //Set each field
                    foreach (var manager in managers)
                    {
                        // ReSharper disable once ReplaceWithSingleCallToFirst
                        var result = (
                            from dept in db.Departments
                            where dept.DID == manager.DID
                            select dept).SingleOrDefault();
                        if(result == null)
                            return new Settings.Return("Failed to find matching Manager for DID " + manager.DID 
                                + " " +manager.FirstName + " " +manager.LastName, 
                                    Settings.ReturnResult.Critical);

                        result.MEID = manager.EID;
                       
                         No longer doing this method!
                        if (manager.Department.DPRTName.Contains("General"))
                        {
                            var managersStore = (
                                from store in db.StoreInfos
                                where store.STRID.Contains(STRID)
                                select store
                            ).SingleOrDefault();

                            managersStore.MEID = manager.EID;
                        }
                        */

                        //Push Changes
                        db.SubmitChanges();
        

                    


                    return new Settings.Return();
                }
            }

            public abstract class Wages
            {
                public static void Make()
                {

                    var employees = db.Employees;


                    foreach (Employee emp in employees)
                    {
                        EmployeeWage temp;
    
                        switch (emp.Position)
                        {
                            case "Associate":

                                temp = new
                                    EmployeeWage()
                                    {
                                        Bonuses = 1000.75,
                                        EID = emp.EID,
                                        HasDirectDeposit = false,
                                        HoursWorked = new Random().Next(25, 40),
                                        Wage = 14.50
                                    };
                                temp.FederalTaxesPaid =
                                    (double?)((temp.Wage * ((temp.HoursWorked * 4 * 12)) + 1000.75) * .1);
                                temp.StateTaxesPaid = (temp.FederalTaxesPaid * (double?).5);
                                temp.CurrentSalary = (temp.Wage) * ((temp.HoursWorked * 4 * 12)) + 125.75;
                                temp.ExpectedSalary = temp.CurrentSalary - temp.Bonuses;
                                db.EmployeeWages.InsertOnSubmit(temp);
                                break;
                            case "Loss Prevention":

                                temp = new
                                    EmployeeWage()
                                    {
                                        Bonuses = 1000.75,
                                        EID = emp.EID,
                                        HasDirectDeposit = false,
                                        HoursWorked = new Random().Next(25, 50),
                                        Wage = 13.50
                                    };
                                temp.FederalTaxesPaid =
                                    (double?)((temp.Wage * ((temp.HoursWorked * 4 * 12)) + 1000.75) * .1);
                                temp.StateTaxesPaid = (temp.FederalTaxesPaid * (double?).5);
                                temp.CurrentSalary = (temp.Wage) * ((temp.HoursWorked * 4 * 12)) + 125.75;
                                temp.ExpectedSalary = temp.CurrentSalary - temp.Bonuses;
                                db.EmployeeWages.InsertOnSubmit(temp);
                                break;
                            case "Cart Collector":

                                 temp = new
                                    EmployeeWage()
                                    {
                                        Bonuses = 125.75,
                                        EID = emp.EID,
                                        HasDirectDeposit = false, //false for now
                                        HoursWorked = new Random().Next(25, 40),
                                        Wage = 7.25
                                    };

                                temp.FederalTaxesPaid =
                                    (double?)((temp.Wage * ((temp.HoursWorked * 4 * 12)) + 1000.75) * .1);
                                temp.StateTaxesPaid = (temp.FederalTaxesPaid * (double?).5);
                                temp.CurrentSalary = (temp.Wage) * ((temp.HoursWorked * 4 * 12)) + 125.75;
                                temp.ExpectedSalary = temp.CurrentSalary - temp.Bonuses;
                                db.EmployeeWages.InsertOnSubmit(temp);
                                break;
                            case "Janitor":

                                temp = new
                                    EmployeeWage()
                                    {
                                        Bonuses = 570.50,
                                        EID = emp.EID,
                                        HasDirectDeposit = false, //false for now
                                        HoursWorked = new Random().Next(25, 40),
                                        Wage = 10.50
                                    };

                                temp.FederalTaxesPaid =
                                    (double?)((temp.Wage * ((temp.HoursWorked * 4 * 12)) + 1000.75) * .1);
                                temp.StateTaxesPaid = (temp.FederalTaxesPaid * (double?).5);
                                temp.CurrentSalary = (temp.Wage) * ((temp.HoursWorked * 4 * 12)) + 125.75;
                                temp.ExpectedSalary = temp.CurrentSalary - temp.Bonuses;
                                db.EmployeeWages.InsertOnSubmit(temp);
                                break;
                            case "Checkout":

                                temp = new
                                    EmployeeWage()
                                    {
                                        Bonuses = 3750.00,
                                        EID = emp.EID,
                                        HasDirectDeposit = false, //false for now
                                        HoursWorked = new Random().Next(35, 50),
                                        Wage = 15.00
                                    };

                                temp.FederalTaxesPaid =
                                    (double?)((temp.Wage * ((temp.HoursWorked * 4 * 12)) + 1000.75) * .1);
                                temp.StateTaxesPaid = (temp.FederalTaxesPaid * (double?).5);
                                temp.CurrentSalary = (temp.Wage) * ((temp.HoursWorked * 4 * 12)) + 125.75;
                                temp.ExpectedSalary = temp.CurrentSalary - temp.Bonuses;
                                db.EmployeeWages.InsertOnSubmit(temp);
                                break;

                            case "Stocker":

                                temp = new
                                    EmployeeWage()
                                    {
                                        Bonuses = 100.00,
                                        EID = emp.EID,
                                        HasDirectDeposit = false, //false for now
                                        HoursWorked = new Random().Next(40, 50),
                                        Wage = 9.00
                                    };
                                temp.FederalTaxesPaid =
                                    (double?)((temp.Wage * ((temp.HoursWorked * 4 * 12)) + 1000.75) * .1);
                                temp.StateTaxesPaid = (temp.FederalTaxesPaid * (double?).5);
                                temp.CurrentSalary = (temp.Wage) * ((temp.HoursWorked * 4 * 12)) + 125.75;
                                temp.ExpectedSalary = temp.CurrentSalary - temp.Bonuses;
                                db.EmployeeWages.InsertOnSubmit(temp);
                                break;

                            case "Manager":

                                temp = new
                                    EmployeeWage()
                                    {
                                        Bonuses = 7750.00,
                                        EID = emp.EID,
                                        HasDirectDeposit = false, //false for now
                                        HoursWorked = new Random().Next(35, 50),
                                        Wage = 18.50
                                    };
                                temp.FederalTaxesPaid =
                                    (double?)((temp.Wage * ((temp.HoursWorked * 4 * 12)) + 1000.75) * .1);
                                temp.StateTaxesPaid = (temp.FederalTaxesPaid * (double?).5);
                                temp.CurrentSalary = (temp.Wage) * ((temp.HoursWorked * 4 * 12)) + 125.75;
                                temp.ExpectedSalary = temp.CurrentSalary - temp.Bonuses;
                                db.EmployeeWages.InsertOnSubmit(temp);
                                break;

                            case "Gardener":

                                temp = new
                                    EmployeeWage()
                                    {
                                        Bonuses = 1150.75,
                                        EID = emp.EID,
                                        HasDirectDeposit = false, //false for now
                                        HoursWorked = new Random().Next(25, 30),
                                        Wage = 15.00
                                    };

                                temp.FederalTaxesPaid =
                                    (double?)((temp.Wage * ((temp.HoursWorked * 4 * 12)) + 1000.75) * .1);
                                temp.StateTaxesPaid = (temp.FederalTaxesPaid * (double?).5);
                                temp.CurrentSalary = (temp.Wage) * ((temp.HoursWorked * 4 * 12)) + 125.75;
                                temp.ExpectedSalary = temp.CurrentSalary - temp.Bonuses;
                                db.EmployeeWages.InsertOnSubmit(temp);
                                break;

                            case "IT":

                                temp = new
                                    EmployeeWage()
                                    {
                                        Bonuses =  9000,
                                        EID = emp.EID,
                                        HasDirectDeposit = false, //false for now
                                        HoursWorked = new Random().Next(35, 40),
                                        Wage = 18.50
                                    };

                                temp.FederalTaxesPaid =
                                    (double?)((temp.Wage * ((temp.HoursWorked * 4 * 12)) + 1000.75) * .1);
                                temp.StateTaxesPaid = (temp.FederalTaxesPaid * (double?).5);
                                temp.CurrentSalary = (temp.Wage) * ((temp.HoursWorked * 4 * 12)) + 125.75;
                                temp.ExpectedSalary = temp.CurrentSalary - temp.Bonuses;
                                db.EmployeeWages.InsertOnSubmit(temp);
                                break;

                            case "Mechanic":

                                temp = new
                                    EmployeeWage()
                                    {
                                        Bonuses = 10000.75,
                                        EID = emp.EID,
                                        HasDirectDeposit = false, //false for now
                                        HoursWorked = new Random().Next(25, 35),
                                        Wage = 21.00
                                    };

                                temp.FederalTaxesPaid =
                                    (double?)((temp.Wage * ((temp.HoursWorked * 4 * 12)) + 1000.75) * .1);
                                temp.StateTaxesPaid = (temp.FederalTaxesPaid * (double?).5);
                                temp.CurrentSalary = (temp.Wage) * ((temp.HoursWorked * 4 * 12)) + 125.75;
                                temp.ExpectedSalary = temp.CurrentSalary - temp.Bonuses;
                                db.EmployeeWages.InsertOnSubmit(temp);
                                break;

                            case "Repair Technician":

                                temp = new
                                    EmployeeWage()
                                    {
                                        Bonuses = 11000.75,
                                        EID = emp.EID,
                                        HasDirectDeposit = false, //false for now
                                        HoursWorked = new Random().Next(25, 35),
                                        Wage = 21.00
                                    };

                                temp.FederalTaxesPaid =
                                    (double?) ((temp.Wage * ((temp.HoursWorked * 4 * 12)) + 1000.75) * .1);
                                temp.StateTaxesPaid = (temp.FederalTaxesPaid * (double?).5);
                                temp.CurrentSalary = (temp.Wage) * ((temp.HoursWorked * 4 * 12)) + 125.75;
                                temp.ExpectedSalary = temp.CurrentSalary - temp.Bonuses;
                                db.EmployeeWages.InsertOnSubmit(temp);
                                break;

                            default:
                                throw new Exception("Can't find Position, Add it to filter statements or remove position! " + emp.Position);


                        }


                    }


                    db.SubmitChanges(); //Commit to backend.

                }
            }

            public abstract class Addresses
            {
                public static Settings.Return Fill()
                {
                    try
                    {
                        var rand = new Random(DateTime.Now.Millisecond * 4);
                        int[] prefixInts = new int[] {678, 912, 404, 212, 556, 876, 940, 117, 200, 808};
                        string[] emailSuffixs = new string[]
                        {
                            "gmail.com", "yahoo.com", "hotmail.com", "outlook.com", "ymail.com", "aol.net",
                            "bellsouth.net"
                        };
                        foreach (Employee emp in db.Employees)
                        {
                            EmployeeContact contact = new EmployeeContact();
                            //Decide if in apt.
                            if (rand.Next(0, 100) % 7 == 0)
                            {
                                contact.Apt_ = rand.Next(1, 1000);
                            }

                            //Set the PK
                            contact.EID = emp.EID;


                            //Gen phone numbers here
                            contact.CellPhoneNumber =
                                long.Parse(prefixInts[rand.Next(prefixInts.Length)] + "" + rand.Next(100, 999) + "" +
                                           rand.Next(1000, 9999));

                            //Assign Regional Constants
                            contact.City = emp.Department.StoreInfo.SCity;
                            contact.State = emp.Department.StoreInfo.SState;
                            contact.Zip = emp.Department.StoreInfo.SZip;
                            //Generate Street
                            contact.Street = Generators.AddressGen.GenAddress().Street;

                            contact.PrimaryEmail = emp.FirstName.Substring(0, 1) + emp.LastName + "@" +
                                                   emailSuffixs[rand.Next(emailSuffixs.Length)];

                            if (rand.Next(0, 100) % 3 == 0)
                                contact.SecondaryEmail = emp.LastName + emp.FirstName + "@" +
                                                         emailSuffixs[rand.Next(emailSuffixs.Length)];
                            else
                            {
                                contact.SecondaryEmail = null;
                            }


                            db.EmployeeContacts.InsertOnSubmit(contact);

                        }

                        //Transfer
                        db.Refresh(RefreshMode.KeepChanges);
                        db.SubmitChanges();


                        //Done. Hopefully...
                        return new Settings.Return();
                    }
                    catch (Exception e)
                    {
                        return new Settings.Return(e.Message, new Settings.Status(Settings.ReturnResult.Bad));
                    }

                }

                public static Settings.Return MakeEmergency()
                {
                    try
                    {
                        //Get list of all contacts

                        var rand = new Random(DateTime.Now.Millisecond * 3);
                        var contacts = (
                            from contact
                                in db.EmployeeContacts
                            select contact).ToList();

                        foreach (var contact in db.EmployeeContacts)
                        {
                            //Get emergency contact by chance
                            var emergencyContact = contacts[rand.Next(0, contacts.Count)];

                            contact.EmergencyEmail = emergencyContact.PrimaryEmail;
                            contact.EmergencyFirstName = emergencyContact.Employee.FirstName;
                            contact.EmergencyLastName = emergencyContact.Employee.LastName;
                            contact.EmergencyPhoneNumber = emergencyContact.CellPhoneNumber;

    



                        }

                        //Commit/Transfer
                        db.SubmitChanges();
                        return new Settings.Return();
                    }
                    catch (Exception e)
                    {
                        return new Settings.Return(e.Message, new Settings.Status(Settings.ReturnResult.Bad));
                    }

                }
            }
        }
        #endregion

        #region Department Fillers
    

         public abstract class Departments
        {
            public static Settings.Return CreateFor(string STRID)
            {
                
                try
                {
                    //This is only used to create new tables, so check for a filled table
                    if (db.Departments.AsQueryable().Any())
                    {
                        return new Settings.Return("Table already filled!", Settings.ReturnResult.Warning);
                    }
                   
                    //Example forech
                    foreach (string s in Settings.DepartmentList)
                    {
                        Department dpt = new Department();

                        dpt.DID = DID(s, STRID);
                        dpt.STRID = STRID;
                        dpt.DPRTName = s;
                        dpt.DPRTLastRestock = DateTime.Now;
                        

                        //Now add to LinQ Queue 
                        db.Departments.InsertOnSubmit(dpt);
                    }

                    //Always submit last, it's more efficient, although there are exceptions.
                    //An exception to this would be if you need to act on, or with, a modified field among other things...
                    db.Refresh(RefreshMode.KeepChanges);
                    db.SubmitChanges();

                    //Return result
                    return new Settings.Return(); //Default Return is an 'Ok', with a status of Ok
                }
                catch (Exception e)
                {
                    return new Settings.Return(e.Message + "\n" + e.StackTrace, Settings.ReturnResult.Critical);
                }
            }
        }

        #endregion

        #region StoreInfo Fillers
        public abstract class StoreInfo
        {
            public static Settings.Return Fill(int amount)
            {
                //Lets try to do this recursively from now on...
                if (amount == 0)
                {
                    return new Settings.Return();
                }

                try
                {
                    var deptTable = db.Departments.AsQueryable();

                    var info = new Models.StoreInfo();
                    
                    var addr = Generators.AddressGen.GenAddress();

                    info.SCity = addr.Region.City;
                    info.SStreet = addr.Street;
                    info.SState = addr.Region.State;
                    info.SZip = addr.Region.Zip;

                    //Gen the store ID
                    //It's safe to use the address object as a creation point with STRIDs, thanks to the overloaded ToString
                    info.STRID = STRID(addr+"");


                    //Add Row Model Object to update queue
                    db.StoreInfos.InsertOnSubmit(info);

                    //Submit
                    db.Refresh(RefreshMode.KeepChanges);
                    db.SubmitChanges();

                    //Print 
                    LogG("Added: " + addr);


                    return Fill(amount - 1); //Recursive Beauty <3
                }
                catch(Exception e)
                {
                    LogErr("Fillers:StoreInfo",e.Message + "\nInner:\n" + e.InnerException + "\nTrace:\n" + e.StackTrace );
                    return new Settings.Return(e.Message, Settings.ReturnResult.Bad);
                }
            }
        }
        #endregion

        public abstract class Orders
        {
            public static Settings.Return GenerateOrdersForCustomers()
            {
                
                //First, we need Items, to fill the inventory infos


                return new Settings.Return();
            }
        }

        public abstract class Inventory
        {

            //TODO:Continue adding to this.
        #region Define category strings
            #region General
            internal static List<string> _generalCats = new List<string>()
            {
                "frozen","chilled","baked","shelved","cooking","cleaning","personalhygenie"
            };

           

            internal static List<string> _genFrozenItems = new List<string>()
            {
                "pizza","carrots_frozen","broccoli_frozen","assorted_fruit_frozen","ice_cream_vanilla","ice_cream_assorted","chicken_strips",
                "cheese_mac_frozen"+"italian_ice","sunday_cones","popsicles","ice_cream_sandwhiches","pizza_rolls","french_fries",
                "waffles_frozen","french_toast_sticks","pretzel_frozen","corn_frozen","ice_bag","boxed_dinner_frozen", "sausage_frozen",
                
            };

            internal static List<string> _genChilledItems = new List<string>()
            {
                "almond_milk","coconut_milk","skim_milk","whole_milk","light_milk","beef","turkey","cookie_dough","juice","flavored_water","sports_drink","beer","liqour",
                "fish","whipped_cream","american_cheese_slices","cheddar_shreaded","cheddar_slices","colby_jack_shredded","premium_beef","burger_patties","steak",
                "turkey_slices","eggs_ten_count","eggs_6_count","eggs_16_count"
            };

            internal static List<string> _genBakedItems = new List<string>()
            {
                "muffin","muffins_size_count","birthday_cake","raisin_bread_cinnamon","raisin_bread","brownies_mini_sixteen_count",
                "brownies_mini_eight_count","choco_chunk_six_count","choco_chunk_ten_count","sugar_cookies_six_count","sugar_cookies_ten_count",
                "yeast_roll_twelve_pack","bread_sticks_ten_count","snickerdoodles_ten_count","snickerdoodles_six_count"
            };

            internal static List<string> _genShelvedItems = new List<string>()
            {
                "ketchup","mustard","pickles","pasta","barbeque_sauce","marshmallows","taco_shells","taco_sauce","pasta_sauce",
                "brown_rice","jasmine_rice","white_rice","sugar","cake_mix","brownie_mix","oatmeal","instant_oatmeal","coffee_beans",
                "ground_coffee","hot_chocolate","granola_bars","potato_chips","cereal_boxed","poptarts","candy","assorted_candy","chocolate_bar",
                "hot_chocolate","tea","boxed_mac_cheese","cup_ramen","packet_ramen","mayonnaise","flour","salt","cinnamon","nutmeg","oregano",
                "basil","seasoning_salt","cereal_bagged","applesauce","sandwich_cookies","packaged_cookies","shortbread_cookies","bottled_water",
                "bottled_water_pack","canned_beans","canned_fruit","canned_vegetables","canned_soup","canned_pasta","protein_bars"
            };

            internal static List<string> _genCookingItems = new List<string>()
            {
                "pot_small","pot_medium","pot_large","crock_pot","nonstick_pan_nonceramic","nonstick_pan_ceramic","utensil_set",
                "cooking_knife","pizza_cutter","tupperware","coffee_maker","blender","microwave","spatula","toaster_oven","herb_grinder",
                "coffee_grinder","mixer_automatic","mixer_manual","water_filter"
            };

            internal static List<string> _genCleaningItems = new List<string>()
            {
                "paper_towels_singleroll","paper_towels_six_rolls","sanitizing_kitchen_wipes", "kitchen_cleaner","bathroom_cleaner",
                "sponges_six_count","mop","broom","scrubber","steel_wool","wash_towel","latex_gloves_twentyfive_count","trash_bags_twelve_count",
                "glass_cleaner","floor_cleaner","liquid_plumber","plunger","toilet_brush","bottle_brush","bucket","trash_can", "dish_soap","dishwashing_liquid",
                "jetdry","dish_rack"

            };

            internal static List<string> _genHygieneList = new List<string>()
            {
                "deodorant_stick","deodorant_spray","bar_soap_six_count","toothbrush","toothpaste","facial_soap","hand_soap",
                "mouthwash","electric_toothbrush","hairspray","hairbrush","body_wipes","toilet_paper_twelve_rolls","toilet_paper_four_rolls",
                "body_spray","face_wash","shampoo","dandruff_shampoo","face_mask","dental_floss","shaving_razor","shaving_cream","cotton_balls",
                "qtips","antibacterial_cream","itch_cream","bandages"
            };

            #endregion

            #region Produce
            internal static List<string> _produceCatList = new List<string>()
            {
                "fruit","vegetable"
            };

            internal static List<string> _produceFruit = new List<string>()
            {
                "fiji_apple",
                "avocado",
                "pineapple",
                "mango",
                "dragonfruit",
                "cherries",
                "grapes",
                "kiwi",
                "strawberries",
                "bannana",
                "pear",
                "orange",
                "watermelon",
                "cantelope",
                "coconut",
                "tomato",
                "red_delicious_apple",
                "super_apple",
                "blueberries",
                "rasberries",
                "lemon",
                "lime"
            };
            
            internal static List<string> _produceVegetable = new List<string>()
            {
                "potato","broccoli","lettuce","carrots","spinach","squash","onion",
                "cabbage","cauliflower","peas","celery","kale","eggplant","radish",
                "zucchini","radish","garlic","green_beans","collard_greens","chili_pepper",
                "green_pepper"
            };



            #endregion

            #region Electronics

            internal static List<string> _electronicCatList = new List<string>()
            {
                "audio","televisions","game_consoles","computers","computer_accesories",
                "game_accessories"
            };

            internal static List<string> _elecAudioList = new List<string>()
            {
                "generic_speakers","pro_speakers","surround_sound_set","subwoofer"
            };

            internal static List<string> _elecTeleList = new List<string>()
            {
                "flat_screen_4k_large","flat_screen_4k_medium","flat_screen_4k_monitor",
                "flat_screen_1080_large","flat_screen_1080_medium","flat_screen_1080_monitor",

            };

            internal static List<string> _elecConsoleList = new List<string>()
            {
                "Xbox One","Xbox One X","PS4","PS4 Pro","Nintendo Switch","Nintendo Wii U","PSP Vita"
            };

            internal static List<string> _elecCompList = new List<string>()
            {
                "low_end_laptop","mid_range_laptop","premium_laptop","gaming_laptop",
                "low_end_pc","mid_range_pc","premium_pc","gaming_pc","macbook_pro","macbook_air",
                "iMac","mac_pro"
            };

            //TODO:Finish elec item list
            internal static List<string> _elecCompAccList = new List<string>()
            {
                "mouse","keyboard","mechanical_keyboard","led_mouse","headset","microphone",
                "webcam","usb_controller"
            };

            //TODO:Finish elec item list
            internal static List<string> _elecGameAccList = new List<string>()
            {
                "gaming_headset","ps4_controller_wireless","ps4_controller_wired",
                "xboxone_controller_wireless","xboxone_controller_wired"

            };



            #endregion

            #region Gardening
            //TODO:Finish elec item list
            internal static List<string> _gardCatList = new List<string>()
            {
                "tools","organic"
            };

            //TODO:Finish elec item list
            internal static List<string> _gardToolsList = new List<string>()
            {
                "edger","rake","wheelbarrow","lawn_mower","garden_hose","pruners","dutch_hoe",
                "sheers"
            };

            internal static List<string> _gardOrganicList = new List<string>()
            {
                "soil","carnations","daffodils","cherry_blossoms","roses","sunflowers","poinsettias","tulips",
                "lilies"
            };
            #endregion

            #region Automotive

            //TODO:Finish elec item list
            internal static List<string> _autoCatList = new List<string>
            {
                "interior_components","maintenance"
            };
            internal static List<string> _autoCompList = new List<string>
            {
                "car_stereo","subwoofer","amp","car_speaker", "steering_wheel_cover",
                "seat_cover","floor_mat"
            
            };
            internal static List<string> _autoMainList = new List<string>
            {
                "tar_paint_bug_remover","wheel_tire_cleaner","fuse_kit","headlight_front","headlight_back",
                "detailer","wax","car_battery","engine_oil","brake_fluid","rain_repellant","windshield_wiper",
                "antifreeze"
            };



            #endregion
            #endregion

            

            public static Settings.Return GenerateInventories()
            {

                //Check if this needs to run.
                var hasInventories = db.Inventories
                                         .GetEnumerator()
                                         .Current != null;
                if (hasInventories)
                {
                    //Return prematurely 
                    return new Settings.Return("Prexisting inventories were found!", Settings.ReturnResult.Warning);
                
                }

                var invInfos = new Dictionary<string, InventoryInfo>();



                //For each department
                foreach (Department dept in db.Departments)
                {
                    //Generate a new inventory
                    Models.Inventory inventory = new Models.Inventory();
                    inventory.InvID = InvID(dept.DPRTName, dept.STRID);
                    inventory.Department = dept;
                   
                    //Grab all related items
                    //TODO:Fill in if/elses
                    if (dept.DPRTName == "General")
                    {
                        var relatedItems = (
                            from x in db.Items
                            where _generalCats.Contains(x.Name)
                            select x
                        ).ToList();

                        foreach (var item in relatedItems)
                        {
                            InventoryInfo newInfo = new InventoryInfo();
                            newInfo.ItemId = item.ItemId;
                            newInfo.InvID = inventory.InvID;
                            newInfo.Price = Double.Parse(Generators.PriceGen.Gen(10, 2));
                            newInfo.LastRestocked = DateTime.Now;
                            newInfo.Quantity = new Random(DateTime.Now.Millisecond).Next(10, 50);

                            if (!invInfos.ContainsKey(item.ItemId)) { invInfos.Add(item.ItemId,newInfo);}
                            
                        }
                    }
                    else if (dept.DPRTName == "Produce")
                    {
                        var relatedItems = (
                            from x in db.Items
                            where _produceCatList.Contains(x.Name)
                            select x
                        ).AsEnumerable();

                        foreach (var item in relatedItems)
                        {
                            InventoryInfo newInfo = new InventoryInfo();
                            newInfo.ItemId = item.ItemId;
                            newInfo.InvID = inventory.InvID;
                            newInfo.Price = Double.Parse(Generators.PriceGen.Gen(10, 2));
                            newInfo.LastRestocked = DateTime.Now;
                            newInfo.Quantity = new Random(DateTime.Now.Millisecond).Next(30, 50);

                             if (!invInfos.ContainsKey(item.ItemId)) { invInfos.Add(item.ItemId,newInfo);}

                        }
                    }
                    else if (dept.DPRTName == "Electronics")
                    {
                        var relatedItems = (
                            from x in db.Items
                            where _electronicCatList.Contains(x.Name)
                            select x
                        ).AsEnumerable();

                        foreach (var item in relatedItems)
                        {

                            InventoryInfo newInfo = new InventoryInfo();

                            newInfo.ItemId = item.ItemId;
                            newInfo.InvID = inventory.InvID;

                            newInfo.Price = Double.Parse(Generators.PriceGen.Gen(999,50));

                            newInfo.LastRestocked = DateTime.Now;
                            newInfo.Quantity = new Random(DateTime.Now.Millisecond).Next(10, 50);

                             if (!invInfos.ContainsKey(item.ItemId)) { invInfos.Add(item.ItemId,newInfo);}

                        }

                    }

                    else if (dept.DPRTName == "Gardening")
                    {
                        var relatedItems = (
                            from x in db.Items
                            where _gardCatList.Contains(x.Name)
                            select x
                        ).AsEnumerable();

                        foreach (var item in relatedItems)
                        {
                            InventoryInfo newInfo = new InventoryInfo();
                            newInfo.ItemId = item.ItemId;
                            newInfo.InvID = inventory.InvID;
                            newInfo.Price = Double.Parse(Generators.PriceGen.Gen(70, 5));
                            newInfo.LastRestocked = DateTime.Now;
                            newInfo.Quantity = new Random(DateTime.Now.Millisecond).Next(10, 50);

                             if (!invInfos.ContainsKey(item.ItemId)) { invInfos.Add(item.ItemId,newInfo);}

                        }
                    }
                    else if (dept.DPRTName == "Automotive")
                    {
                        var relatedItems = (
                            from x in db.Items
                            where _autoCatList.Contains(x.Name)
                            select x
                        ).AsEnumerable();

                        foreach (var item in relatedItems)
                        {
                            InventoryInfo newInfo = new InventoryInfo();
                            newInfo.ItemId = item.ItemId;
                            newInfo.InvID = inventory.InvID;
                            newInfo.Price = Double.Parse(Generators.PriceGen.Gen(80, 3));
                            newInfo.LastRestocked = DateTime.Now;
                            newInfo.Quantity = new Random(DateTime.Now.Millisecond).Next(15, 70);

                             if (!invInfos.ContainsKey(item.ItemId)) { invInfos.Add(item.ItemId,newInfo);}

                        }
                    }




                                 
                    LogG("Fillers",
                        "Added new inventory for " + inventory.Department.DPRTName + " " + inventory.Department.STRID);    
                }


                db.InventoryInfos.InsertAllOnSubmit(invInfos.Values);
                LogG("Fillers", "Updating Inventory info");
                db.SubmitChanges(ConflictMode.ContinueOnConflict);


                    return new Settings.Return();
            }



            public static Settings.Return GenerateCategories()
            {

                var hasCats = db.Categories.GetEnumerator().MoveNext();

                if (hasCats)
                {
                    return new Settings.Return("Categories exist already!", Settings.ReturnResult.Warning);
                }
                #region Filling general catgories
                foreach (var cat in _generalCats)
                {

                    var newCat = new Category()
                    {
                        CatID = CatID(cat),
                        Name = cat,
                        StorePromotions = null
                    };
                    db.Categories.InsertOnSubmit(newCat);
            
                }
                #endregion

                #region Filling auto catgories
                foreach (var cat in _autoCatList)
                {

                    var newCat = new Category()
                    {
                        CatID = CatID(cat),
                        Name = cat,
                        StorePromotions = null
                    };
                    db.Categories.InsertOnSubmit(newCat);
                }
                #endregion

                #region Filling produce catgories
                foreach (var cat in _produceCatList)
                {

                    var newCat = new Category()
                    {
                        CatID = CatID(cat),
                        Name = cat,
                        StorePromotions = null
                    };
                    db.Categories.InsertOnSubmit(newCat);
                }
                #endregion

                #region Filling gardening catgories
                foreach (var cat in _gardCatList)
                {

                    var newCat = new Category()
                    {
                        CatID = CatID(cat),
                        Name = cat,
                        StorePromotions = null
                    };
                    db.Categories.InsertOnSubmit(newCat);
                }
                #endregion

                #region Filling electronics catgories
                foreach (var cat in _electronicCatList)
                {

                    var newCat = new Category()
                    {
                        CatID = CatID(cat),
                        Name = cat,
                        StorePromotions = null
                    };
                    db.Categories.InsertOnSubmit(newCat);
                }
                #endregion

                LogG("Submitting Categories to DB...");
                db.SubmitChanges(); //submit
                LogG("Success.");

                

                return new Settings.Return();
            }


            public static Settings.Return GenerateItems()
            {
                try
                {

                    var items = new Dictionary<string, Item>();
                    //if there are categories
                    if (db.Categories.AsQueryable().Any())
                    {
                        foreach (var category in db.Categories)
                        {

                            var catName = category.Name;

                            #region generate General items

                            if (_generalCats.Contains(category.Name))
                            {
                                
                                //General cat list
                                //"frozen","chilled","baked","shelved","cooking","cleaning","personalhygenie"

                                switch (catName)
                                {
                                    case "frozen":
                                        foreach (var item in _genFrozenItems)
                                        {
                                            var newItem = new Item();
                                            newItem.CatID = category.CatID;
                                            newItem.Description = "This is a frozen item.";
                                            newItem.ItemId = ItemID(item+"frz", category.CatID);
                                            newItem.Name = item;

                                            if (!items.ContainsKey(newItem.ItemId))
                                            {
                                                items.Add(newItem.ItemId, newItem);
                                            }
                                            
                                        }

                                        break;
                                    case "chilled":
                                        foreach (var item in _genChilledItems)
                                        {
                                            var newItem = new Item();
                                            newItem.CatID = category.CatID;
                                            newItem.Description = "This is a chilled item.";
                                            newItem.ItemId = ItemID(item+"chl", category.CatID);

                                            newItem.Name = item;
                                            if (!items.ContainsKey(newItem.ItemId))
                                            {
                                                items.Add(newItem.ItemId, newItem);
                                            }
                                        }

                                        break;
                                    case "baked":
                                        foreach (var item in _genBakedItems)
                                        {
                                            var newItem = new Item();
                                            newItem.CatID = category.CatID;
                                            newItem.Description = "This is a baked item.";
                                            newItem.ItemId = ItemID(item, category.CatID);
                                            newItem.Name = item;
                                            if (!items.ContainsKey(newItem.ItemId))
                                            {
                                                items.Add(newItem.ItemId, newItem);
                                            }

                                        }

                                        break;
                                    case "shelved":
                                        foreach (var item in _genShelvedItems)
                                        {
                                            var newItem = new Item();
                                            newItem.CatID = category.CatID;
                                            newItem.Description = "This is a shelved item.";
                                            newItem.ItemId = ItemID(item+"shv", category.CatID);

                                            newItem.Name = item;
                                            if (!items.ContainsKey(newItem.ItemId))
                                            {
                                                items.Add(newItem.ItemId, newItem);
                                            }

                                        }

                                        break;
                                    case "cooking":
                                        foreach (var item in _genCookingItems)
                                        {
                                            var newItem = new Item();
                                            newItem.CatID = category.CatID;
                                            newItem.Description = "This is a cooking item.";
                                            newItem.ItemId = ItemID(item+"coo", category.CatID);

                                            newItem.Name = item;
                                            if (!items.ContainsKey(newItem.ItemId))
                                            {
                                                items.Add(newItem.ItemId, newItem);
                                            }

                                        }

                                        break;
                                    case "cleaning":
                                        foreach (var item in _genCleaningItems)
                                        {
                                            var newItem = new Item();
                                            newItem.CatID = category.CatID;
                                            newItem.Description = "This is a cleaning item.";
                                            newItem.ItemId = ItemID(item+"cln", category.CatID);

                                            newItem.Name = item;
                                             if (!items.ContainsKey(newItem.ItemId)) { items.Add(newItem.ItemId, newItem); }
                                            
                                        }

                                        break;
                                    case "personalhygenie":
                                        foreach (var item in _genHygieneList)
                                        {
                                            var newItem = new Item();
                                            newItem.CatID = category.CatID;
                                            newItem.Description = "This is a personal-hygenie item.";
                                            newItem.ItemId = ItemID(item+"hyg", category.CatID);
                                            newItem.Name = item;
                                             if (!items.ContainsKey(newItem.ItemId)) { items.Add(newItem.ItemId, newItem); }
                                            
                                        }

                                        break;
                                }
                            }

                            #endregion

                            #region generate Produce items

                            if (_produceCatList.Contains(category.Name))
                            {
                                //Produce cat list
                                //"fruit","vegetable"

                                switch (catName)
                                {
                                    case "fruit":
                                        foreach (var item in _produceFruit)
                                        {
                                            var newItem = new Item();
                                            newItem.CatID = category.CatID;
                                            newItem.Description = "This is a fruit.";
                                            newItem.ItemId = ItemID(item+"frt", category.CatID);


                                            newItem.Name = item;
                                             if (!items.ContainsKey(newItem.ItemId)) { items.Add(newItem.ItemId, newItem); }
                                            
                                        }

                                        break;
                                    case "vegetable":
                                        foreach (var item in _produceVegetable)
                                        {
                                            var newItem = new Item();
                                            newItem.CatID = category.CatID;
                                            newItem.Description = "This is a  vegetable.";
                                            newItem.ItemId = ItemID(item+"veg", category.CatID);

                                            newItem.Name = item;
                                             if (!items.ContainsKey(newItem.ItemId)) { items.Add(newItem.ItemId, newItem); }
                                            
                                        }

                                        break;

                                }
                            }

                            #endregion

                            #region generate Electronic items

                            if (_electronicCatList.Contains(category.Name))
                            {
                               
                                //Elec cat list
                                //  "audio","televisions","game_consoles","computers","computer_accessories",
                                //"game_accessories"

                                switch (catName)
                                {
                                    case "audio":
                                        foreach (var item in _elecAudioList)
                                        {
                                            var newItem = new Item();
                                            newItem.CatID = category.CatID;
                                            newItem.Description = "This is an audio item.";
                                            newItem.ItemId = ItemID(item+"aud", category.CatID);

                                            newItem.Name = item;
                                             if (!items.ContainsKey(newItem.ItemId)) { items.Add(newItem.ItemId, newItem); }
                                            
                                        }

                                        break;
                                    case "televisions":
                                        foreach (var item in _elecTeleList)
                                        {
                                            var newItem = new Item();
                                            newItem.CatID = category.CatID;
                                            newItem.Description = "This is a  TV item.";
                                            newItem.ItemId = ItemID(item+"tv", category.CatID);

                                             if (!items.ContainsKey(newItem.ItemId)) { items.Add(newItem.ItemId, newItem); }
                                            
                                        }

                                        break;
                                    case "game_consoles":
                                        foreach (var item in _elecConsoleList)
                                        {
                                            var newItem = new Item();
                                            newItem.CatID = category.CatID;
                                            newItem.Description = "This is a  game console.";
                                            newItem.ItemId = ItemID(item+"gamc", category.CatID);

                                            newItem.Name = item;
                                             if (!items.ContainsKey(newItem.ItemId)) { items.Add(newItem.ItemId, newItem); }
                                            
                                        }

                                        break;
                                    case "computers":
                                        foreach (var item in _elecCompList)
                                        {
                                            var newItem = new Item();
                                            newItem.CatID = category.CatID;
                                            newItem.Description = "This is a computer.";
                                            newItem.ItemId = ItemID(item+"comp", category.CatID);

                                            newItem.Name = item;
                                             if (!items.ContainsKey(newItem.ItemId)) { items.Add(newItem.ItemId, newItem); }
                                            
                                        }

                                        break;
                                    case "computer_accessories":
                                        foreach (var item in _elecCompAccList)
                                        {
                                            var newItem = new Item();
                                            newItem.CatID = category.CatID;
                                            newItem.Description = "This is a computer accessory.";
                                            newItem.ItemId = ItemID(item+"elecComp", category.CatID);

                                            newItem.Name = item;
                                             if (!items.ContainsKey(newItem.ItemId)) { items.Add(newItem.ItemId, newItem); }
                                            
                                        }

                                        break;
                                    case "game_accessories":
                                        foreach (var item in _elecGameAccList)
                                        {
                                            var newItem = new Item();
                                            newItem.CatID = category.CatID;
                                            newItem.Description = "This is a game accessory.";
                                            newItem.ItemId = ItemID(item+"gamc", category.CatID);

                                            newItem.Name = item;
                                             if (!items.ContainsKey(newItem.ItemId)) { items.Add(newItem.ItemId, newItem); }
                                            
                                        }

                                        break;


                                }
                            }

                            #endregion


                            #region generate Gardening items

                            if (_gardCatList.Contains(category.Name))
                            {
                               
                                //tools, organic


                                switch (catName)
                                {
                                    case "tools":
                                        foreach (var item in _gardToolsList)
                                        {
                                            var newItem = new Item();
                                            newItem.CatID = category.CatID;
                                            newItem.Description = "This is a gardening tool.";
                                            newItem.ItemId = ItemID(item+"gart", category.CatID);

                                            newItem.Name = item;
                                             if (!items.ContainsKey(newItem.ItemId)) { items.Add(newItem.ItemId, newItem); }
                                            
                                        }

                                        break;
                                    case "organic":
                                        foreach (var item in _gardOrganicList)
                                        {
                                            var newItem = new Item();
                                            newItem.CatID = category.CatID;
                                            newItem.Description = "This is an organic garden item.";
                                            newItem.ItemId = ItemID(item+"garo", category.CatID);

                                            newItem.Name = item;
                                             if (!items.ContainsKey(newItem.ItemId)) { items.Add(newItem.ItemId, newItem); }
                                            
                                        }

                                        break;

                                }
                            }

                            #endregion

                            #region generate Automotive items

                            if (_autoCatList.Contains(category.Name))
                            {
                              
                                //"interior_components","maintenance"


                                switch (catName)
                                {
                                    case "interior_components":
                                        foreach (var item in _autoCompList)
                                        {
                                            var newItem = new Item();
                                            newItem.CatID = category.CatID;
                                            newItem.Description = "This is a car interior  item.";
                                            newItem.ItemId = ItemID(item+"atcmp", category.CatID);

                                            newItem.Name = item;
                                             if (!items.ContainsKey(newItem.ItemId)) { items.Add(newItem.ItemId, newItem); }
                                            
                                        }

                                        break;
                                    case "maintenance":
                                        foreach (var item in _autoMainList)
                                        {
                                            var newItem = new Item();
                                            newItem.CatID = category.CatID;
                                            newItem.Description = "This is a car maintenance item.";
                                            newItem.ItemId = ItemID(item+"atmain", category.CatID);

                                            newItem.Name = item;
                                             if (!items.ContainsKey(newItem.ItemId)) { items.Add(newItem.ItemId, newItem); }
                                            
                                        }

                                        break;

                                }


                                
                            }

                            #endregion


                            //End loop
                        }

                        db.Items.InsertAllOnSubmit(items.Values);
                        LogG("Submitting Items to DB...");
                        db.SubmitChanges(ConflictMode.ContinueOnConflict); //submit
                        LogG("Success.");


                    }

                }
                catch (Exception e)
                {
                    return new Settings.Return(e.Message, Settings.ReturnResult.Critical);
                }

                return new Settings.Return();
            }
        }
    }



    
    
}
