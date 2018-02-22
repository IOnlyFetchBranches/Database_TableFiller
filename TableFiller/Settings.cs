using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TableFiller.Util;

namespace TableFiller
{
    //Abstract class that contains global static properties to be used by the program.
    public abstract class Settings
    {

        //List of departments, kept here for simplicity 
        public static List<String> DepartmentList => new List<string>()
        {
            "Produce","General","Electronics","Gardening","Automotive"
        };


        //Data Classes
        //Store all defaults here, to make our testing easier
        public abstract class Defaults
        {
            //Set this to => "(Some STRID Here)";
            public static string Store { get; set; }
        }
        //We'll store connection strings here
        public abstract class ConnectionStrings
        {
            //{YOURCOMPUTERNAMEHERE}\\SQLEXPRESS;Initial Catalog=WallyWorld;Integrated Security=True; --Default structure of these....
            public static string Localhost => "Data Source=DESKTOP-OQ6MOPB\\SQLEXPRESS;Initial Catalog=WallyWorld;Integrated Security=True"; //The connection string obtained from Server Explorer.
        }

        //Enums
        


        //Structs {Used to keep data consistent}
        /// <summary>
        /// Represets the result of an operation
        /// Default constructor returns a good instance!
        /// </summary>
        public struct Return
        {

            //Store it here
            private Status status;

            /// <summary>
            /// Gets the Status of a return.
            /// </summary>
            public  Status Status
            {
                get => status;
                set
                {
                    if (status.Value == ReturnResult.Ok)
                    {
                        status = value; // You can change an ok status to an error, but not the other way around
                        //This allows us to have the default set as ok.
                    }
                }
            }

            //Store it here
            private string message;

            //Same concept for Messages
            /// <summary>
            /// Get's the Message of a Return 
            /// </summary>
            public  string Message
            {
                get => message;
                set
                {
                    if (message == "Ok")
                        message = value;
                    else
                    {
                        Logger.LogErr("Return", "Message can only be overwritten once per return.");
                    }
                }
            }


        
            //Constructors
            public Return(Status status)
            {
                this.status = status;
                this.message = "Ok";
            }
            public Return(string message, Status status)
            {
                this.status = status;
                this.message = message;
            }
            public Return(ReturnResult returnResult)
            {
                this.status = new Status(returnResult);
                this.message = "Ok";
            }

            public Return(string message,ReturnResult returnResult)
            {
                this.status = new Status(returnResult);
                this.message = message;
            }

            /// <summary>
            /// Used to attach a message payload to a return. Remember messages can be set only once per return!
            /// </summary>
            /// <param name="message"></param>
            /// <returns></returns>
            public Return WithMessage(string message)
            {
                this.Message = message;
                return this;
            }

            //You can use it as a string.
            public override string ToString()
            {
                return Message;
            }




            //How to compare?
        public override bool Equals(object obj)
            {
                if (obj is Status)
                {
                    if (Status.Equals(((Status) obj)))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (obj is Return)
                    {
                        if (Status.Equals(((Return) obj).Status))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        } //defines something a method can return.

        public struct Status
            //Wrapper struct for ReturnResult Enum
        {
            public readonly ReturnResult Value;

            public override bool Equals(object obj)
            {
                if (obj is Status)
                {
                    return ((Status) obj).Value == this.Value;
                }
                else
                {
                    return false;
                }
            }

            public Status(ReturnResult result )
            {
                Value = result;
            }
        }
        

        //Enums
        public enum ReturnResult
        {
            Ok = 0x001,
            Warning = 0x002,
            Bad = 0x004,
            Critical = 0x008
        }

    }
}
