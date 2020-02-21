using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestBuilds
{
    class Program
    {
        static string test = @"C:\Daten\..\AMD\Daten\root\tste\dir\..\..\";
        static void Main(string[] args)
        {
            Application.SetCompatibleTextRenderingDefault( true );
            Application.EnableVisualStyles();
            Application.Run(new Form1());


            //Console.WriteLine(test);
            //Console.WriteLine(MakePath(test));
            //Console.Read();
        }
        private static string MakePath(string Dir)
        {
            string ret = "";
            var str = test.Split('\\');
            int rm = 0;
            for (int i = str.Length-1; i >= 0; i-=1)
            {
                if (str[i] == "..")
                {
                    rm +=1;
                    str[i] = "";
                }
                else if(rm>0 && !str[i].Contains(":"))
                {
                    str[i] = "";
                    rm -= 1;
                }
                else
                {
                    ret += str[i] + @"\";
                }
            }
            var v = ret.Split('\\');
            string retur = "";
            for (int i = v.Length-1; i >0 ; i-=1)
            {
                retur += v[i] + @"\";
            }
            return retur.Substring(1);
        }
    }
}
