using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TableFiller.Data.Models;
using TableFiller.Util;
using TableFiller.Util.Models;
using static System.Console; //Make the static import to make simple Console Write calls.

namespace TableFiller.Data.Actors
{
    //When we get to filling Employee Contact info, besure to use the Store that each employee is assigned to a base for their address.
    //I.e a store in Altanta would not have an employee working in it's departmenet with a contact address in statesboro.
    public abstract class Fillers
    {
        //Going to be using LinQ here
        //First grab the Data Context we created from the Linq to Sql class [Data\Models]
        internal static WallyWorldDataContext db = Manager.DB;

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

              
                Logger.LogG("Filling Store:"+STRID);

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
                Logger.LogG("Manger Count:" + mangerSpots);
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

                    Logger.LogG("Current Emp:" + fullName);

                    //Assign
                    newEmployee.FirstName = fullName.First;
                    newEmployee.LastName = fullName.Last;

                    

                    //Finally gen the EID using the name
                    newEmployee.EID = Generators.IDGen.EID(fullName.First, fullName.Last, newEmployee.DID,
                        newEmployee.Position);
                    
                    

                    //Add to Employees
                    newEmployess.Add(newEmployee);

                    //In Our Employee Contact Filler we'll need to make sure that the Assigned Address Make sense as compared to the store's info.



                }

                Logger.LogG("Finished Generating employees.");

                foreach (Employee e in newEmployess)
                {
                    Logger.WriteL(e.EID + " "+ e.FirstName + " " + e.LastName +" " + e.Position);
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
                    Logger.LogG("Updating Manager IDs");


                    //First get all managers
                    var managers = new List<Employee>();

                    foreach (Employee e in db.Employees)
                    {
                        if (e.Position == "Manager")
                            if(e.Department.STRID == STRID)
                                managers.Add(e);

                    }






                    //Set each field
                    foreach (var manager in managers)
                    {
                        // ReSharper disable once ReplaceWithSingleCallToFirst
                        var result = (
                            from dept in db.Departments
                            where dept.DID == manager.DID
                            select dept).SingleOrDefault();
                        if(result == null)
                            return new Settings.Return("Failed to find matching department for DID " + manager.DID 
                                + " " +manager.FirstName + " " +manager.LastName, 
                                    Settings.ReturnResult.Critical);

                        result.MEID = manager.EID;


                        if (manager.Department.DPRTName.Contains("General"))
                        {
                            var managersStore = (
                                from store in db.StoreInfos
                                where store.STRID.Contains(STRID)
                                select store
                            ).SingleOrDefault();

                            managersStore.MEID = manager.EID;
                        }

                        //Push Changes
                        db.SubmitChanges();
        

                    }


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

                        dpt.DID = Generators.IDGen.DID(s, STRID);
                        dpt.MEID = null; //Set in employee generator
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
                    info.STRID = Generators.IDGen.STRID(addr+"");


                    //Add Row Model Object to update queue
                    db.StoreInfos.InsertOnSubmit(info);

                    //Submit
                    db.Refresh(RefreshMode.KeepChanges);
                    db.SubmitChanges();

                    //Print 
                    Logger.LogG("Added: " + addr);


                    return Fill(amount - 1); //Recursive Beauty <3
                }
                catch(Exception e)
                {
                    Logger.LogErr("Fillers:StoreInfo",e.Message + "\nInner:\n" + e.InnerException + "\nTrace:\n" + e.StackTrace );
                    return new Settings.Return(e.Message, Settings.ReturnResult.Bad);
                }
            }
        }
    }



    
    
}
