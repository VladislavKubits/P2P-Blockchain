using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2P_Blockchain.Blockchain
{
    class Users
    {
        public string Login { get; set; }
        public string PubKey { get; set; }
        public string Level { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string SecondName { get; set; }
        public string Serial { get; set; }
        public string Number { get; set; }

        public Users() { }
        public Users(string Login, string PubKey, string Level, string Name, string Surname, string SecondName, string Serial, string Number)
        {
            this.Login = Login;
            this.PubKey = PubKey;
            this.Level = Level;
            this.Name = Name;
            this.Surname = Surname;
            this.SecondName = SecondName;
            this.Serial = Serial;
            this.Number = Number;
        }

        public String ConvertNewUserToData()
        {
            return String.Format("NewUser:Login:{0}:Level:{1}:PublicKey:{2}:Surname:{3}:Name:{4}:SecondName:{5}:Serial:{6}:Number:{7}", Login, Level, 
                Convert.ToBase64String(Encoding.UTF8.GetBytes(PubKey)), Surname, Name, SecondName, Serial, Number);
        }
    }
}
